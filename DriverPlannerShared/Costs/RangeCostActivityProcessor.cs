/*
 * Process a single activity for the range cost calculator
*/

namespace DriverPlannerShared {
    public static class RangeCostActivityProcessor {
        /* Process activities */

        public static void ProcessDriverActivity(Activity searchActivity, ref Activity shiftFirstActivity, ref Activity parkingActivity, ref Activity prevActivity, ref Activity beforeHotelActivity, Func<Activity, bool> isHotelAfterActivity, SaDriverInfo driverInfo, Driver driver, SaInfo info, Instance instance) {
            if (instance.AreSameShift(prevActivity, searchActivity)) {
                /* Same shift */
                // Check overlap
                bool isOverlapViolation = !info.Instance.IsValidSuccession(prevActivity, searchActivity);
                if (isOverlapViolation) {
                    driverInfo.PenaltyInfo.AddOverlapViolation();
                }

                // Count robustness
                driverInfo.Stats.Robustness += instance.ActivitySuccessionRobustness(prevActivity, searchActivity);

                // Shared car travel costs
                int carTravelDistance = instance.CarTravelDistance(prevActivity, searchActivity);
                driverInfo.Stats.Cost += carTravelDistance * RulesConfig.SharedCarCostsPerKilometer;

                // Check for invalid hotel stay
                bool isInvalidHotelAfter = isHotelAfterActivity(prevActivity);
                if (isInvalidHotelAfter) {
                    driverInfo.PenaltyInfo.AddInvalidHotel();
                }

                ProcessDriverActivity(prevActivity, isOverlapViolation, isInvalidHotelAfter, driverInfo, driver);
            } else {
                ProcessDriverActivity(prevActivity, false, false, driverInfo, driver);

                /* Start of new shift */
                ProcessDriverEndShift(prevActivity, searchActivity, ref shiftFirstActivity, ref parkingActivity, ref beforeHotelActivity, isHotelAfterActivity, driverInfo, driver, info, instance);
            }

            prevActivity = searchActivity;
        }

        public static void ProcessDriverEndRange(Activity activityAfterRange, ref Activity shiftFirstActivity, ref Activity parkingActivity, ref Activity prevActivity, ref Activity beforeHotelActivity, Func<Activity, bool> isHotelAfterActivity, SaDriverInfo driverInfo, Driver driver, SaInfo info, Instance instance) {
            if (prevActivity != null) {
                ProcessDriverActivity(prevActivity, false, false, driverInfo, driver);
            }

            // If the range is not empty, finish the last shift of the range
            if (shiftFirstActivity != null) {
                // This is the end of the range, but not the driver path
                ProcessDriverEndShift(prevActivity, activityAfterRange, ref shiftFirstActivity, ref parkingActivity, ref beforeHotelActivity, isHotelAfterActivity, driverInfo, driver, info, instance);
            }
        }

        static void ProcessDriverActivity(Activity activity, bool isOverlapViolation, bool isInvalidHotelAfter, SaDriverInfo driverInfo, Driver driver) {
            // Check for qualification violation
            driverInfo.PenaltyInfo.AddPotentialQualificationViolation(activity, driver);

            // Update shared route counts
            int? sharedRouteIndex = activity.SharedRouteIndex;
            if (sharedRouteIndex.HasValue) {
                driverInfo.SharedRouteCounts[sharedRouteIndex.Value]++;
            }

#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentStageInfo().AddActivity(activity, isOverlapViolation, isInvalidHotelAfter);
            }
#endif
        }


        /* End shifts */

        static void ProcessDriverEndShift(Activity shiftLastActivity, Activity nextShiftFirstActivity, ref Activity shiftFirstActivity, ref Activity parkingActivity, ref Activity beforeHotelActivity, Func<Activity, bool> isHotelAfterActivity, SaDriverInfo driverInfo, Driver driver, SaInfo info, Instance instance) {
            Activity afterHotelActivity = isHotelAfterActivity(shiftLastActivity) ? nextShiftFirstActivity : null;
            (MainShiftInfo mainShiftInfo, DriverTypeMainShiftInfo driverTypeMainShiftInfo, int realMainShiftLength, int fullShiftLength, int fullShiftStartTime, int fullShiftEndTime, int ownCarTravelTime, int sharedCarTravelTimeAfter, int ownCarTravelTimeAfter, float fullShiftCost) = GetShiftDetails(shiftFirstActivity, shiftLastActivity, parkingActivity, beforeHotelActivity, afterHotelActivity, driver, info, instance);

            // Store shift details
            driverInfo.ShiftCount++;
            driverInfo.WorkedTime += driverTypeMainShiftInfo.PaidMainShiftLength;
            driverInfo.TravelTime += ownCarTravelTime;
            driverInfo.Stats.RawCost += fullShiftCost;

            // Check shift length
            driverInfo.PenaltyInfo.AddPossibleShiftLengthViolation(fullShiftLength, mainShiftInfo);

            // Check driver availability
            driverInfo.PenaltyInfo.AddPotentialAvailabilityViolation(fullShiftStartTime, fullShiftEndTime, driver);

            // Determine ideal shift length score
            driverInfo.IdealShiftLengthScore += Math.Max(0, realMainShiftLength - RulesConfig.IdealShiftLength);

            // Update night and weekend counts
            if (mainShiftInfo.IsNightShiftByCompanyRules) driverInfo.NightShiftCountByCompanyRules++;
            if (mainShiftInfo.IsWeekendShiftByCompanyRules) driverInfo.WeekendShiftCountByCompanyRules++;

            if (nextShiftFirstActivity == null) {
                // This is the final shift of the driver path
                // Any hotel stay here would be invalid
                bool isInvalidHotelAfter = isHotelAfterActivity(shiftLastActivity);
                if (isInvalidHotelAfter) {
                    driverInfo.PenaltyInfo.AddInvalidHotel();
                }

#if DEBUG
                if (DevConfig.DebugCheckOperations) {
                    SaDebugger.GetCurrentStageInfo().SetRestInfo(null, false, isInvalidHotelAfter);
                }
#endif
            } else {
                // This is a non-final shift of the driver path
                ProcessRestTime(mainShiftInfo, shiftLastActivity, nextShiftFirstActivity, ref shiftFirstActivity, ref parkingActivity, ref beforeHotelActivity, afterHotelActivity != null, sharedCarTravelTimeAfter, ownCarTravelTimeAfter, driverInfo, driver, info, instance);
            }
        }

        static void ProcessRestTime(MainShiftInfo mainShiftInfo, Activity shiftLastActivity, Activity nextShiftFirstActivity, ref Activity shiftFirstActivity, ref Activity parkingActivity, ref Activity beforeHotelActivity, bool isHotelAfter, int sharedCarTravelTimeAfter, int ownCarTravelTimeAfter, SaDriverInfo driverInfo, Driver driver, SaInfo info, Instance instance) {
            int restTimeAfter;
            bool isInvalidHotelAfter = false; // Used for debugger
            if (isHotelAfter) {
                // Hotel stay after
                driverInfo.HotelCount++;
                restTimeAfter = instance.RestTimeViaHotel(shiftLastActivity, nextShiftFirstActivity);
                driverInfo.Stats.RawCost += RulesConfig.HotelCosts;

                // Check if the hotel stay is valid
                if (!driver.IsHotelAllowed || restTimeAfter > RulesConfig.HotelMaxRestTime) {
                    driverInfo.PenaltyInfo.AddInvalidHotel();
                    isInvalidHotelAfter = true;
                }

                beforeHotelActivity = shiftLastActivity;
            } else {
                // No hotel stay after
                int travelTimeBetweenShiftAndNext = sharedCarTravelTimeAfter + ownCarTravelTimeAfter + driver.HomeTravelTimeToStart(nextShiftFirstActivity);
                restTimeAfter = instance.RestTimeWithTravelTime(shiftLastActivity, nextShiftFirstActivity, travelTimeBetweenShiftAndNext);

                // Update free days
                if (restTimeAfter > RulesConfig.DoubleFreeDayMinRestTime) {
                    driverInfo.SingleFreeDayCount++;
                } else if (restTimeAfter > RulesConfig.SingleFreeDayMinRestTime) {
                    driverInfo.DoubleFreeDayCount++;
                }

                // Set new parking activity
                parkingActivity = nextShiftFirstActivity;
                beforeHotelActivity = null;
            }

            // Check rest time
            driverInfo.PenaltyInfo.AddPossibleRestTimeViolation(restTimeAfter, mainShiftInfo.MinRestTimeAfter);

            // Determine ideal rest time score
            if (restTimeAfter < RulesConfig.IdealRestTime) {
                int idealRestTimeDeficit = RulesConfig.IdealRestTime - restTimeAfter;
                driverInfo.IdealRestingTimeScore += idealRestTimeDeficit * idealRestTimeDeficit;
            }

            // Start new shift
            shiftFirstActivity = nextShiftFirstActivity;

#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentStageInfo().SetRestInfo(restTimeAfter, isHotelAfter, isInvalidHotelAfter);
            }
#endif
        }


        /* Helpers */

        public static (MainShiftInfo, DriverTypeMainShiftInfo, int, int, int, int, int, int, int, float) GetShiftDetails(Activity shiftFirstActivity, Activity shiftLastActivity, Activity parkingActivity, Activity beforeHotelActivity, Activity afterHotelActivity, Driver driver, SaInfo info, Instance instance) {
            (int sharedCarTravelTimeBefore, int sharedCarTravelDistanceBefore, int ownCarTravelTimeBefore, int ownCarTravelDistanceBefore) = GetTravelInfoBefore(beforeHotelActivity, shiftFirstActivity, driver, instance);
            (int sharedCarTravelTimeAfter, int sharedCarTravelDistanceAfter, int ownCarTravelTimeAfter, int ownCarTravelDistanceAfter) = GetTravelInfoAfter(shiftLastActivity, afterHotelActivity, parkingActivity, driver, instance);

            int mainShiftStartTime = shiftFirstActivity.StartTime - sharedCarTravelTimeBefore;
            int realMainShiftEndTime = shiftLastActivity.EndTime + sharedCarTravelTimeAfter;
            int realMainShiftLength = realMainShiftEndTime - mainShiftStartTime;

            MainShiftInfo mainShiftInfo = info.Instance.MainShiftInfo(mainShiftStartTime, realMainShiftEndTime);
            DriverTypeMainShiftInfo driverTypeMainShiftInfo = mainShiftInfo.ByDriver(driver);

            // Get travel time and cost and shift length
            int ownCarTravelTime = ownCarTravelTimeBefore + ownCarTravelTimeAfter;
            int ownCarTravelDistance = ownCarTravelDistanceBefore + ownCarTravelDistanceAfter;
            float ownCarTravelCost = driver.GetPaidTravelCost(ownCarTravelTime, ownCarTravelDistance);

            // Get shared car costs to pick up personal car
            int sharedCarTravelDistance = sharedCarTravelDistanceBefore + sharedCarTravelDistanceAfter;

            // Get full shift length and cost
            int fullShiftStartTime = mainShiftStartTime - ownCarTravelTimeBefore;
            int fullShiftEndTime = realMainShiftEndTime + ownCarTravelTimeAfter;
            int fullShiftLength = fullShiftEndTime - fullShiftStartTime;
            float fullShiftCost = driverTypeMainShiftInfo.MainShiftCost + ownCarTravelCost + sharedCarTravelDistance * RulesConfig.SharedCarCostsPerKilometer;

#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentStageInfo().SetShiftDetails(shiftFirstActivity, shiftLastActivity, afterHotelActivity, mainShiftInfo, driverTypeMainShiftInfo, fullShiftLength, sharedCarTravelTimeBefore, ownCarTravelTimeBefore, sharedCarTravelTimeAfter, ownCarTravelTimeAfter);
            }
#endif

            return (mainShiftInfo, driverTypeMainShiftInfo, realMainShiftLength, fullShiftLength, fullShiftStartTime, fullShiftEndTime, ownCarTravelTime, sharedCarTravelTimeAfter, ownCarTravelTimeAfter, fullShiftCost);
        }

        public static (int, int, int, int) GetTravelInfoBefore(Activity beforeHotelActivity, Activity shiftFirstActivity, Driver driver, Instance instance) {
            int sharedCarTravelTimeBefore, sharedCarTravelDistanceBefore, ownCarTravelTimeBefore, ownCarTravelDistanceBefore;
            if (beforeHotelActivity == null) {
                // No hotel stay before
                sharedCarTravelTimeBefore = sharedCarTravelDistanceBefore = 0;
                ownCarTravelTimeBefore = driver.HomeTravelTimeToStart(shiftFirstActivity);
                ownCarTravelDistanceBefore = driver.HomeTravelDistanceToStart(shiftFirstActivity);
            } else {
                // Hotel stay before
                sharedCarTravelTimeBefore = instance.ExpectedHalfTravelTimeViaHotel(beforeHotelActivity, shiftFirstActivity);
                sharedCarTravelDistanceBefore = instance.HalfTravelDistanceViaHotel(beforeHotelActivity, shiftFirstActivity);
                ownCarTravelTimeBefore = ownCarTravelDistanceBefore = 0;
            }
            return (sharedCarTravelTimeBefore, sharedCarTravelDistanceBefore, ownCarTravelTimeBefore, ownCarTravelDistanceBefore);
        }

        public static (int, int, int, int) GetTravelInfoAfter(Activity shiftLastActivity, Activity afterHotelActivity, Activity parkingActivity, Driver driver, Instance instance) {
            int sharedCarTravelTimeAfter, sharedCarTravelDistanceAfter, ownCarTravelTimeAfter, ownCarTravelDistanceAfter;
            if (afterHotelActivity == null) {
                // No hotel stay after
                sharedCarTravelTimeAfter = instance.ExpectedCarTravelTime(shiftLastActivity, parkingActivity);
                sharedCarTravelDistanceAfter = instance.CarTravelDistance(shiftLastActivity, parkingActivity);
                ownCarTravelTimeAfter = driver.HomeTravelTimeToStart(parkingActivity);
                ownCarTravelDistanceAfter = driver.HomeTravelDistanceToStart(parkingActivity);
            } else {
                // Hotel stay after
                sharedCarTravelTimeAfter = instance.ExpectedHalfTravelTimeViaHotel(shiftLastActivity, afterHotelActivity);
                sharedCarTravelDistanceAfter = instance.HalfTravelDistanceViaHotel(shiftLastActivity, afterHotelActivity);
                ownCarTravelTimeAfter = ownCarTravelDistanceAfter = 0;
            }
            return (sharedCarTravelTimeAfter, sharedCarTravelDistanceAfter, ownCarTravelTimeAfter, ownCarTravelDistanceAfter);
        }
    }
}

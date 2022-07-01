using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class RangeCostActivityProcessor {
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
                driverInfo.Stats.Cost += carTravelDistance * SalaryConfig.SharedCarCostsPerKilometer;

                // Check for invalid hotel stay
                bool isInvalidHotelAfter = isHotelAfterActivity(prevActivity);
                if (isInvalidHotelAfter) {
                    driverInfo.PenaltyInfo.AddInvalidHotel();
                }

                // Check for qualification violation
                driverInfo.PenaltyInfo.AddPotentialQualificationViolation(prevActivity, driver);

                ProcessDriverActivity(prevActivity, isOverlapViolation, isInvalidHotelAfter, driverInfo);
            } else {
                ProcessDriverActivity(prevActivity, false, false, driverInfo);

                /* Start of new shift */
                ProcessDriverEndNonFinalShift(searchActivity, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivity, driverInfo, driver, info, instance);
            }

            prevActivity = searchActivity;
        }

        public static void ProcessDriverEndRange(Activity activityAfterRange, ref Activity shiftFirstActivity, ref Activity parkingActivity, ref Activity prevActivity, ref Activity beforeHotelActivity, Func<Activity, bool> isHotelAfterActivity, SaDriverInfo driverInfo, Driver driver, SaInfo info, Instance instance) {
            if (prevActivity != null) {
                ProcessDriverActivity(prevActivity, false, false, driverInfo);
            }

            // If the range is not empty, finish the last shift of the range
            if (shiftFirstActivity != null) {
                if (activityAfterRange == null) {
                    // This is the end of the driver path
                    ProcessDriverEndFinalShift(ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivity, driverInfo, driver, info, instance);
                } else {
                    // This is the end of the range, but not the driver path
                    ProcessDriverEndNonFinalShift(activityAfterRange, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivity, driverInfo, driver, info, instance);
                }
            }
        }

        static void ProcessDriverEndNonFinalShift(Activity searchActivity, ref Activity shiftFirstActivity, ref Activity parkingActivity, ref Activity prevActivity, ref Activity beforeHotelActivity, Func<Activity, bool> isHotelAfterActivity, SaDriverInfo driverInfo, Driver driver, SaInfo info, Instance instance) {
            ShiftInfo shiftInfo = info.Instance.ShiftInfo(shiftFirstActivity, prevActivity);

            // Get travel time after and rest time
            bool isHotelAfter = isHotelAfterActivity(prevActivity);
            (int travelTimeAfter, int travelDistanceAfter, int poolCarTravelDistanceAfter) = GetTravelInfoAfter(prevActivity, searchActivity, parkingActivity, isHotelAfter, driver, instance);
            int restTimeAfter;
            bool isInvalidHotelAfter = false; // Used for debugger
            if (isHotelAfter) {
                // Hotel stay after
                driverInfo.HotelCount++;
                restTimeAfter = instance.RestTimeViaHotel(prevActivity, searchActivity);
                driverInfo.Stats.RawCost += SalaryConfig.HotelCosts;

                // Check if the hotel stay is valid
                if (!driver.IsHotelAllowed || restTimeAfter > RulesConfig.HotelMaxRestTime) {
                    driverInfo.PenaltyInfo.AddInvalidHotel();
                    isInvalidHotelAfter = true;
                }

                beforeHotelActivity = prevActivity;
            } else {
                // No hotel stay after
                restTimeAfter = instance.RestTimeWithTravelTime(prevActivity, searchActivity, travelTimeAfter + driver.HomeTravelTimeToStart(searchActivity));

                // Update free days
                if (restTimeAfter > RulesConfig.DoubleFreeDayMinRestTime) {
                    driverInfo.SingleFreeDayCount++;
                } else if (restTimeAfter > RulesConfig.SingleFreeDayMinRestTime) {
                    driverInfo.DoubleFreeDayCount++;
                }

                // Set new parking activity
                parkingActivity = searchActivity;
                beforeHotelActivity = null;
            }

            // Check rest time
            driverInfo.PenaltyInfo.AddPossibleRestTimeViolation(restTimeAfter, shiftInfo.MinRestTimeAfter);

            // Determine ideal rest time score
            if (restTimeAfter < RulesConfig.IdealRestTime) {
                int idealRestTimeDeficit = RulesConfig.IdealRestTime - restTimeAfter;
                driverInfo.IdealRestingTimeScore += idealRestTimeDeficit * idealRestTimeDeficit;
            }

            #if DEBUG
            if (AppConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentStageInfo().EndShiftPart1(restTimeAfter, isHotelAfter, isInvalidHotelAfter);
            }
            #endif

            // Process parts shared between final and non-final shifts
            ProcessDriverEndAnyShift(shiftInfo, travelTimeAfter, travelDistanceAfter, poolCarTravelDistanceAfter, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivity, driverInfo, driver, info, instance);

            // Start new shift
            shiftFirstActivity = searchActivity;
        }

        static void ProcessDriverEndFinalShift(ref Activity shiftFirstActivity, ref Activity parkingActivity, ref Activity prevActivity, ref Activity beforeHotelActivity, Func<Activity, bool> isHotelAfterActivity, SaDriverInfo driverInfo, Driver driver, SaInfo info, Instance instance) {
            ShiftInfo shiftInfo = info.Instance.ShiftInfo(shiftFirstActivity, prevActivity);

            // Get travel time after
            int travelTimeAfter = instance.ExpectedCarTravelTime(prevActivity, parkingActivity) + driver.HomeTravelTimeToStart(parkingActivity);
            int poolCarTravelDistanceAfter = instance.CarTravelDistance(prevActivity, parkingActivity);
            int travelDistanceAfter = poolCarTravelDistanceAfter + driver.HomeTravelDistanceToStart(parkingActivity);

            // Check for invalid hotel stay
            bool isInvalidHotelAfter = isHotelAfterActivity(prevActivity);
            if (isInvalidHotelAfter) {
                driverInfo.PenaltyInfo.AddInvalidHotel();
            }

            #if DEBUG
            if (AppConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentStageInfo().EndShiftPart1(null, false, isInvalidHotelAfter);
            }
            #endif

            // Process parts shared between final and non-final shifts
            ProcessDriverEndAnyShift(shiftInfo, travelTimeAfter, travelDistanceAfter, poolCarTravelDistanceAfter, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivity, driverInfo, driver, info, instance);
        }

        static void ProcessDriverEndAnyShift(ShiftInfo shiftInfo, int travelTimeAfter, int travelDistanceAfter, int sharedCarTravelDistanceAfter, ref Activity shiftFirstActivity, ref Activity parkingActivity, ref Activity prevActivity, ref Activity beforeHotelActivity, Func<Activity, bool> isHotelAfterActivity, SaDriverInfo driverInfo, Driver driver, SaInfo info, Instance instance) {
            driverInfo.ShiftCount++;

            // Get travel time before
            (int travelTimeBefore, int travelDistanceBefore) = GetTravelInfoBefore(beforeHotelActivity, shiftFirstActivity, driver, instance);

            // Get driving time
            int shiftLengthWithoutTravel = shiftInfo.DrivingTime;
            float drivingCost = driver.DrivingCost(shiftFirstActivity, prevActivity);
            driverInfo.WorkedTime += shiftLengthWithoutTravel;

            // Get travel time and shift length
            int travelTime = travelTimeBefore + travelTimeAfter;
            int travelDistance = travelDistanceBefore + travelDistanceAfter;
            driverInfo.TravelTime += travelTime;
            int shiftLengthWithTravel = shiftLengthWithoutTravel + travelTime;

            // Get shift cost
            float travelCost = driver.GetPaidTravelCost(travelTime, travelDistance);
            float shiftCost = drivingCost + travelCost;
            driverInfo.Stats.RawCost += shiftCost;

            // Get shared car costs to pick up personal car
            int sharedCarTravelDistance = travelDistanceBefore + sharedCarTravelDistanceAfter;
            driverInfo.Stats.Cost += sharedCarTravelDistance * SalaryConfig.SharedCarCostsPerKilometer;

            // Check shift length
            driverInfo.PenaltyInfo.AddPossibleShiftLengthViolation(shiftLengthWithoutTravel, shiftLengthWithTravel, shiftInfo.MaxShiftLengthWithoutTravel, shiftInfo.MaxShiftLengthWithTravel);

            // Determine ideal shift length score
            driverInfo.IdealShiftLengthScore += Math.Max(0, shiftLengthWithoutTravel - RulesConfig.IdealShiftLength);

            // Update night and weekend counts
            if (shiftInfo.IsNightShiftByCompanyRules) driverInfo.NightShiftCountByCompanyRules++;
            if (shiftInfo.IsWeekendShiftByCompanyRules) driverInfo.WeekendShiftCountByCompanyRules++;

            #if DEBUG
            if (AppConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentStageInfo().EndShiftPart2(shiftInfo, shiftLengthWithTravel, travelTimeBefore, travelTimeAfter);
            }
            #endif
        }

        static void ProcessDriverActivity(Activity activity, bool isOverlapViolation, bool isInvalidHotelAfter, SaDriverInfo driverInfo) {
            // Update shared route counts
            int? sharedRouteIndex = activity.SharedRouteIndex;
            if (sharedRouteIndex.HasValue) {
                driverInfo.SharedRouteCounts[sharedRouteIndex.Value]++;
            }

            #if DEBUG
            if (AppConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentStageInfo().AddActivity(activity, isOverlapViolation, isInvalidHotelAfter);
            }
            #endif
        }


        /* Helpers */

        public static (int, int) GetTravelInfoBefore(Activity activityBeforeHotel, Activity shiftFirstActivity, Driver driver, Instance instance) {
            int travelTimeBefore, travelDistanceBefore;
            if (activityBeforeHotel == null) {
                // No hotel stay before
                travelTimeBefore = driver.HomeTravelTimeToStart(shiftFirstActivity);
                travelDistanceBefore = driver.HomeTravelDistanceToStart(shiftFirstActivity);
            } else {
                // Hotel stay before
                travelTimeBefore = instance.ExpectedHalfTravelTimeViaHotel(activityBeforeHotel, shiftFirstActivity);
                travelDistanceBefore = instance.HalfTravelDistanceViaHotel(activityBeforeHotel, shiftFirstActivity);
            }
            return (travelTimeBefore, travelDistanceBefore);
        }

        public static (int, int, int) GetTravelInfoAfter(Activity shiftLastActivity, Activity nextShiftFirstActivity, Activity parkingActivity, bool isHotelAfter, Driver driver, Instance instance) {
            int travelTimeAfter, travelDistanceAfter, poolCarTravelDistanceAfter;
            if (isHotelAfter) {
                // Hotel stay after
                travelTimeAfter = instance.ExpectedHalfTravelTimeViaHotel(shiftLastActivity, nextShiftFirstActivity);
                poolCarTravelDistanceAfter = instance.HalfTravelDistanceViaHotel(shiftLastActivity, nextShiftFirstActivity);
                travelDistanceAfter = poolCarTravelDistanceAfter;
            } else {
                // No hotel stay after
                travelTimeAfter = instance.ExpectedCarTravelTime(shiftLastActivity, parkingActivity) + driver.HomeTravelTimeToStart(parkingActivity);
                poolCarTravelDistanceAfter = instance.CarTravelDistance(shiftLastActivity, parkingActivity);
                travelDistanceAfter = poolCarTravelDistanceAfter + driver.HomeTravelDistanceToStart(parkingActivity);
            }
            return (travelTimeAfter, travelDistanceAfter, poolCarTravelDistanceAfter);
        }
    }
}

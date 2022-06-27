using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class RangeCostTripProcessor {
        public static void ProcessDriverTrip(Trip searchTrip, ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, Func<Trip, bool> isHotelAfterTrip, SaDriverInfo driverInfo, Driver driver, SaInfo info, Instance instance) {
            if (instance.AreSameShift(prevTrip, searchTrip)) {
                /* Same shift */
                // Check precedence
                bool isPrecedenceViolation = !info.Instance.IsValidPrecedence(prevTrip, searchTrip);
                if (isPrecedenceViolation) {
                    driverInfo.PenaltyInfo.AddPrecedenceViolation();
                }

                // Count robustness
                driverInfo.Stats.Robustness += instance.TripSuccessionRobustness(prevTrip, searchTrip);

                // Check for invalid hotel stay
                bool isInvalidHotelAfter = isHotelAfterTrip(prevTrip);
                if (isInvalidHotelAfter) {
                    driverInfo.PenaltyInfo.AddInvalidHotel();
                }

                ProcessDriverTrip(prevTrip, isPrecedenceViolation, isInvalidHotelAfter, driverInfo);
            } else {
                ProcessDriverTrip(prevTrip, false, false, driverInfo);

                /* Start of new shift */
                ProcessDriverEndNonFinalShift(searchTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, instance);
            }

            prevTrip = searchTrip;
        }

        public static void ProcessDriverEndRange(Trip tripAfterRange, ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, Func<Trip, bool> isHotelAfterTrip, SaDriverInfo driverInfo, Driver driver, SaInfo info, Instance instance) {
            if (prevTrip != null) {
                ProcessDriverTrip(prevTrip, false, false, driverInfo);
            }

            // If the range is not empty, finish the last shift of the range
            if (shiftFirstTrip != null) {
                if (tripAfterRange == null) {
                    // This is the end of the driver path
                    ProcessDriverEndFinalShift(ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, instance);
                } else {
                    // This is the end of the range, but not the driver path
                    ProcessDriverEndNonFinalShift(tripAfterRange, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, instance);
                }
            }
        }

        static void ProcessDriverEndNonFinalShift(Trip searchTrip, ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, Func<Trip, bool> isHotelAfterTrip, SaDriverInfo driverInfo, Driver driver, SaInfo info, Instance instance) {
            ShiftInfo shiftInfo = info.Instance.ShiftInfo(shiftFirstTrip, prevTrip);

            // Get travel time after and rest time
            bool isHotelAfter = isHotelAfterTrip(prevTrip);
            (int travelTimeAfter, int travelDistanceAfter) = GetTravelInfoAfter(prevTrip, searchTrip, parkingTrip, isHotelAfter, driver, instance);
            int restTimeAfter;
            bool isInvalidHotelAfter = false; // Used for debugger
            if (isHotelAfter) {
                // Hotel stay after
                driverInfo.HotelCount++;
                restTimeAfter = instance.RestTimeViaHotel(prevTrip, searchTrip);
                driverInfo.Stats.RawCost += SalaryConfig.HotelCosts;

                // Check if the hotel stay is valid
                if (!driver.IsHotelAllowed || restTimeAfter > RulesConfig.HotelMaxRestTime) {
                    driverInfo.PenaltyInfo.AddInvalidHotel();
                    isInvalidHotelAfter = true;
                }

                beforeHotelTrip = prevTrip;
            } else {
                // No hotel stay after
                restTimeAfter = instance.RestTimeWithTravelTime(prevTrip, searchTrip, travelTimeAfter + driver.HomeTravelTimeToStart(searchTrip));

                // Update free days
                if (restTimeAfter > RulesConfig.DoubleFreeDayMinRestTime) {
                    driverInfo.SingleFreeDayCount++;
                } else if (restTimeAfter > RulesConfig.SingleFreeDayMinRestTime) {
                    driverInfo.DoubleFreeDayCount++;
                }

                // Set new parking trip
                parkingTrip = searchTrip;
                beforeHotelTrip = null;
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
            ProcessDriverEndAnyShift(shiftInfo, travelTimeAfter, travelDistanceAfter, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, instance);

            // Start new shift
            shiftFirstTrip = searchTrip;
        }

        static void ProcessDriverEndFinalShift(ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, Func<Trip, bool> isHotelAfterTrip, SaDriverInfo driverInfo, Driver driver, SaInfo info, Instance instance) {
            ShiftInfo shiftInfo = info.Instance.ShiftInfo(shiftFirstTrip, prevTrip);

            // Get travel time after
            int travelTimeAfter = instance.ExpectedCarTravelTime(prevTrip, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);
            int travelDistanceAfter = instance.CarTravelDistance(prevTrip, parkingTrip) + driver.HomeTravelDistanceToStart(parkingTrip);

            // Check for invalid hotel stay
            bool isInvalidHotelAfter = isHotelAfterTrip(prevTrip);
            if (isInvalidHotelAfter) {
                driverInfo.PenaltyInfo.AddInvalidHotel();
            }

            #if DEBUG
            if (AppConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentStageInfo().EndShiftPart1(null, false, isInvalidHotelAfter);
            }
            #endif

            // Process parts shared between final and non-final shifts
            ProcessDriverEndAnyShift(shiftInfo, travelTimeAfter, travelDistanceAfter, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, instance);
        }

        static void ProcessDriverEndAnyShift(ShiftInfo shiftInfo, int travelTimeAfter, int travelDistanceAfter, ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, Func<Trip, bool> isHotelAfterTrip, SaDriverInfo driverInfo, Driver driver, SaInfo info, Instance instance) {
            driverInfo.ShiftCount++;

            // Get travel time before
            (int travelTimeBefore, int travelDistanceBefore) = GetTravelInfoBefore(beforeHotelTrip, shiftFirstTrip, driver, instance);

            // Get driving time
            int shiftLengthWithoutTravel = shiftInfo.DrivingTime;
            float drivingCost = driver.DrivingCost(shiftFirstTrip, prevTrip);
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

        static void ProcessDriverTrip(Trip trip, bool isPrecedenceViolation, bool isInvalidHotelAfter, SaDriverInfo driverInfo) {
            // Update shared route counts
            int? sharedRouteIndex = trip.SharedRouteIndex;
            if (sharedRouteIndex.HasValue) {
                driverInfo.SharedRouteCounts[sharedRouteIndex.Value]++;
            }

            #if DEBUG
            if (AppConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentStageInfo().AddTrip(trip, isPrecedenceViolation, isInvalidHotelAfter);
            }
            #endif
        }


        /* Helpers */

        public static (int, int) GetTravelInfoBefore(Trip tripBeforeHotel, Trip shiftFirstTrip, Driver driver, Instance instance) {
            int travelTimeBefore, travelDistanceBefore;
            if (tripBeforeHotel == null) {
                // No hotel stay before
                travelTimeBefore = driver.HomeTravelTimeToStart(shiftFirstTrip);
                travelDistanceBefore = driver.HomeTravelDistanceToStart(shiftFirstTrip);
            } else {
                // Hotel stay before
                travelTimeBefore = instance.ExpectedHalfTravelTimeViaHotel(tripBeforeHotel, shiftFirstTrip);
                travelDistanceBefore = instance.HalfTravelDistanceViaHotel(tripBeforeHotel, shiftFirstTrip);
            }
            return (travelTimeBefore, travelDistanceBefore);
        }

        public static (int, int) GetTravelInfoAfter(Trip shiftLastTrip, Trip nextShiftFirstTrip, Trip parkingTrip, bool isHotelAfter, Driver driver, Instance instance) {
            int travelTimeAfter, travelDistanceAfter;
            if (isHotelAfter) {
                // Hotel stay after
                travelTimeAfter = instance.ExpectedHalfTravelTimeViaHotel(shiftLastTrip, nextShiftFirstTrip);
                travelDistanceAfter = instance.HalfTravelDistanceViaHotel(shiftLastTrip, nextShiftFirstTrip);
            } else {
                // No hotel stay after
                travelTimeAfter = instance.ExpectedCarTravelTime(shiftLastTrip, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);
                travelDistanceAfter = instance.CarTravelDistance(shiftLastTrip, parkingTrip) + driver.HomeTravelDistanceToStart(parkingTrip);
            }
            return (travelTimeAfter, travelDistanceAfter);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class RangeCostTripProcessor {
        public static void ProcessDriverTrip(Trip searchTrip, ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, Func<Trip, bool> isHotelAfterTrip, DriverInfo driverInfo, Driver driver, SaInfo info, Instance instance) {
            if (instance.AreSameShift(prevTrip, searchTrip)) {
                /* Same shift */
                // Check precedence
                bool isPrecedenceViolation = !info.Instance.IsValidPrecedence(prevTrip, searchTrip);
                if (isPrecedenceViolation) {
                    driverInfo.PenaltyInfo.AddPrecedenceViolation();
                }

                // Count robustness
                driverInfo.Robustness += instance.TripSuccessionRobustness(prevTrip, searchTrip);

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

        public static void ProcessDriverEndRange(Trip tripAfterRange, ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, Func<Trip, bool> isHotelAfterTrip, DriverInfo driverInfo, Driver driver, SaInfo info, Instance instance) {
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

        static void ProcessDriverEndNonFinalShift(Trip searchTrip, ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, Func<Trip, bool> isHotelAfterTrip, DriverInfo driverInfo, Driver driver, SaInfo info, Instance instance) {
            ShiftInfo shiftInfo = info.Instance.ShiftInfo(shiftFirstTrip, prevTrip);

            // Get travel time after and rest time
            bool isHotelAfter = isHotelAfterTrip(prevTrip);
            int travelTimeAfter, restTimeAfter;
            bool isInvalidHotelAfter = false; // Used for debugger
            if (isHotelAfter) {
                // Hotel stay after
                driverInfo.HotelCount++;
                travelTimeAfter = instance.HalfTravelTimeViaHotel(prevTrip, searchTrip);
                restTimeAfter = instance.RestTimeViaHotel(prevTrip, searchTrip);
                driverInfo.RawCost += Config.HotelCosts;

                // Check if the hotel stay isn't too long
                if (restTimeAfter > Config.HotelMaxRestTime) {
                    driverInfo.PenaltyInfo.AddInvalidHotel();
                    isInvalidHotelAfter = true;
                }

                beforeHotelTrip = prevTrip;
            } else {
                // No hotel stay after
                travelTimeAfter = instance.CarTravelTime(prevTrip, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);
                restTimeAfter = instance.RestTimeWithTravelTime(prevTrip, searchTrip, travelTimeAfter + driver.HomeTravelTimeToStart(searchTrip));

                // Update free days
                if (restTimeAfter > Config.DoubleFreeDayMinRestTime) {
                    driverInfo.SingleFreeDays++;
                } else if (restTimeAfter > Config.SingleFreeDayMinRestTime) {
                    driverInfo.DoubleFreeDays++;
                }

                // Set new parking trip
                parkingTrip = searchTrip;
                beforeHotelTrip = null;
            }

            // Check rest time
            driverInfo.PenaltyInfo.AddPossibleRestTimeViolation(restTimeAfter);

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentStageInfo().EndShiftPart1(restTimeAfter, isHotelAfter, isInvalidHotelAfter);
            }
            #endif

            // Process parts shared between final and non-final shifts
            ProcessDriverEndAnyShift(shiftInfo, travelTimeAfter, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, instance);

            // Start new shift
            shiftFirstTrip = searchTrip;
        }

        static void ProcessDriverEndFinalShift(ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, Func<Trip, bool> isHotelAfterTrip, DriverInfo driverInfo, Driver driver, SaInfo info, Instance instance) {
            ShiftInfo shiftInfo = info.Instance.ShiftInfo(shiftFirstTrip, prevTrip);

            // Get travel time after
            int travelTimeAfter = instance.CarTravelTime(prevTrip, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);

            // Check for invalid hotel stay
            bool isInvalidHotelAfter = isHotelAfterTrip(prevTrip);
            if (isInvalidHotelAfter) {
                driverInfo.PenaltyInfo.AddInvalidHotel();
            }

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentStageInfo().EndShiftPart1(null, false, isInvalidHotelAfter);
            }
            #endif

            // Process parts shared between final and non-final shifts
            ProcessDriverEndAnyShift(shiftInfo, travelTimeAfter, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, instance);
        }

        static void ProcessDriverEndAnyShift(ShiftInfo shiftInfo, int travelTimeAfter, ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, Func<Trip, bool> isHotelAfterTrip, DriverInfo driverInfo, Driver driver, SaInfo info, Instance instance) {
            driverInfo.ShiftCount++;

            // Get travel time before
            int travelTimeBefore;
            if (beforeHotelTrip == null) {
                // No hotel stay before
                travelTimeBefore = driver.HomeTravelTimeToStart(shiftFirstTrip);
            } else {
                // Hotel stay before
                travelTimeBefore = instance.HalfTravelTimeViaHotel(beforeHotelTrip, shiftFirstTrip);
            }

            // Get driving time
            int shiftLengthWithoutTravel = shiftInfo.DrivingTime;
            float drivingCost = driver.DrivingCost(shiftFirstTrip, prevTrip);
            driverInfo.WorkedTime += shiftLengthWithoutTravel;

            // Get travel time and shift length
            int travelTime = travelTimeBefore + travelTimeAfter;
            driverInfo.TravelTime += travelTime;
            int shiftLengthWithTravel = shiftLengthWithoutTravel + travelTime;

            // Get shift cost
            float travelCost = driver.GetPaidTravelCost(travelTime);
            float shiftCost = drivingCost + travelCost;
            driverInfo.RawCost += shiftCost;

            // Check shift length
            driverInfo.PenaltyInfo.AddPossibleShiftLengthViolation(shiftLengthWithoutTravel, shiftLengthWithTravel);

            // Update night and weekend counts
            if (shiftInfo.IsNightShift) driverInfo.NightShiftCount++;
            if (shiftInfo.IsWeekendShift) driverInfo.WeekendShiftCount++;

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentStageInfo().EndShiftPart2(shiftInfo, shiftLengthWithTravel, travelTimeBefore, travelTimeAfter);
            }
            #endif
        }

        static void ProcessDriverTrip(Trip trip, bool isPrecedenceViolation, bool isInvalidHotelAfter, DriverInfo driverInfo) {
            // Update shared route counts
            int? sharedRouteIndex = trip.SharedRouteIndex;
            if (sharedRouteIndex.HasValue) {
                driverInfo.SharedRouteCounts[sharedRouteIndex.Value]++;
            }

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentStageInfo().AddTrip(trip, isPrecedenceViolation, isInvalidHotelAfter);
            }
            #endif
        }
    }
}

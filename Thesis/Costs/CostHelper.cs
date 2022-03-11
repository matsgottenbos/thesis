using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class CostHelper {
        /* Travelling */

        public static int OneWayTravelTimeFromHome(Trip trip, Driver driver) {
            return driver.OneWayTravelTimes[trip.FirstStation];
        }

        public static int OneWayTravelTimeToHome(Trip trip, Driver driver) {
            return driver.OneWayTravelTimes[trip.LastStation];
        }

        public static int TwoWayPayedTravelTimeFromHome(Trip trip, Driver driver) {
            return driver.TwoWayPayedTravelTimes[trip.FirstStation];
        }

        public static float TwoWayPayedTravelCostFromHome(Trip trip, Driver driver) {
            return TwoWayPayedTravelTimeFromHome(trip, driver) * Config.SalaryRate;
        }

        public static int CarTravelTime(Trip trip1, Trip trip2, Instance instance) {
            return instance.CarTravelTimes[trip1.LastStation, trip2.FirstStation];
        }


        /* Waiting and same shift threshold */

        static int WaitingTime(Trip trip1, Trip trip2, Instance instance) {
            return trip2.StartTime - trip1.EndTime - CarTravelTime(trip1, trip2, instance);
        }

        public static bool AreSameShift(Trip trip1, Trip trip2, Instance instance) {
            return WaitingTime(trip1, trip2, instance) <= Config.ShiftWaitingTimeThreshold;
        }


        /* Shifts and resting */

        public static int ShiftStartTimeWithTwoWayTravel(Trip firstTripInternal, Driver driver) {
            return firstTripInternal.StartTime - TwoWayPayedTravelTimeFromHome(firstTripInternal, driver);
        }

        public static int ShiftEndTimeWithoutTwoWayTravel(Trip firstTripInternal, Trip lastTripInternal, Instance instance) {
            return lastTripInternal.EndTime + instance.CarTravelTimes[lastTripInternal.LastStation, firstTripInternal.FirstStation];
        }

        public static int ShiftLength(Trip firstTripInternal, Trip lastTripInternal, Driver driver, Instance instance) {
            return ShiftEndTimeWithoutTwoWayTravel(firstTripInternal, lastTripInternal, instance) - ShiftStartTimeWithTwoWayTravel(firstTripInternal, driver);
        }

        public static int RestTime(Trip shift1FirstTrip, Trip shift1LastTrip, Trip shift2FirstTrip, Driver driver, Instance instance) {
            return shift2FirstTrip.StartTime - shift1LastTrip.EndTime - CarTravelTime(shift1LastTrip, shift1FirstTrip, instance) - OneWayTravelTimeToHome(shift1LastTrip, driver) - OneWayTravelTimeFromHome(shift2FirstTrip, driver);
        }


        /* Base penalties */

        public static float GetPrecedenceBasePenalty(Trip trip1, Trip trip2, Instance instance, bool debugIsNew) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                if (debugIsNew) SaDebugger.GetCurrentNormalDiff().Precedence.AddNew((trip1, trip2));
                else SaDebugger.GetCurrentNormalDiff().Precedence.AddOld((trip1, trip2));
            }
            #endif

            if (instance.TripSuccession[trip1.Index, trip2.Index]) return 0;
            else return Config.PrecendenceViolationPenalty;
        }

        public static float GetShiftLengthBasePenalty(int shiftLength, bool debugIsNew) {
            int shiftLengthViolation = Math.Max(0, shiftLength - Config.MaxShiftLength);
            float amountBasePenalty = shiftLengthViolation * Config.ShiftLengthViolationPenaltyPerMin;
            float countBasePenalty = shiftLengthViolation > 0 ? Config.ShiftLengthViolationPenalty : 0;

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                if (debugIsNew) SaDebugger.GetCurrentNormalDiff().ShiftLength.AddNew(shiftLength);
                else SaDebugger.GetCurrentNormalDiff().ShiftLength.AddOld(shiftLength);
            }
            #endif

            return amountBasePenalty + countBasePenalty;
        }

        public static float GetRestTimeBasePenalty(Trip shift1FirstTrip, Trip shift1LastTrip, Trip shift2FirstTrip, Driver driver, Instance instance, bool debugIsNew) {
            int restTime = RestTime(shift1FirstTrip, shift1LastTrip, shift2FirstTrip, driver, instance);
            float shiftLengthViolation = Math.Max(0, Config.MinRestTime - restTime);
            float amountBasePenalty = shiftLengthViolation * Config.RestTimeViolationPenaltyPerMin;
            float countBasePenalty = shiftLengthViolation > 0 ? Config.RestTimeViolationPenalty : 0;

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                if (debugIsNew) SaDebugger.GetCurrentNormalDiff().RestTime.AddNew(restTime);
                else SaDebugger.GetCurrentNormalDiff().RestTime.AddOld(restTime);
            }
            #endif

            return amountBasePenalty + countBasePenalty;
        }

        public static float GetContractTimeBasePenaltyDiff(int oldWorkedTime, int newWorkedTime, Driver driver) {
            float contractTimeBasePenaltyDiff = 0;

            int oldContractTimeViolation = 0;
            if (oldWorkedTime < driver.MinContractTime) {
                oldContractTimeViolation += driver.MinContractTime - oldWorkedTime;
                contractTimeBasePenaltyDiff -= Config.ContractTimeViolationPenalty;
            } else if (oldWorkedTime > driver.MaxContractTime) {
                oldContractTimeViolation += oldWorkedTime - driver.MaxContractTime;
                contractTimeBasePenaltyDiff -= Config.ContractTimeViolationPenalty;
            }

            int newContractTimeViolation = 0;
            if (newWorkedTime < driver.MinContractTime) {
                newContractTimeViolation += driver.MinContractTime - newWorkedTime;
                contractTimeBasePenaltyDiff += Config.ContractTimeViolationPenalty;
            } else if (newWorkedTime > driver.MaxContractTime) {
                newContractTimeViolation += newWorkedTime - driver.MaxContractTime;
                contractTimeBasePenaltyDiff += Config.ContractTimeViolationPenalty;
            }

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentNormalDiff().ContractTime.Add(oldWorkedTime, newWorkedTime);
            }
            #endif

            contractTimeBasePenaltyDiff += (newContractTimeViolation - oldContractTimeViolation) * Config.ContractTimeViolationPenaltyPerMin;

            return contractTimeBasePenaltyDiff;
        }


        /* Getting trips */

        /** Returns previous trip of driver; returns as first of tuple when in same shift, or second of tuple when in previous shift */
        public static (Trip, Trip) GetPrevTrip(Trip trip, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance) {
            int startTimeThreshold = trip.StartTime - Config.BetweenShiftsMaxStartTimeDiff;
            for (int searchTripIndex = trip.Index - 1; searchTripIndex >= 0; searchTripIndex--) {
                Trip searchTrip = instance.Trips[searchTripIndex];
                if (searchTrip.StartTime < startTimeThreshold) return (null, null);

                // Check if this is the previous trip for this driver
                if (assignment[searchTripIndex] == driver && searchTrip != tripToIgnore) {
                    if (AreSameShift(searchTrip, trip, instance)) return (searchTrip, null); // Found trip is in current shift
                    return (null, searchTrip); // Found trip is in previous shift
                }
            }
            return (null, null);
        }

        /** Returns next trip of driver; returns as first of tuple when in same shift, or second of tuple when in next shift */
        public static (Trip, Trip) GetNextTrip(Trip trip, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance) {
            int startTimeThreshold = trip.StartTime + Config.BetweenShiftsMaxStartTimeDiff;
            for (int searchTripIndex = trip.Index + 1; searchTripIndex < instance.Trips.Length; searchTripIndex++) {
                Trip searchTrip = instance.Trips[searchTripIndex];
                if (searchTrip.StartTime > startTimeThreshold) return (null, null);

                // Check if this is the next trip for this driver
                if (assignment[searchTripIndex] == driver && searchTrip != tripToIgnore) {
                    if (AreSameShift(trip, searchTrip, instance)) return (searchTrip, null); // Found trip is in current shift
                    return (null, searchTrip); // Found trip is in next shift
                }
            }
            return (null, null);
        }

        /** Returns driver's first trip of shift, and last trip of previous shift */
        public static (Trip, Trip) GetFirstTripInternalAndPrevShiftTrip(Trip trip, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance) {
            Trip firstTripInternal = trip;
            int startTimeThreshold = firstTripInternal.StartTime - Config.ShiftMaxStartTimeDiff;
            for (int searchTripIndex = trip.Index - 1; searchTripIndex >= 0; searchTripIndex--) {
                Trip searchTrip = instance.Trips[searchTripIndex];
                if (searchTrip.StartTime < startTimeThreshold) {
                    return (firstTripInternal, null);
                }

                if (assignment[searchTripIndex] == driver && searchTrip != tripToIgnore) {
                    if (AreSameShift(searchTrip, firstTripInternal, instance)) {
                        firstTripInternal = searchTrip;
                        startTimeThreshold = firstTripInternal.StartTime - Config.ShiftMaxStartTimeDiff;
                    } else {
                        return (firstTripInternal, searchTrip);
                    }
                }
            }
            return (firstTripInternal, null);
        }

        /** Returns driver's last trip of shift, and first trip of next shift */
        public static (Trip, Trip) GetLastTripInternalAndNextShiftTrip(Trip trip, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance) {
            Trip lastTripInternal = trip;
            int startTimeThreshold = lastTripInternal.StartTime + Config.ShiftMaxStartTimeDiff;
            for (int searchTripIndex = trip.Index + 1; searchTripIndex < instance.Trips.Length; searchTripIndex++) {
                Trip searchTrip = instance.Trips[searchTripIndex];
                if (searchTrip.StartTime > startTimeThreshold) {
                    return (lastTripInternal, null);
                }

                if (assignment[searchTripIndex] == driver && searchTrip != tripToIgnore) {
                    if (AreSameShift(lastTripInternal, searchTrip, instance)) {
                        lastTripInternal = searchTrip;
                        startTimeThreshold = lastTripInternal.StartTime + Config.ShiftMaxStartTimeDiff;
                    } else {
                        return (lastTripInternal, searchTrip);
                    }
                }
            }
            return (lastTripInternal, null);
        }
    }
}

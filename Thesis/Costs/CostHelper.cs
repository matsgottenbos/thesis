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

        public static int WorkDayStartTimeWithTwoWayTravel(Trip firstDayTrip, Driver driver) {
            return firstDayTrip.StartTime - TwoWayPayedTravelTimeFromHome(firstDayTrip, driver);
        }

        public static int WorkDayEndTimeWithoutTwoWayTravel(Trip firstDayTrip, Trip lastDayTrip, Instance instance) {
            return lastDayTrip.EndTime + instance.CarTravelTimes[lastDayTrip.LastStation, firstDayTrip.FirstStation];
        }

        public static int ShiftLength(Trip firstDayTrip, Trip lastDayTrip, Driver driver, Instance instance) {
            return WorkDayEndTimeWithoutTwoWayTravel(firstDayTrip, lastDayTrip, instance) - WorkDayStartTimeWithTwoWayTravel(firstDayTrip, driver);
        }

        public static int RestTime(Trip shift1FirstTrip, Trip shift1LastTrip, Trip shift2FirstTrip, Driver driver, Instance instance) {
            return shift2FirstTrip.StartTime - shift1LastTrip.EndTime - CarTravelTime(shift1LastTrip, shift1FirstTrip, instance) - OneWayTravelTimeToHome(shift1LastTrip, driver) - OneWayTravelTimeFromHome(shift2FirstTrip, driver);
        }


        /* Base penalties */

        public static float GetPrecedenceBasePenalty(Trip trip1, Trip trip2, Instance instance) {
            if (instance.TripSuccession[trip1.Index, trip2.Index]) return 0;
            else return Config.PrecendenceViolationPenalty;
        }

        public static float GetShiftLengthBasePenaltyDiff(int oldShiftLength, int newShiftLength) {
            return GetShiftLengthPenaltyBase(newShiftLength) - GetShiftLengthPenaltyBase(oldShiftLength);
        }
        public static float GetShiftLengthPenaltyBase(int shiftLength) {
            int shiftLengthViolation = Math.Max(0, shiftLength - Config.MaxWorkDayLength);
            float amountPenaltyBase = shiftLengthViolation * Config.ShiftLengthViolationPenaltyPerMin;
            float countPenaltyBase = shiftLengthViolation > 0 ? Config.ShiftLengthViolationPenalty : 0;
            return amountPenaltyBase + countPenaltyBase;
        }

        public static float GetRestTimeBasePenalty(Trip shift1FirstTrip, Trip shift1LastTrip, Trip shift2FirstTrip, Driver driver, Instance instance, bool debugIsNew) {
            int restTime = RestTime(shift1FirstTrip, shift1LastTrip, shift2FirstTrip, driver, instance);
            float workDayLengthViolation = Math.Max(0, Config.MinRestTime - restTime);
            float amountPenaltyBase = workDayLengthViolation * Config.RestTimeViolationPenaltyPerMin;
            float countPenaltyBase = workDayLengthViolation > 0 ? Config.RestTimeViolationPenalty : 0;

            if (Config.DebugCheckAndLogOperations) {
                if (debugIsNew) SaDebugger.CurrentOperation.CurrentPart.RestTime.AddNew(restTime, driver);
                else SaDebugger.CurrentOperation.CurrentPart.RestTime.AddOld(restTime, driver);
            }

            return amountPenaltyBase + countPenaltyBase;
        }

        public static float GetContractTimeBasePenaltyDiff(int oldWorkedTime, int newWorkedTime, Driver driver) {
            float contractTimePenaltyBaseDiff = 0;

            int oldContractTimeViolation = 0;
            if (oldWorkedTime < driver.MinContractTime) {
                oldContractTimeViolation += driver.MinContractTime - oldWorkedTime;
                contractTimePenaltyBaseDiff -= Config.ContractTimeViolationPenalty;
            } else if (oldWorkedTime > driver.MaxContractTime) {
                oldContractTimeViolation += oldWorkedTime - driver.MaxContractTime;
                contractTimePenaltyBaseDiff -= Config.ContractTimeViolationPenalty;
            }

            int newContractTimeViolation = 0;
            if (newWorkedTime < driver.MinContractTime) {
                newContractTimeViolation += driver.MinContractTime - newWorkedTime;
                contractTimePenaltyBaseDiff += Config.ContractTimeViolationPenalty;
            } else if (newWorkedTime > driver.MaxContractTime) {
                newContractTimeViolation += newWorkedTime - driver.MaxContractTime;
                contractTimePenaltyBaseDiff += Config.ContractTimeViolationPenalty;
            }

            // Debug
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.CurrentOperation.CurrentPart.ContractTime.Add(oldWorkedTime, newWorkedTime, driver);
            }

            contractTimePenaltyBaseDiff += (newContractTimeViolation - oldContractTimeViolation) * Config.ContractTimeViolationPenaltyPerMin;

            return contractTimePenaltyBaseDiff;
        }


        /* Getting trips */

        /** Returns previous trip of driver; returns as first of tuple when in same shift, or second of tuple when in previous shift */
        public static (Trip, Trip) GetPrevTrip(Trip trip, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance) {
            int startTimeThreshold = trip.StartTime - Config.BetweenShiftsMaxStartTimeDiff;
            for (int searchTripIndex = trip.Index - 1; searchTripIndex >= 0; searchTripIndex--) {
                Trip searchTrip = instance.Trips[searchTripIndex];
                if (searchTrip.StartTime < startTimeThreshold) return (null, null);

                // Check if this is the previous trip in the same shift for this driver
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

                // Check if this is the same day trip after for this driver
                if (assignment[searchTripIndex] == driver && searchTrip != tripToIgnore) {
                    if (AreSameShift(trip, searchTrip, instance)) return (searchTrip, null); // Found trip is in current shift
                    return (null, searchTrip); // Found trip is in next shift
                }
            }
            return (null, null);
        }

        /** Returns driver's first trip of shift, and last trip of previous shift */
        public static (Trip, Trip) GetFirstTripInternalAndPrevShiftTrip(Trip trip, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance) {
            int startTimeThreshold = trip.StartTime - Config.ShiftMaxStartTimeDiff;
            Trip firstDayTrip = trip;
            for (int searchTripIndex = trip.Index - 1; searchTripIndex >= 0; searchTripIndex--) {
                Trip searchTrip = instance.Trips[searchTripIndex];
                if (searchTrip.StartTime < startTimeThreshold) {
                    return (firstDayTrip, null);
                }

                if (assignment[searchTripIndex] == driver && searchTrip != tripToIgnore) {
                    if (AreSameShift(searchTrip, firstDayTrip, instance)) {
                        firstDayTrip = searchTrip;
                    } else {
                        return (firstDayTrip, searchTrip);
                    }
                }
            }
            return (firstDayTrip, null);
        }

        /** Returns driver's last trip of shift, and first trip of next shift */
        public static (Trip, Trip) GetLastTripInternalAndNextShiftTrip(Trip trip, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance) {
            int startTimeThreshold = trip.StartTime + Config.ShiftMaxStartTimeDiff;
            Trip lastDayTrip = trip;
            for (int searchTripIndex = trip.Index + 1; searchTripIndex < instance.Trips.Length; searchTripIndex++) {
                Trip searchTrip = instance.Trips[searchTripIndex];
                if (searchTrip.StartTime > startTimeThreshold) {
                    return (lastDayTrip, null);
                }

                if (assignment[searchTripIndex] == driver && searchTrip != tripToIgnore) {
                    if (AreSameShift(lastDayTrip, searchTrip, instance)) {
                        lastDayTrip = searchTrip;
                    } else {
                        return (lastDayTrip, searchTrip);
                    }
                }
            }
            return (lastDayTrip, null);
        }
    }
}

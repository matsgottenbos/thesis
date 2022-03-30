using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class TravelHelper {
        public static (double, double, double, int) AddOrRemoveHotelStay(bool isAddition, Trip beforeHotelLastTrip, Driver driver, int driverOldWorkedTime, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string templateStr = isAddition ? "Add hotel stay to trip {0} with driver {1}" : "Remove hotel stay from trip {0} with driver {1}";
                SaDebugger.GetCurrentOperation().StartPart(string.Format(templateStr, beforeHotelLastTrip.Index, driver.GetId()), !isAddition, driver);
            }
            #endif

            // Get shift before the hotel stay
            (Trip beforeHotelFirstTrip, Trip beforeBeforeHotelLastTrip) = AssignmentHelper.GetFirstTripInternalAndPrevShiftTrip(beforeHotelLastTrip, driver, null, info);

            // Get shift after the hotel stay
            (Trip lastTripInternal, Trip afterHotelFirstTrip) = AssignmentHelper.GetLastTripInternalAndNextShiftTrip(beforeHotelLastTrip, driver, null, info);

            // If this trip is not the end of a shift, of there is no next shift, this hotel stay is invalid
            if (lastTripInternal != beforeHotelLastTrip || afterHotelFirstTrip == null) {
                float invalidHotelBasePenaltyDiff = Config.InvalidHotelPenalty;
                double invalidHotelCostDiff = invalidHotelBasePenaltyDiff * info.PenaltyFactor;
                if (isAddition) return (invalidHotelCostDiff, 0, invalidHotelBasePenaltyDiff, 0);
                else return (-invalidHotelCostDiff, 0, -invalidHotelBasePenaltyDiff, 0);
            }

            // Get shift after the shift after the hotel stay
            (Trip afterHotelLastTrip, Trip afterAfterHotelFirstTrip) = AssignmentHelper.GetLastTripInternalAndNextShiftTrip(afterHotelFirstTrip, driver, null, info);

            /* If no before/after hotel stays */
            // Old: home > beforeHotelFirstTrip > beforeHotelLastTrip > beforeHotelFirstTrip > home > afterHotelFirstTrip > afterHotelLastTrip > afterHotelFirstTrip  > home > afterAfterHotelFirstTrip
            // New: home > beforeHotelFirstTrip > beforeHotelLastTrip > hotel                       > afterHotelFirstTrip > afterHotelLastTrip > beforeHotelFirstTrip > home > afterAfterHotelFirstTrip

            /* If before/after hotel stays */
            // Old: beforeBeforeHotelLastTrip > hotel > beforeHotelFirstTrip > [...] > afterHotelLastTrip > hotel > afterAfterHotelFirstTrip > ... > beforeBeforeNextPickupFirstTrip > hotel > beforeNextPickupFirstTrip > beforeNextPickupLastTrip > afterHotelFirstTrip  > home > afterNextPickupFirstTrip
            // New: beforeBeforeHotelLastTrip > hotel > beforeHotelFirstTrip > [...] > afterHotelLastTrip > hotel > afterAfterHotelFirstTrip > ... > beforeBeforeNextPickupFirstTrip > hotel > beforeNextPickupFirstTrip > beforeNextPickupLastTrip > beforeHotelFirstTrip > home > afterNextPickupFirstTrip

            // Init diff variables
            int addHotelTravelTimeDiff = 0;
            double addHotelBasePenaltyDiff = 0;

            // Get half travel time via new hotel
            int halfTravelTimeViaNewHotel = info.Instance.HalfTravelTimeViaHotel(beforeHotelLastTrip, afterHotelFirstTrip);

            /* Shift before hotel */
            // Get travel time at start of before hotel shift
            int beforeHotelTravelTimeBefore;
            if (beforeBeforeHotelLastTrip != null && info.IsHotelStayAfterTrip[beforeBeforeHotelLastTrip.Index]) {
                // There is a hotel stay before
                beforeHotelTravelTimeBefore = info.Instance.HalfTravelTimeViaHotel(beforeBeforeHotelLastTrip, beforeHotelFirstTrip);
            } else {
                // There is no hotel stay before
                beforeHotelTravelTimeBefore = driver.HomeTravelTimeToStart(beforeHotelFirstTrip);
            }

            // Get before hotel shift length
            int beforeHotelDrivingTime = driver.DrivingTime(beforeHotelFirstTrip, beforeHotelLastTrip);
            int beforeHotelOldTravelTimeAfter = info.Instance.CarTravelTime(beforeHotelLastTrip, beforeHotelFirstTrip) + driver.HomeTravelTimeToStart(beforeHotelFirstTrip);
            int beforeHotelDrivingAndTravelTimeAfter = beforeHotelTravelTimeBefore + beforeHotelDrivingTime;
            int beforeHotelOldShiftLength = beforeHotelOldTravelTimeAfter + beforeHotelDrivingAndTravelTimeAfter;
            int beforeHotelNewShiftLength = halfTravelTimeViaNewHotel + beforeHotelDrivingAndTravelTimeAfter;
            addHotelTravelTimeDiff += beforeHotelNewShiftLength - beforeHotelOldShiftLength;
            addHotelBasePenaltyDiff += PenaltyHelper.GetShiftLengthBasePenalty(beforeHotelNewShiftLength, true) - PenaltyHelper.GetShiftLengthBasePenalty(beforeHotelOldShiftLength, false);

            /* Shift after hotel */
            // Get travel time at end of after hotel shift
            int afterHotelOldTravelTimeAfter, afterHotelNewTravelTimeAfter;
            if (afterAfterHotelFirstTrip != null && info.IsHotelStayAfterTrip[afterHotelLastTrip.Index]) {
                // There is a hotel stay after
                afterHotelOldTravelTimeAfter = afterHotelNewTravelTimeAfter = info.Instance.HalfTravelTimeViaHotel(afterHotelLastTrip, afterAfterHotelFirstTrip);
            } else {
                // There is no hotel stay after
                afterHotelOldTravelTimeAfter = info.Instance.CarTravelTime(afterHotelLastTrip, afterHotelFirstTrip) + driver.HomeTravelTimeToStart(afterHotelFirstTrip);
                afterHotelNewTravelTimeAfter = info.Instance.CarTravelTime(afterHotelLastTrip, beforeHotelFirstTrip) + driver.HomeTravelTimeToStart(beforeHotelFirstTrip);
            }

            // Get after hotel shift length
            int afterHotelOldTravelTimeBefore = driver.HomeTravelTimeToStart(afterHotelFirstTrip);
            int afterHotelDrivingTime = driver.DrivingTime(afterHotelFirstTrip, afterHotelLastTrip);
            int afterHotelOldShiftLength = afterHotelOldTravelTimeBefore + afterHotelDrivingTime + afterHotelOldTravelTimeAfter;
            int afterHotelNewShiftLength = halfTravelTimeViaNewHotel + afterHotelDrivingTime + afterHotelNewTravelTimeAfter;
            addHotelTravelTimeDiff += afterHotelNewShiftLength - afterHotelOldShiftLength;
            addHotelBasePenaltyDiff += PenaltyHelper.GetShiftLengthBasePenalty(afterHotelNewShiftLength, true) - PenaltyHelper.GetShiftLengthBasePenalty(afterHotelOldShiftLength, false);

            /* Hotel rest time */
            int hotelRestTimeWithoutTravel = afterHotelFirstTrip.StartTime - beforeHotelLastTrip.EndTime;
            int oldHotelRestTime = hotelRestTimeWithoutTravel - beforeHotelOldTravelTimeAfter - afterHotelOldTravelTimeBefore;
            int newHotelRestTime = hotelRestTimeWithoutTravel - info.Instance.TravelTimeViaHotel(beforeHotelLastTrip, afterHotelFirstTrip);
            addHotelBasePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(newHotelRestTime, true) - PenaltyHelper.GetRestTimeBasePenalty(oldHotelRestTime, false);

            if (info.IsHotelStayAfterTrip[afterHotelLastTrip.Index]) {
                // There is a hotel stay after

                // Get shifts before and after next car pickup
                (Trip beforeBeforeNextPickupLastTrip, Trip beforeNextPickupFirstTrip, Trip beforeNextPickupLastTrip, Trip afterNextPickupFirstTrip) = GetNextPickupShift(beforeHotelLastTrip, afterHotelFirstTrip, driver, info);
                if (beforeNextPickupFirstTrip == null) throw new NotImplementedException("Penalty");

                // Get before pickup shift length
                int beforePickupTravelTimeBefore = info.Instance.HalfTravelTimeViaHotel(beforeBeforeNextPickupLastTrip, beforeNextPickupFirstTrip);
                int beforePickupDrivingTime = driver.DrivingTime(beforeNextPickupFirstTrip, beforeNextPickupLastTrip);
                int beforePickupDrivingAndTravelTimeAfter = beforePickupTravelTimeBefore + beforePickupDrivingTime;
                int beforePickupOldTravelTimeAfter = info.Instance.CarTravelTime(beforeNextPickupLastTrip, afterHotelFirstTrip) + driver.HomeTravelTimeToStart(afterHotelFirstTrip);
                int beforePickupNewTravelTimeAfter = info.Instance.CarTravelTime(beforeNextPickupLastTrip, beforeHotelFirstTrip) + driver.HomeTravelTimeToStart(beforeHotelFirstTrip);
                int beforePickupOldShiftLength = beforePickupDrivingAndTravelTimeAfter + beforePickupOldTravelTimeAfter;
                int beforePickupNewShiftLength = beforePickupDrivingAndTravelTimeAfter + beforePickupNewTravelTimeAfter;
                addHotelTravelTimeDiff += beforePickupNewShiftLength - beforePickupOldShiftLength;
                addHotelBasePenaltyDiff += PenaltyHelper.GetShiftLengthBasePenalty(beforePickupNewShiftLength, true) - PenaltyHelper.GetShiftLengthBasePenalty(beforePickupOldShiftLength, false);

                // Check rest time before-after pickup
                if (afterNextPickupFirstTrip != null) {
                    int pickupOldRestTime = afterNextPickupFirstTrip.StartTime - afterHotelLastTrip.EndTime - beforePickupTravelTimeBefore - beforePickupOldTravelTimeAfter;
                    int pickupNewRestTime = afterNextPickupFirstTrip.StartTime - beforeHotelFirstTrip.EndTime - beforePickupTravelTimeBefore - beforePickupNewTravelTimeAfter;
                    addHotelBasePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(pickupNewRestTime, true) - PenaltyHelper.GetRestTimeBasePenalty(pickupOldRestTime, false);
                }
            } else if (afterAfterHotelFirstTrip != null) {
                // There is no hotel stay after, but there is a second shift after

                // Check rest time after-after2 hotel
                int afterHotelOldRestTime = afterAfterHotelFirstTrip.StartTime - afterHotelLastTrip.EndTime - info.Instance.TravelTimeViaHotel(afterHotelLastTrip, afterAfterHotelFirstTrip);
                int afterHotelNewRestTime = afterAfterHotelFirstTrip.StartTime - beforeHotelFirstTrip.EndTime - info.Instance.TravelTimeViaHotel(beforeHotelFirstTrip, afterAfterHotelFirstTrip);
                addHotelBasePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(afterHotelNewRestTime, true) - PenaltyHelper.GetRestTimeBasePenalty(afterHotelOldRestTime, false);
            }

            // Get correct values for this type of operation
            int travelTimeDiff;
            double basePenaltyDiff;
            if (isAddition) {
                travelTimeDiff = addHotelTravelTimeDiff;
                basePenaltyDiff = addHotelBasePenaltyDiff;
            } else {
                travelTimeDiff = -addHotelTravelTimeDiff;
                basePenaltyDiff = -addHotelBasePenaltyDiff;
            }

            // Contract time penalty
            basePenaltyDiff += driver.GetContractTimeBasePenalty(driverOldWorkedTime + travelTimeDiff, true) - driver.GetContractTimeBasePenalty(driverOldWorkedTime, false);

            // Determine cost diffs
            double costDiffWithoutPenalty = travelTimeDiff * Config.InternalDriverTravelSalaryRate;
            double costDiff = costDiffWithoutPenalty + basePenaltyDiff;

            return (costDiff, costDiffWithoutPenalty, basePenaltyDiff, travelTimeDiff);
        }

        /** Get the trip after which the car is picked up, ignoring the current shift; returns null if this is the last shift */
        static (Trip, Trip, Trip, Trip) GetNextPickupShift(Trip beforeHotelLastTrip, Trip afterHotelFirstTrip, Driver driver, SaInfo info) {
            Trip prevShiftLastTrip = beforeHotelLastTrip;
            Trip firstTripInternal = afterHotelFirstTrip;
            Trip lastTripInternal, nextShiftFirstTrip;
            while (true) {
                // Go to the next shift
                (lastTripInternal, nextShiftFirstTrip) = AssignmentHelper.GetLastTripInternalAndNextShiftTrip(firstTripInternal, driver, null, info);

                // If there is no next shift, or the next shift isn't linked, this is the pick-up trip
                if (nextShiftFirstTrip == null || !info.IsHotelStayAfterTrip[nextShiftFirstTrip.Index]) break;

                prevShiftLastTrip = lastTripInternal;
                firstTripInternal = nextShiftFirstTrip;
            }
            return (prevShiftLastTrip, firstTripInternal, lastTripInternal, nextShiftFirstTrip);
        }
    }
}

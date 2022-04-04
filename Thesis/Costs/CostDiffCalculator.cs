using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class CostDiffCalculator {
        public static (double, double, double, int) GetDriverCostDiff(Trip unassignedTrip, Trip assignedTrip, Trip addedHotel, Trip removedHotel, Driver driver, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string description = string.Format("Driver {0}: ", driver.GetId());
                if (unassignedTrip != null) description += string.Format("Unassign trip {0}; ", unassignedTrip.Index);
                if (assignedTrip != null) description += string.Format("Assign trip {0}; ", assignedTrip.Index);
                if (addedHotel != null) description += string.Format("Add hotel stay after trip {0}; ", addedHotel.Index);
                if (removedHotel != null) description += string.Format("Remove hotel stay after trip {0}; ", removedHotel.Index);
                SaDebugger.GetCurrentOperation().StartPart(description, false, driver);
            }
            #endif

            // Skip till first trip of driver
            Trip driverOldFirstTrip = null;
            Trip driverNewFirstTrip = null;
            for (int driverFirstTripIndex = 0; driverFirstTripIndex < info.Assignment.Length; driverFirstTripIndex++) {
                Driver searchTripDriver = info.Assignment[driverFirstTripIndex];
                if (searchTripDriver == driver) {
                    if (driverOldFirstTrip == null) driverOldFirstTrip = info.Instance.Trips[driverFirstTripIndex];
                    if (driverNewFirstTrip == null && (unassignedTrip == null || driverFirstTripIndex != unassignedTrip.Index)) {
                        driverNewFirstTrip = info.Instance.Trips[driverFirstTripIndex];
                        break;
                    }
                } else if (assignedTrip != null && driverFirstTripIndex == assignedTrip.Index) {
                    driverNewFirstTrip = info.Instance.Trips[driverFirstTripIndex];
                }
            }

            // Get old driver cost
            double oldCostWithoutPenalty = 0;
            double oldBasePenalty = 0;
            int oldWorkedTime = 0;
            if (driverOldFirstTrip != null) {
                Trip oldShiftFirstTrip = driverOldFirstTrip;
                Trip oldParkingTrip = driverOldFirstTrip;
                Trip oldPrevTrip = driverOldFirstTrip;
                Trip oldBeforeHotelTrip = null;
                for (int tripIndex = driverOldFirstTrip.Index + 1; tripIndex < info.Assignment.Length; tripIndex++) {
                    Driver searchTripDriver = info.Assignment[tripIndex];
                    if (searchTripDriver != driver) continue;

                    Trip searchTrip = info.Instance.Trips[tripIndex];
                    ProcessDriverTrip(searchTrip, ref oldShiftFirstTrip, ref oldParkingTrip, ref oldPrevTrip, ref oldBeforeHotelTrip, ref oldCostWithoutPenalty, ref oldBasePenalty, ref oldWorkedTime, null, null, driver, info, false);
                }
                ProcessLastDriverShift(oldShiftFirstTrip, oldParkingTrip, oldPrevTrip, ref oldCostWithoutPenalty, ref oldBasePenalty, ref oldWorkedTime, null, null, driver, info, false);
            }
            oldBasePenalty += driver.GetContractTimeBasePenalty(oldWorkedTime, false);
            double oldCost = oldCostWithoutPenalty + oldBasePenalty * info.PenaltyFactor;

            // Get new driver cost
            double newCostWithoutPenalty = 0;
            double newBasePenalty = 0;
            int newWorkedTime = 0;
            if (driverNewFirstTrip != null) {
                Trip newShiftFirstTrip = driverNewFirstTrip;
                Trip newParkingTrip = driverNewFirstTrip;
                Trip newPrevTrip = driverNewFirstTrip;
                Trip newBeforeHotelTrip = null;
                for (int tripIndex = driverNewFirstTrip.Index + 1; tripIndex < info.Assignment.Length; tripIndex++) {
                    Driver searchTripDriver = info.Assignment[tripIndex];
                    Trip searchTrip = info.Instance.Trips[tripIndex];
                    if (searchTripDriver != driver && searchTrip != assignedTrip || searchTrip == unassignedTrip) continue;

                    ProcessDriverTrip(searchTrip, ref newShiftFirstTrip, ref newParkingTrip, ref newPrevTrip, ref newBeforeHotelTrip, ref newCostWithoutPenalty, ref newBasePenalty, ref newWorkedTime, addedHotel, removedHotel, driver, info, true);
                }
                ProcessLastDriverShift(newShiftFirstTrip, newParkingTrip, newPrevTrip, ref newCostWithoutPenalty, ref newBasePenalty, ref newWorkedTime, addedHotel, removedHotel, driver, info, true);
            }
            newBasePenalty += driver.GetContractTimeBasePenalty(newWorkedTime, true);
            double newCost = newCostWithoutPenalty + newBasePenalty * info.PenaltyFactor;

            // Get diffs
            double costDiff = newCost - oldCost;
            double costWithoutPenaltyDiff = newCostWithoutPenalty - oldCostWithoutPenalty;
            double basePenaltyDiff = newBasePenalty - oldBasePenalty;
            int workedTimeDiff = newWorkedTime - oldWorkedTime;

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentNormalDiff().CostDiff = costDiff;
                SaDebugger.GetCurrentNormalDiff().CostWithoutPenaltyDiff = costWithoutPenaltyDiff;
                SaDebugger.GetCurrentNormalDiff().BasePenaltyDiff = basePenaltyDiff;

                CheckErrors(unassignedTrip, assignedTrip, addedHotel, removedHotel, driver, info);
            }
            #endif

            return (costDiff, costWithoutPenaltyDiff, basePenaltyDiff, workedTimeDiff);
        }

        static void ProcessDriverTrip(Trip searchTrip, ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, ref double costWithoutPenalty, ref double basePenalty, ref int workedTime, Trip addedHotel, Trip removedHotel, Driver driver, SaInfo info, bool debugIsNew) {
            if (info.Instance.AreSameShift(prevTrip, searchTrip)) {
                /* Same shift */
                // Check precedence
                basePenalty += PenaltyHelper.GetPrecedenceBasePenalty(prevTrip, searchTrip, info, debugIsNew);

                // Check for invalid hotel stay
                if (IsHotelAfter(prevTrip, addedHotel, removedHotel, info)) {
                    basePenalty += PenaltyHelper.GetHotelBasePenalty(prevTrip, info, debugIsNew);
                }
            } else {
                /* Start of new shift */
                // Get travel time before
                int travelTimeBefore;
                if (beforeHotelTrip == null) {
                    // No hotel stay before
                    travelTimeBefore = driver.HomeTravelTimeToStart(shiftFirstTrip);
                } else {
                    // Hotel stay before
                    travelTimeBefore = info.Instance.HalfTravelTimeViaHotel(beforeHotelTrip, shiftFirstTrip);
                }

                // Get driving time
                int shiftLengthWithoutTravel = driver.DrivingTime(shiftFirstTrip, prevTrip);
                float drivingCost = driver.DrivingCost(shiftFirstTrip, prevTrip);
                workedTime += shiftLengthWithoutTravel;

                // Get travel time after and rest time
                int travelTimeAfter, restTime;
                if (IsHotelAfter(prevTrip, addedHotel, removedHotel, info)) {
                    // Hotel stay after
                    travelTimeAfter = info.Instance.HalfTravelTimeViaHotel(prevTrip, searchTrip);
                    restTime = info.Instance.RestTimeViaHotel(prevTrip, searchTrip);
                    costWithoutPenalty += Config.HotelCosts;

                    // Check if the hotel stay isn't too long
                    if (restTime > Config.HotelMaxRestTime) {
                        basePenalty += PenaltyHelper.GetHotelBasePenalty(prevTrip, info, debugIsNew);
                    }

                    beforeHotelTrip = prevTrip;
                } else {
                    // No hotel stay after
                    travelTimeAfter = info.Instance.CarTravelTime(prevTrip, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);
                    restTime = info.Instance.RestTimeWithTravelTime(prevTrip, searchTrip, travelTimeAfter + driver.HomeTravelTimeToStart(searchTrip));

                    // Set new parking trip
                    parkingTrip = searchTrip;
                    beforeHotelTrip = null;
                }

                // Get shift length
                int travelTime = travelTimeBefore + travelTimeAfter;
                int shiftLengthWithTravel = shiftLengthWithoutTravel + travelTime;

                // Get shift cost
                float travelCost = driver.GetPayedTravelCost(travelTime);
                float shiftCost = drivingCost + travelCost;
                costWithoutPenalty += shiftCost;

                // Check shift length
                basePenalty += PenaltyHelper.GetShiftLengthBasePenalty(shiftLengthWithoutTravel, shiftLengthWithTravel, debugIsNew);

                // Check rest time
                basePenalty += PenaltyHelper.GetRestTimeBasePenalty(restTime, debugIsNew);

                // Start new shift
                shiftFirstTrip = searchTrip;
            }

            prevTrip = searchTrip;
        }

        static void ProcessLastDriverShift(Trip shiftFirstTrip, Trip parkingTrip, Trip prevTrip, ref double costWithoutPenalty, ref double basePenalty, ref int workedTime, Trip addedHotel, Trip removedHotel, Driver driver, SaInfo info, bool debugIsNew) {
            // End final shift
            (int shiftLengthWithoutTravel, int shiftLengthWithTravel) = driver.ShiftLengthWithCustomPickup(shiftFirstTrip, prevTrip, parkingTrip);
            workedTime += shiftLengthWithoutTravel;
            costWithoutPenalty += driver.ShiftCostWithCustomPickup(shiftFirstTrip, prevTrip, parkingTrip);
            basePenalty += PenaltyHelper.GetShiftLengthBasePenalty(shiftLengthWithoutTravel, shiftLengthWithTravel, debugIsNew);

            // Check for invalid final hotel stay
            if (IsHotelAfter(prevTrip, addedHotel, removedHotel, info)) {
                basePenalty += PenaltyHelper.GetHotelBasePenalty(prevTrip, info, debugIsNew);
            }
        }

        static bool IsHotelAfter(Trip trip, Trip addedHotel, Trip removedHotel, SaInfo info) {
            return trip == addedHotel || trip != removedHotel && info.IsHotelStayAfterTrip[trip.Index];
        }

        static void CheckErrors(Trip unassignedTrip, Trip assignedTrip, Trip addedHotel, Trip removedHotel, Driver driver, SaInfo info) {
            List<Trip> driverPathBefore = TotalCostCalculator.GetSingleDriverPath(driver, null, info);

            // Get total before
            TotalCostCalculator.GetDriverPathCost(driverPathBefore, info.IsHotelStayAfterTrip, driver, info);
            SaDebugger.GetCurrentOperationPart().FinishCheckBefore();

            // Get driver path after
            List<Trip> driverPathAfter = driverPathBefore.Copy();
            if (unassignedTrip != null) {
                int removedCount = driverPathAfter.RemoveAll(searchTrip => searchTrip.Index == unassignedTrip.Index);
                if (removedCount != 1) throw new Exception("Error removing trip from driver path");
            }
            if (assignedTrip != null) {
                driverPathAfter.Add(assignedTrip);
                driverPathAfter = driverPathAfter.OrderBy(searchTrip => searchTrip.StartTime).ToList();
            }

            // Get hotel stays after
            bool[] isHotelStayAfterTripAfter = info.IsHotelStayAfterTrip.Copy();
            if (addedHotel != null) isHotelStayAfterTripAfter[addedHotel.Index] = true;
            if (removedHotel != null) isHotelStayAfterTripAfter[removedHotel.Index] = false;

            // Get total after
            TotalCostCalculator.GetDriverPathCost(driverPathAfter, isHotelStayAfterTripAfter, driver, info);
            SaDebugger.GetCurrentOperationPart().FinishCheckAfter();

            // Check for errors
            SaDebugger.GetCurrentOperationPart().CheckErrors();
        }
    }
}

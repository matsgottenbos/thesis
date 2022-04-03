using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class CostDiffCalculator2 {
        public static (double, double, double, int) GetDriverCostDiff(Trip unassignedTrip, Trip assignedTrip, Driver driver, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string description;
                if (unassignedTrip != null && assignedTrip != null) description = string.Format("Unassign trip {0} and assign trip {1} to driver {2}", unassignedTrip.Index, assignedTrip.Index, driver.GetId());
                else if (unassignedTrip != null) description = string.Format("Unassign trip {0} from driver {1}", unassignedTrip.Index, driver.GetId());
                else if (assignedTrip != null) description = string.Format("Assign trip {0} to driver {1}", assignedTrip.Index, driver.GetId());
                else throw new Exception("Requesting cost diff calculation without change");
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
                    ProcessDriverTrip(searchTrip, ref oldShiftFirstTrip, ref oldParkingTrip, ref oldPrevTrip, ref oldBeforeHotelTrip, ref oldCostWithoutPenalty, ref oldBasePenalty, ref oldWorkedTime, driver, info, false);
                }
                ProcessLastDriverShift(oldShiftFirstTrip, oldParkingTrip, oldPrevTrip, ref oldCostWithoutPenalty, ref oldBasePenalty, ref oldWorkedTime, driver, info, false);
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

                    ProcessDriverTrip(searchTrip, ref newShiftFirstTrip, ref newParkingTrip, ref newPrevTrip, ref newBeforeHotelTrip, ref newCostWithoutPenalty, ref newBasePenalty, ref newWorkedTime, driver, info, true);
                }
                ProcessLastDriverShift(newShiftFirstTrip, newParkingTrip, newPrevTrip, ref newCostWithoutPenalty, ref newBasePenalty, ref newWorkedTime, driver, info, true);
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

                CheckErrors(unassignedTrip, assignedTrip, driver, info);
            }
            #endif

            return (costDiff, costWithoutPenaltyDiff, basePenaltyDiff, workedTimeDiff);
        }

        static void ProcessDriverTrip(Trip searchTrip, ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, ref double costWithoutPenalty, ref double basePenalty, ref int workedTime, Driver driver, SaInfo info, bool debugIsNew) {
            if (info.Instance.AreSameShift(prevTrip, searchTrip)) {
                /* Same shift */
                // Check precedence
                basePenalty += PenaltyHelper.GetPrecedenceBasePenalty(prevTrip, searchTrip, info, debugIsNew);

                // Check for invalid hotel stay
                if (info.IsHotelStayAfterTrip[prevTrip.Index]) {
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
                int drivingTime = driver.DrivingTime(shiftFirstTrip, prevTrip);
                float drivingCost = driver.DrivingCost(shiftFirstTrip, prevTrip);

                // Get travel time after and rest time
                int travelTimeAfter, restTime;
                if (info.IsHotelStayAfterTrip[prevTrip.Index]) {
                    // Hotel stay after
                    travelTimeAfter = info.Instance.HalfTravelTimeViaHotel(prevTrip, searchTrip);
                    restTime = searchTrip.StartTime - prevTrip.EndTime - info.Instance.TravelTimeViaHotel(prevTrip, searchTrip);

                    beforeHotelTrip = prevTrip;
                } else {
                    // No hotel stay after
                    travelTimeAfter = info.Instance.CarTravelTime(prevTrip, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);
                    restTime = searchTrip.StartTime - prevTrip.EndTime - travelTimeAfter - driver.HomeTravelTimeToStart(searchTrip);

                    // Set new parking trip
                    parkingTrip = searchTrip;
                    beforeHotelTrip = null;
                }

                // Get shift length
                int travelTime = travelTimeBefore + travelTimeAfter;
                int shiftLength = drivingTime + travelTime;
                workedTime += shiftLength;

                // Get shift cost
                float travelCost = driver.GetPayedTravelCost(travelTime);
                float shiftCost = drivingCost + travelCost;
                costWithoutPenalty += shiftCost;

                // Check shift length
                basePenalty += PenaltyHelper.GetShiftLengthBasePenalty(shiftLength, debugIsNew);

                // Check rest time
                basePenalty += PenaltyHelper.GetRestTimeBasePenalty(restTime, debugIsNew);

                // Start new shift
                shiftFirstTrip = searchTrip;
            }

            prevTrip = searchTrip;
        }

        static void ProcessLastDriverShift(Trip shiftFirstTrip, Trip parkingTrip, Trip prevTrip, ref double costWithoutPenalty, ref double basePenalty, ref int workedTime, Driver driver, SaInfo info, bool debugIsNew) {
            // End final shift
            int shiftLength = driver.ShiftLengthWithCustomPickup(shiftFirstTrip, prevTrip, parkingTrip);
            workedTime += shiftLength;
            costWithoutPenalty += driver.ShiftCostWithCustomPickup(shiftFirstTrip, prevTrip, parkingTrip);
            basePenalty += PenaltyHelper.GetShiftLengthBasePenalty(shiftLength, debugIsNew);

            // Check for invalid final hotel stay
            if (info.IsHotelStayAfterTrip[prevTrip.Index]) {
                basePenalty += PenaltyHelper.GetHotelBasePenalty(prevTrip, info, debugIsNew);
            }
        }

        static void CheckErrors(Trip unassignedTrip, Trip assignedTrip, Driver driver, SaInfo info) {
            List<Trip> driverPathBefore = TotalCostCalculator.GetSingleDriverPath(driver, null, info);

            // Get total before
            TotalCostCalculator.GetDriverPathCost(driverPathBefore, driver, info);
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

            // Get total after
            TotalCostCalculator.GetDriverPathCost(driverPathAfter, driver, info);
            SaDebugger.GetCurrentOperationPart().FinishCheckAfter();

            // Check for errors
            SaDebugger.GetCurrentOperationPart().CheckErrors();
        }
    }
}

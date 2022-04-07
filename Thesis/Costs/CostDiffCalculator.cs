using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class CostDiffCalculator {
        public static (double, double, double, int) GetDriverCostDiff(Trip unassignedTrip, Trip assignedTrip, Trip addedHotelTrip, Trip removedHotelTrip, Driver driver, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string description = string.Format("Driver {0}: ", driver.GetId());
                if (unassignedTrip != null) description += string.Format("Unassign trip {0}; ", unassignedTrip.Index);
                if (assignedTrip != null) description += string.Format("Assign trip {0}; ", assignedTrip.Index);
                if (addedHotelTrip != null) description += string.Format("Add hotel stay after trip {0}; ", addedHotelTrip.Index);
                if (removedHotelTrip != null) description += string.Format("Remove hotel stay after trip {0}; ", removedHotelTrip.Index);
                SaDebugger.GetCurrentOperation().StartPart(description, false, driver);
            }
            #endif

            Driver[] assignment = info.Assignment;
            Instance instance = info.Instance;
            Trip[] trips = instance.Trips;

            // Get first trip of driver
            Trip driverOldFirstTrip = null;
            Trip driverNewFirstTrip = null;
            for (int searchTripIndex = 0; searchTripIndex < assignment.Length; searchTripIndex++) {
                Driver searchTripDriver = assignment[searchTripIndex];
                if (searchTripDriver == driver) {
                    if (driverOldFirstTrip == null) {
                        driverOldFirstTrip = trips[searchTripIndex];
                    }
                    if (driverNewFirstTrip == null) {
                        if (unassignedTrip == null || searchTripIndex != unassignedTrip.Index) {
                            driverNewFirstTrip = trips[searchTripIndex];
                            break;
                        }
                    } else {
                        break;
                    }
                } else if (assignedTrip != null && searchTripIndex == assignedTrip.Index) {
                    driverNewFirstTrip = trips[searchTripIndex];
                }
            }

            // Get last changed trip
            int lastChangedTripIndex = 0;
            if (assignedTrip != null) lastChangedTripIndex = Math.Max(lastChangedTripIndex, assignedTrip.Index);
            if (unassignedTrip != null) lastChangedTripIndex = Math.Max(lastChangedTripIndex, unassignedTrip.Index);
            if (addedHotelTrip != null) lastChangedTripIndex = Math.Max(lastChangedTripIndex, addedHotelTrip.Index);
            if (removedHotelTrip != null) lastChangedTripIndex = Math.Max(lastChangedTripIndex, removedHotelTrip.Index);

            // Get last relevant trip of driver
            Trip driverLastRelevantTrip = trips[lastChangedTripIndex];
            for (int searchTripIndex = lastChangedTripIndex + 1; searchTripIndex < assignment.Length; searchTripIndex++) {
                Driver searchTripDriver = assignment[searchTripIndex];
                if (searchTripDriver == driver) {
                    Trip searchTrip = trips[searchTripIndex];
                    if (driverLastRelevantTrip != assignedTrip && driverLastRelevantTrip != unassignedTrip && searchTrip != unassignedTrip && !instance.AreSameShift(driverLastRelevantTrip, searchTrip) && !info.IsHotelStayAfterTrip[driverLastRelevantTrip.Index] && driverLastRelevantTrip != addedHotelTrip) {
                        // This is the last relevant trip for this driver
                        driverLastRelevantTrip = searchTrip;
                        break;
                    }
                    driverLastRelevantTrip = searchTrip;
                }
            }

            // Get old driver cost
            double oldPartialCostWithoutPenalty = 0;
            double oldPartialBasePenalty = 0;
            int oldPartialWorkedTime = 0;
            if (driverOldFirstTrip != null) {
                Trip oldShiftFirstTrip = driverOldFirstTrip;
                Trip oldParkingTrip = driverOldFirstTrip;
                Trip oldPrevTrip = driverOldFirstTrip;
                Trip oldBeforeHotelTrip = null;
                int tripIndex;
                for (tripIndex = driverOldFirstTrip.Index + 1; tripIndex < trips.Length; tripIndex++) {
                    Driver searchTripDriver = assignment[tripIndex];
                    if (searchTripDriver != driver) continue;

                    Trip searchTrip = trips[tripIndex];
                    ProcessDriverTrip(searchTrip, ref oldShiftFirstTrip, ref oldParkingTrip, ref oldPrevTrip, ref oldBeforeHotelTrip, ref oldPartialCostWithoutPenalty, ref oldPartialBasePenalty, ref oldPartialWorkedTime, null, null, driver, info, instance, false);

                    if (searchTrip.Index > driverLastRelevantTrip.Index) {
                        // Further trips are no longer relevant
                        break;
                    }
                }
                if (tripIndex == trips.Length) {
                    // Last relevant trip was the last trip in the driver path, so finish the last shift
                    ProcessLastDriverShift(oldShiftFirstTrip, oldParkingTrip, oldPrevTrip, ref oldPartialCostWithoutPenalty, ref oldPartialBasePenalty, ref oldPartialWorkedTime, null, null, driver, info, false);
                }
            }
            int driverOldWorkedTime = info.DriversWorkedTime[driver.AllDriversIndex];
            oldPartialBasePenalty += driver.GetContractTimeBasePenalty(driverOldWorkedTime, false);
            double oldCost = oldPartialCostWithoutPenalty + oldPartialBasePenalty * info.PenaltyFactor;

            // Get new driver cost
            double newPartialCostWithoutPenalty = 0;
            double newPartialBasePenalty = 0;
            int newPartialWorkedTime = 0;
            if (driverNewFirstTrip != null) {
                Trip newShiftFirstTrip = driverNewFirstTrip;
                Trip newParkingTrip = driverNewFirstTrip;
                Trip newPrevTrip = driverNewFirstTrip;
                Trip newBeforeHotelTrip = null;
                int tripIndex;
                for (tripIndex = driverNewFirstTrip.Index + 1; tripIndex < trips.Length; tripIndex++) {
                    Driver searchTripDriver = assignment[tripIndex];
                    Trip searchTrip = trips[tripIndex];
                    if (searchTripDriver != driver && searchTrip != assignedTrip || searchTrip == unassignedTrip) continue;

                    ProcessDriverTrip(searchTrip, ref newShiftFirstTrip, ref newParkingTrip, ref newPrevTrip, ref newBeforeHotelTrip, ref newPartialCostWithoutPenalty, ref newPartialBasePenalty, ref newPartialWorkedTime, addedHotelTrip, removedHotelTrip, driver, info, instance, true);

                    if (searchTrip.Index > driverLastRelevantTrip.Index) {
                        // Further trips are no longer relevant
                        break;
                    }
                }
                if (tripIndex == trips.Length) {
                    // Last relevant trip was the last trip in the driver path, so finish the last shift
                    ProcessLastDriverShift(newShiftFirstTrip, newParkingTrip, newPrevTrip, ref newPartialCostWithoutPenalty, ref newPartialBasePenalty, ref newPartialWorkedTime, addedHotelTrip, removedHotelTrip, driver, info, true);
                }
            }
            int driverNewWorkedTime = driverOldWorkedTime + newPartialWorkedTime - oldPartialWorkedTime;
            newPartialBasePenalty += driver.GetContractTimeBasePenalty(driverNewWorkedTime, true);
            double newCost = newPartialCostWithoutPenalty + newPartialBasePenalty * info.PenaltyFactor;

            // Get diffs
            double costDiff = newCost - oldCost;
            double costWithoutPenaltyDiff = newPartialCostWithoutPenalty - oldPartialCostWithoutPenalty;
            double basePenaltyDiff = newPartialBasePenalty - oldPartialBasePenalty;
            int workedTimeDiff = newPartialWorkedTime - oldPartialWorkedTime;

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentNormalDiff().CostDiff = costDiff;
                SaDebugger.GetCurrentNormalDiff().CostWithoutPenaltyDiff = costWithoutPenaltyDiff;
                SaDebugger.GetCurrentNormalDiff().BasePenaltyDiff = basePenaltyDiff;

                CheckErrors(unassignedTrip, assignedTrip, addedHotelTrip, removedHotelTrip, driver, info);
            }
            #endif

            return (costDiff, costWithoutPenaltyDiff, basePenaltyDiff, workedTimeDiff);
        }

        static void ProcessDriverTrip(Trip searchTrip, ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, ref double costWithoutPenalty, ref double basePenalty, ref int workedTime, Trip addedHotel, Trip removedHotel, Driver driver, SaInfo info, Instance instance, bool debugIsNew) {
            if (instance.AreSameShift(prevTrip, searchTrip)) {
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
                    travelTimeBefore = instance.HalfTravelTimeViaHotel(beforeHotelTrip, shiftFirstTrip);
                }

                // Get driving time
                int shiftLengthWithoutTravel = driver.DrivingTime(shiftFirstTrip, prevTrip);
                float drivingCost = driver.DrivingCost(shiftFirstTrip, prevTrip);
                workedTime += shiftLengthWithoutTravel;

                // Get travel time after and rest time
                int travelTimeAfter, restTime;
                if (IsHotelAfter(prevTrip, addedHotel, removedHotel, info)) {
                    // Hotel stay after
                    travelTimeAfter = instance.HalfTravelTimeViaHotel(prevTrip, searchTrip);
                    restTime = instance.RestTimeViaHotel(prevTrip, searchTrip);
                    costWithoutPenalty += Config.HotelCosts;

                    // Check if the hotel stay isn't too long
                    if (restTime > Config.HotelMaxRestTime) {
                        basePenalty += PenaltyHelper.GetHotelBasePenalty(prevTrip, info, debugIsNew);
                    }

                    beforeHotelTrip = prevTrip;
                } else {
                    // No hotel stay after
                    travelTimeAfter = instance.CarTravelTime(prevTrip, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);
                    restTime = instance.RestTimeWithTravelTime(prevTrip, searchTrip, travelTimeAfter + driver.HomeTravelTimeToStart(searchTrip));

                    // Set new parking trip
                    parkingTrip = searchTrip;
                    beforeHotelTrip = null;
                }

                // Get shift length
                int travelTime = travelTimeBefore + travelTimeAfter;
                int shiftLengthWithTravel = shiftLengthWithoutTravel + travelTime;

                // Get shift cost
                float travelCost = driver.GetPaidTravelCost(travelTime);
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
                driverPathAfter = driverPathAfter.OrderBy(searchTrip => searchTrip.Index).ToList();
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

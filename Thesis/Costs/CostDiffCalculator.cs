using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class CostDiffCalculator {
        static string GetRangeString(Trip firstRelevantTrip, Trip lastRelevantTrip) {
            return string.Format("{0}--{1}", ParseHelper.TripToIndexOrUnderscore(firstRelevantTrip), ParseHelper.TripToIndexOrUnderscore(lastRelevantTrip));
        }

        static string GetNormalRangeInfo(Trip firstRelevantTrip, Trip lastRelevantTrip) {
            return "Relevant range: " + GetRangeString(firstRelevantTrip, lastRelevantTrip);
        }


        /* Operation cost diffs */

        public static (double, double, double, int, int) GetUnassignDriverCostDiff(Trip unassignedTrip, Driver driver, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Unassign trip {0} from driver {1}", unassignedTrip.Index, driver.GetId()), false, driver);
            }
            #endif

            List<Trip> driverPath = info.DriverPaths[driver.AllDriversIndex];
            (Trip firstRelevantTrip, Trip lastRelevantTrip) = GetTripRelevantRange(unassignedTrip, driverPath, info);
            Func<Trip, bool> newIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];
            (double costDiff, double costWithoutPenaltyDiff, double penaltyDiff, int workedTimeDiff, int shiftCountDiff) = GetRangeCostDiffWithUnassign(firstRelevantTrip, lastRelevantTrip, info.DriversWorkedTime[driver.AllDriversIndex], info.DriversShiftCounts[driver.AllDriversIndex], unassignedTrip, newIsHotelAfterTrip, driver, driverPath, info);

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string relevantRangeInfo = GetNormalRangeInfo(firstRelevantTrip, lastRelevantTrip);
                CheckErrors(costDiff, costWithoutPenaltyDiff, penaltyDiff, relevantRangeInfo, unassignedTrip, null, null, null, driver, info);
            }
            #endif

            return (costDiff, costWithoutPenaltyDiff, penaltyDiff, workedTimeDiff, shiftCountDiff);
        }

        public static (double, double, double, int, int) GetAssignDriverCostDiff(Trip assignedTrip, Driver driver, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Assign trip {0} to driver {1}", assignedTrip.Index, driver.GetId()), false, driver);
            }
            #endif

            List<Trip> driverPath = info.DriverPaths[driver.AllDriversIndex];
            (Trip firstRelevantTrip, Trip lastRelevantTrip) = GetTripRelevantRangeWithAssign(assignedTrip, driverPath, info);
            Func<Trip, bool> newIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];
            (double costDiff, double costWithoutPenaltyDiff, double penaltyDiff, int workedTimeDiff, int shiftCountDiff) = GetRangeCostDiffWithAssign(firstRelevantTrip, lastRelevantTrip, info.DriversWorkedTime[driver.AllDriversIndex], info.DriversShiftCounts[driver.AllDriversIndex], assignedTrip, newIsHotelAfterTrip, driver, driverPath, info);

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string relevantRangeInfo = GetNormalRangeInfo(firstRelevantTrip, lastRelevantTrip);
                CheckErrors(costDiff, costWithoutPenaltyDiff, penaltyDiff, relevantRangeInfo, null, assignedTrip, null, null, driver, info);
            }
            #endif

            return (costDiff, costWithoutPenaltyDiff, penaltyDiff, workedTimeDiff, shiftCountDiff);
        }

        public static (double, double, double, int, int) GetSwapDriverCostDiff(Trip unassignedTrip, Trip assignedTrip, Driver driver, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Unassign trip {0} from and assign trip {1} to driver {2}", unassignedTrip.Index, assignedTrip.Index, driver.GetId()), false, driver);
            }
            #endif

            List<Trip> driverPath = info.DriverPaths[driver.AllDriversIndex];
            (Trip unassignFirstRelevantTrip, Trip unassignLastRelevantTrip) = GetTripRelevantRange(unassignedTrip, driverPath, info);
            (Trip assignFirstRelevantTrip, Trip assignLastRelevantTrip) = GetTripRelevantRangeWithAssign(assignedTrip, driverPath, info);

            #if DEBUG
            string unassignRangeString, assignRangeString;
            if (Config.DebugCheckAndLogOperations) {
                unassignRangeString = GetRangeString(unassignFirstRelevantTrip, unassignLastRelevantTrip);
                assignRangeString = GetRangeString(assignFirstRelevantTrip, assignLastRelevantTrip);
            }
            #endif

            if (unassignLastRelevantTrip.Index >= assignFirstRelevantTrip.Index) {
                // Overlap, so calculate diff together
                Trip combinedFirstRelevantTrip = unassignFirstRelevantTrip.Index < assignFirstRelevantTrip.Index ? unassignFirstRelevantTrip : assignFirstRelevantTrip;
                Trip combinedLastRelevantTrip = unassignLastRelevantTrip.Index > assignLastRelevantTrip.Index ? unassignLastRelevantTrip : assignLastRelevantTrip;
                Func<Trip, bool> combinedNewIsDriverTrip = (Trip trip) => info.Assignment[trip.Index] == driver && trip != unassignedTrip || trip == assignedTrip;
                Func<Trip, bool> combinedNewIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];
                (double costDiff, double costWithoutPenaltyDiff, double penaltyDiff, int workedTimeDiff, int shiftCountDiff) = GetRangeCostDiffWithSwap(combinedFirstRelevantTrip, combinedLastRelevantTrip, info.DriversWorkedTime[driver.AllDriversIndex], info.DriversShiftCounts[driver.AllDriversIndex], unassignedTrip, assignedTrip, combinedNewIsHotelAfterTrip, driver, driverPath, info);

                #if DEBUG
                if (Config.DebugCheckAndLogOperations) {
                    string combinedRangeString = GetRangeString(combinedFirstRelevantTrip, combinedLastRelevantTrip);
                    string relevantRangeInfo = string.Format("Unassign relevant range: {0}; Assign relevant range: {1}; Combined relevant range: {2}", unassignRangeString, assignRangeString, combinedRangeString);
                    CheckErrors(costDiff, costWithoutPenaltyDiff, penaltyDiff, relevantRangeInfo, unassignedTrip, assignedTrip, null, null, driver, info);
                }
                #endif

                return (costDiff, costWithoutPenaltyDiff, penaltyDiff, workedTimeDiff, shiftCountDiff);
            } else {
                // No overlap, so calculate diffs separately
                // Unassign diff
                Func<Trip, bool> unassignNewIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];
                int oldFullWorkedTime = info.DriversWorkedTime[driver.AllDriversIndex];
                int oldFullShiftCount = info.DriversShiftCounts[driver.AllDriversIndex];
                (double unassignCostDiff, double unassignCostWithoutPenaltyDiff, double unassignPenaltyDiff, int unassignWorkedTimeDiff, int unassignShiftCountDiff) = GetRangeCostDiffWithUnassign(unassignFirstRelevantTrip, unassignLastRelevantTrip, oldFullWorkedTime, oldFullShiftCount, unassignedTrip, unassignNewIsHotelAfterTrip, driver, driverPath, info);

                // Assign diff
                Func<Trip, bool> assignNewIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];
                int fullWorkedTimeAfterUnassign = oldFullWorkedTime + unassignWorkedTimeDiff;
                int fullShiftCountAfterUnassign = oldFullShiftCount + unassignShiftCountDiff;
                (double assignCostDiff, double assignCostWithoutPenaltyDiff, double assignPenaltyDiff, int assignWorkedTimeDiff, int assignShiftCountDiff) = GetRangeCostDiffWithAssign(assignFirstRelevantTrip, assignLastRelevantTrip, fullWorkedTimeAfterUnassign, fullShiftCountAfterUnassign, assignedTrip, assignNewIsHotelAfterTrip, driver, driverPath, info);

                // Total diff
                double costDiff = unassignCostDiff + assignCostDiff;
                double costWithoutPenaltyDiff = unassignCostWithoutPenaltyDiff + assignCostWithoutPenaltyDiff;
                double penaltyDiff = unassignPenaltyDiff + assignPenaltyDiff;
                int workedTimeDiff = unassignWorkedTimeDiff + assignWorkedTimeDiff;
                int shiftCountDiff = unassignShiftCountDiff + assignShiftCountDiff;

                #if DEBUG
                if (Config.DebugCheckAndLogOperations) {
                    string relevantRangeInfo = string.Format("Unassign relevant range: {0}; Assign relevant range: {1}; Calculated separately", unassignRangeString, assignRangeString);
                    CheckErrors(costDiff, costWithoutPenaltyDiff, penaltyDiff, relevantRangeInfo, unassignedTrip, assignedTrip, null, null, driver, info);
                }
                #endif

                return (costDiff, costWithoutPenaltyDiff, penaltyDiff, workedTimeDiff, shiftCountDiff);
            }
        }

        public static (double, double, double, int, int) GetAddHotelDriverCostDiff(Trip addedHotelTrip, Driver driver, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Add hotel after {0} for driver {1}", addedHotelTrip.Index, driver.GetId()), false, driver);
            }
            #endif

            List<Trip> driverPath = info.DriverPaths[driver.AllDriversIndex];
            (Trip firstRelevantTrip, Trip lastRelevantTrip) = GetTripRelevantRange(addedHotelTrip, driverPath, info);
            Func<Trip, bool> newIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index] || trip == addedHotelTrip;
            (double costDiff, double costWithoutPenaltyDiff, double penaltyDiff, int workedTimeDiff, int shiftCountDiff) = GetRangeCostDiff(firstRelevantTrip, lastRelevantTrip, info.DriversWorkedTime[driver.AllDriversIndex], info.DriversShiftCounts[driver.AllDriversIndex], newIsHotelAfterTrip, driver, driverPath, info);

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string relevantRangeInfo = GetNormalRangeInfo(firstRelevantTrip, lastRelevantTrip);
                CheckErrors(costDiff, costWithoutPenaltyDiff, penaltyDiff, relevantRangeInfo, null, null, addedHotelTrip, null, driver, info);
            }
            #endif

            return (costDiff, costWithoutPenaltyDiff, penaltyDiff, workedTimeDiff, shiftCountDiff);
        }

        public static (double, double, double, int, int) GetRemoveHotelDriverCostDiff(Trip removedHotelTrip, Driver driver, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Remove hotel after {0} for driver {1}", removedHotelTrip.Index, driver.GetId()), false, driver);
            }
            #endif

            List<Trip> driverPath = info.DriverPaths[driver.AllDriversIndex];
            (Trip firstRelevantTrip, Trip lastRelevantTrip) = GetTripRelevantRange(removedHotelTrip, driverPath, info);
            Func<Trip, bool> newIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index] && trip != removedHotelTrip;
            (double costDiff, double costWithoutPenaltyDiff, double penaltyDiff, int workedTimeDiff, int shiftCountDiff) = GetRangeCostDiff(firstRelevantTrip, lastRelevantTrip, info.DriversWorkedTime[driver.AllDriversIndex], info.DriversShiftCounts[driver.AllDriversIndex], newIsHotelAfterTrip, driver, driverPath, info);

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string relevantRangeInfo = GetNormalRangeInfo(firstRelevantTrip, lastRelevantTrip);
                CheckErrors(costDiff, costWithoutPenaltyDiff, penaltyDiff, relevantRangeInfo, null, null, null, removedHotelTrip, driver, info);
            }
            #endif

            return (costDiff, costWithoutPenaltyDiff, penaltyDiff, workedTimeDiff, shiftCountDiff);
        }


        /* Relevant range */

        static (Trip, Trip) GetTripRelevantRange(Trip trip, List<Trip> driverPath, SaInfo info) {
            if (driverPath.Count == 0) return (trip, trip);
            (Trip firstTripInternal, Trip prevShiftLastTrip) = AssignmentHelper.GetFirstTripInternalAndPrevShiftTrip(trip, driverPath, info);
            (Trip lastTripInternal, Trip nextShiftFirstTrip) = AssignmentHelper.GetLastTripInternalAndNextShiftTrip(trip, driverPath, info);
            return GetTripRelevantRange(trip, firstTripInternal, prevShiftLastTrip, lastTripInternal, nextShiftFirstTrip, driverPath, info);
        }

        static (Trip, Trip) GetTripRelevantRangeWithAssign(Trip assignedTrip, List<Trip> driverPath, SaInfo info) {
            if (driverPath.Count == 0) return (null, null);

            int pathTripIndex = AssignmentHelper.GetAssignedPathTripIndexBefore(assignedTrip, driverPath);
            (Trip firstTripInternal, Trip prevShiftLastTrip) = AssignmentHelper.GetFirstTripInternalAndPrevShiftTrip(assignedTrip, pathTripIndex, driverPath, info);
            (Trip lastTripInternal, Trip nextShiftFirstTrip) = AssignmentHelper.GetLastTripInternalAndNextShiftTrip(assignedTrip, pathTripIndex + 1, driverPath, info);

            (Trip firstRelevantTrip, Trip lastRelevantTrip) = GetTripRelevantRange(assignedTrip, firstTripInternal, prevShiftLastTrip, lastTripInternal, nextShiftFirstTrip, driverPath, info);

            // Ensure first and last trips are not the assigned trip; this can only happen if before the first or after the last trip in the path, respectively
            if (firstRelevantTrip == assignedTrip) firstRelevantTrip = driverPath[0];
            else if (lastRelevantTrip == assignedTrip) lastRelevantTrip = driverPath[driverPath.Count - 1];

            return (firstRelevantTrip, lastRelevantTrip);
        }

        static (Trip, Trip) GetTripRelevantRange(Trip trip, Trip firstTripInternal, Trip prevShiftLastTrip, Trip lastTripInternal, Trip nextShiftFirstTrip, List<Trip> driverPath, SaInfo info) {
            // If this trip is the first in shift, first relevant trip is the first trip connected to the *previous* shift by hotel stays
            // Else, first relevant trip is the first trip connected to the *current* shift by hotel stays
            bool isFirstTripInShift = firstTripInternal == trip;
            if (prevShiftLastTrip != null) {
                if (isFirstTripInShift) {
                    (firstTripInternal, prevShiftLastTrip) = AssignmentHelper.GetFirstTripInternalAndPrevShiftTrip(prevShiftLastTrip, driverPath, info);
                }
                while (prevShiftLastTrip != null && info.IsHotelStayAfterTrip[prevShiftLastTrip.Index]) {
                    (firstTripInternal, prevShiftLastTrip) = AssignmentHelper.GetFirstTripInternalAndPrevShiftTrip(prevShiftLastTrip, driverPath, info);
                }
            }

            // If this trip is the first or last in shift, last relevant trip is the last trip connected to the next shift by hotel stays
            // Else, last relevant trip is last trip of shift
            if ((isFirstTripInShift || lastTripInternal == trip) && nextShiftFirstTrip != null) {
                (lastTripInternal, nextShiftFirstTrip) = AssignmentHelper.GetLastTripInternalAndNextShiftTrip(nextShiftFirstTrip, driverPath, info);
                while (nextShiftFirstTrip != null && info.IsHotelStayAfterTrip[lastTripInternal.Index]) {
                    (lastTripInternal, nextShiftFirstTrip) = AssignmentHelper.GetLastTripInternalAndNextShiftTrip(nextShiftFirstTrip, driverPath, info);
                }
            }

            // Return first and last trip in range, as well as trip before and after range
            return (firstTripInternal, lastTripInternal);
        }


        /* Range cost diff */
        static (double, double, double, int, int) GetRangeCostDiff(Trip rangeFirstTrip, Trip rangeLastTrip, int oldFullWorkedTime, int oldFullShiftCount, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            (double newCostWithoutPenalty, double newPenalty, int newWorkedTime, int newShiftCount) = GetRangeCost(rangeFirstTrip, rangeLastTrip, newIsHotelAfterTrip, driver, driverPath, info, true);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldFullWorkedTime, oldFullShiftCount, newCostWithoutPenalty, newPenalty, newWorkedTime, newShiftCount, driver, driverPath, info);
        }

        static (double, double, double, int, int) GetRangeCostDiffWithUnassign(Trip rangeFirstTrip, Trip rangeLastTrip, int oldFullWorkedTime, int oldFullShiftCount, Trip unassignedTrip, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            (double newCostWithoutPenalty, double newPenalty, int newWorkedTime, int newShiftCount) = GetRangeCostWithUnassign(rangeFirstTrip, rangeLastTrip, unassignedTrip, newIsHotelAfterTrip, driver, driverPath, info, true);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldFullWorkedTime, oldFullShiftCount, newCostWithoutPenalty, newPenalty, newWorkedTime, newShiftCount, driver, driverPath, info);
        }

        static (double, double, double, int, int) GetRangeCostDiffWithAssign(Trip rangeFirstTrip, Trip rangeLastTrip, int oldFullWorkedTime, int oldFullShiftCount, Trip assignedTrip, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            (double newCostWithoutPenalty, double newPenalty, int newWorkedTime, int newShiftCount) = GetRangeCostWithAssign(rangeFirstTrip, rangeLastTrip, assignedTrip, newIsHotelAfterTrip, driver, driverPath, info, true);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldFullWorkedTime, oldFullShiftCount, newCostWithoutPenalty, newPenalty, newWorkedTime, newShiftCount, driver, driverPath, info);
        }

        static (double, double, double, int, int) GetRangeCostDiffWithSwap(Trip rangeFirstTrip, Trip rangeLastTrip, int oldFullWorkedTime, int oldFullShiftCount, Trip unassignedTrip, Trip assignedTrip, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            (double newCostWithoutPenalty, double newPenalty, int newWorkedTime, int newShiftCount) = GetRangeCostWithSwap(rangeFirstTrip, rangeLastTrip, unassignedTrip, assignedTrip, newIsHotelAfterTrip, driver, driverPath, info, true);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldFullWorkedTime, oldFullShiftCount, newCostWithoutPenalty, newPenalty, newWorkedTime, newShiftCount, driver, driverPath, info);
        }

        static (double, double, double, int, int) GetRangeCostDiffFromNewCosts(Trip rangeFirstTrip, Trip rangeLastTrip, int oldFullWorkedTime, int oldFullShiftCount, double newCostWithoutPenalty, double newPenalty, int newWorkedTime, int newShiftCount, Driver driver, List<Trip> driverPath, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentNormalDiff().DriverPathString = ParseHelper.DriverPathToString(driverPath, info);
            }
            #endif

            // Old range cost
            Func<Trip, bool> oldIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];
            (double oldCostWithoutPenalty, double oldPenalty, int oldWorkedTime, int oldShiftCount) = GetRangeCost(rangeFirstTrip, rangeLastTrip, oldIsHotelAfterTrip, driver, driverPath, info, false);
            oldPenalty += driver.GetContractTimePenalty(oldFullWorkedTime, false);
            oldPenalty += PenaltyHelper.GetShiftCountPenalty(oldFullShiftCount, false);
            double oldCost = oldCostWithoutPenalty + oldPenalty;

            // New range cost
            int driverNewWorkedTime = oldFullWorkedTime + newWorkedTime - oldWorkedTime;
            newPenalty += driver.GetContractTimePenalty(driverNewWorkedTime, true);
            int driverNewShiftCount = oldFullShiftCount + newShiftCount - oldShiftCount;
            newPenalty += PenaltyHelper.GetShiftCountPenalty(driverNewShiftCount, true);
            double newCost = newCostWithoutPenalty + newPenalty;

            // Diffs
            double costDiff = newCost - oldCost;
            double costWithoutPenaltyDiff = newCostWithoutPenalty - oldCostWithoutPenalty;
            double penaltyDiff = newPenalty - oldPenalty;
            int workedTimeDiff = newWorkedTime - oldWorkedTime;
            int shiftCountDiff = newShiftCount - oldShiftCount;

            return (costDiff, costWithoutPenaltyDiff, penaltyDiff, workedTimeDiff, shiftCountDiff);
        }


        /* Range costs */

        /** Get costs of part of a driver's path; penalty are computed with without worked time and shift count penalties */
        static (double, double, int, int) GetRangeCost(Trip rangeFirstTrip, Trip rangeLastTrip, Func<Trip, bool> isHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info, bool debugIsNew) {
            if (driverPath.Count == 0) return (0, 0, 0, 0);
            int rangeFirstPathIndex = info.DriverPathIndices[rangeFirstTrip.Index];
            int rangeLastPathIndex = info.DriverPathIndices[rangeLastTrip.Index];
            double costWithoutPenalty = 0, partialPenalty = 0;
            int workedTime = 0, shiftCount = 0;
            Trip shiftFirstTrip = rangeFirstTrip, parkingTrip = rangeFirstTrip, prevTrip = rangeFirstTrip, beforeHotelTrip = null;
            for (int pathTripIndex = rangeFirstPathIndex + 1; pathTripIndex <= rangeLastPathIndex; pathTripIndex++) {
                Trip searchTrip = driverPath[pathTripIndex];
                ProcessDriverTrip(searchTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref partialPenalty, ref workedTime, ref shiftCount, driver, info, info.Instance, debugIsNew);
            }
            Trip tripAfterRange = rangeLastPathIndex + 1 < driverPath.Count ? driverPath[rangeLastPathIndex + 1] : null;
            ProcessDriverEndRange(tripAfterRange, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref partialPenalty, ref workedTime, ref shiftCount, driver, info, info.Instance, debugIsNew);
            return (costWithoutPenalty, partialPenalty, workedTime, shiftCount);
        }

        /** Get costs of part of a driver's path where a trip is unassigned; penalty are computed with without worked time and shift count penalties */
        static (double, double, int, int) GetRangeCostWithUnassign(Trip rangeFirstTrip, Trip rangeLastTrip, Trip unassignedTrip, Func<Trip, bool> isHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info, bool debugIsNew) {
            int rangeFirstPathIndex = info.DriverPathIndices[rangeFirstTrip.Index];
            int rangeLastPathIndex = info.DriverPathIndices[rangeLastTrip.Index];
            double costWithoutPenalty = 0, partialPenalty = 0;
            int workedTime = 0, shiftCount = 0;
            Trip shiftFirstTrip = null, parkingTrip = null, prevTrip = null, beforeHotelTrip = null;
            for (int pathTripIndex = rangeFirstPathIndex; pathTripIndex <= rangeLastPathIndex; pathTripIndex++) {
                Trip searchTrip = driverPath[pathTripIndex];
                if (searchTrip == unassignedTrip) continue;

                if (shiftFirstTrip == null) {
                    shiftFirstTrip = parkingTrip = prevTrip = searchTrip;
                    continue;
                }

                ProcessDriverTrip(searchTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref partialPenalty, ref workedTime, ref shiftCount, driver, info, info.Instance, debugIsNew);
            }
            Trip tripAfterRange = rangeLastPathIndex + 1 < driverPath.Count ? driverPath[rangeLastPathIndex + 1] : null;
            ProcessDriverEndRange(tripAfterRange, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref partialPenalty, ref workedTime, ref shiftCount, driver, info, info.Instance, debugIsNew);
            return (costWithoutPenalty, partialPenalty, workedTime, shiftCount);
        }

        /** Get costs of part of a driver's path where a trip is assigned; penalty are computed with without worked time and shift count penalties */
        static (double, double, int, int) GetRangeCostWithAssign(Trip rangeFirstTrip, Trip rangeLastTrip, Trip assignedTrip, Func<Trip, bool> isHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info, bool debugIsNew) {
            double costWithoutPenalty = 0, partialPenalty = 0;
            int workedTime = 0, shiftCount = 0;
            Trip shiftFirstTrip = null, parkingTrip = null, prevTrip = null, beforeHotelTrip = null;

            if (driverPath.Count == 0) {
                // New path only contains assigned trip
                shiftFirstTrip = parkingTrip = prevTrip = assignedTrip;
                ProcessDriverEndRange(null, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref partialPenalty, ref workedTime, ref shiftCount, driver, info, info.Instance, debugIsNew);
                return (costWithoutPenalty, partialPenalty, workedTime, shiftCount);
            }

            int rangeFirstPathIndex = info.DriverPathIndices[rangeFirstTrip.Index];
            int rangeLastPathIndex = info.DriverPathIndices[rangeLastTrip.Index];

            // Process part before assigned trip
            int pathTripIndex;
            for (pathTripIndex = rangeFirstPathIndex; pathTripIndex <= rangeLastPathIndex; pathTripIndex++) {
                Trip searchTrip = driverPath[pathTripIndex];
                if (searchTrip.Index > assignedTrip.Index) break;

                if (shiftFirstTrip == null) {
                    shiftFirstTrip = parkingTrip = prevTrip = searchTrip;
                    continue;
                }

                ProcessDriverTrip(searchTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref partialPenalty, ref workedTime, ref shiftCount, driver, info, info.Instance, debugIsNew);
            }

            // Process assigned trip
            if (shiftFirstTrip == null) {
                shiftFirstTrip = parkingTrip = prevTrip = assignedTrip;
            } else {
                ProcessDriverTrip(assignedTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref partialPenalty, ref workedTime, ref shiftCount, driver, info, info.Instance, debugIsNew);
            }

            // Process part after assigned trip
            for (; pathTripIndex <= rangeLastPathIndex; pathTripIndex++) {
                Trip searchTrip = driverPath[pathTripIndex];
                ProcessDriverTrip(searchTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref partialPenalty, ref workedTime, ref shiftCount, driver, info, info.Instance, debugIsNew);
            }

            Trip tripAfterRange = rangeLastPathIndex + 1 < driverPath.Count ? driverPath[rangeLastPathIndex + 1] : null;
            ProcessDriverEndRange(tripAfterRange, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref partialPenalty, ref workedTime, ref shiftCount, driver, info, info.Instance, debugIsNew);
            return (costWithoutPenalty, partialPenalty, workedTime, shiftCount);
        }

        /** Get costs of part of a driver's path where a trip is unassigned and another assigned; penalty are computed with without worked time and shift count penalties */
        static (double, double, int, int) GetRangeCostWithSwap(Trip rangeFirstTrip, Trip rangeLastTrip, Trip unassignedTrip, Trip assignedTrip, Func<Trip, bool> isHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info, bool debugIsNew) {
            double costWithoutPenalty = 0, partialPenalty = 0;
            int workedTime = 0, shiftCount = 0;
            Trip shiftFirstTrip = null, parkingTrip = null, prevTrip = null, beforeHotelTrip = null;

            if (driverPath.Count == 0) {
                // New path only contains assigned trip
                ProcessDriverTrip(assignedTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref partialPenalty, ref workedTime, ref shiftCount, driver, info, info.Instance, debugIsNew);
                ProcessDriverEndRange(null, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref partialPenalty, ref workedTime, ref shiftCount, driver, info, info.Instance, debugIsNew);
                return (costWithoutPenalty, partialPenalty, workedTime, shiftCount);
            }

            int rangeFirstPathIndex = info.DriverPathIndices[rangeFirstTrip.Index];
            int rangeLastPathIndex = info.DriverPathIndices[rangeLastTrip.Index];

            // Process part before assigned trip
            int pathTripIndex;
            for (pathTripIndex = rangeFirstPathIndex ; pathTripIndex <= rangeLastPathIndex; pathTripIndex++) {
                Trip searchTrip = driverPath[pathTripIndex];
                if (searchTrip.Index > assignedTrip.Index) break;
                if (searchTrip == unassignedTrip) continue;

                if (shiftFirstTrip == null) {
                    shiftFirstTrip = parkingTrip = prevTrip = searchTrip;
                    continue;
                }

                ProcessDriverTrip(searchTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref partialPenalty, ref workedTime, ref shiftCount, driver, info, info.Instance, debugIsNew);
            }

            // Process assigned trip
            if (shiftFirstTrip == null) {
                shiftFirstTrip = parkingTrip = prevTrip = assignedTrip;
            } else {
                ProcessDriverTrip(assignedTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref partialPenalty, ref workedTime, ref shiftCount, driver, info, info.Instance, debugIsNew);
            }

            // Process part after assigned trip
            for (; pathTripIndex <= rangeLastPathIndex; pathTripIndex++) {
                Trip searchTrip = driverPath[pathTripIndex];
                if (searchTrip == unassignedTrip) continue;
                ProcessDriverTrip(searchTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref partialPenalty, ref workedTime, ref shiftCount, driver, info, info.Instance, debugIsNew);
            }

            Trip tripAfterRange = rangeLastPathIndex + 1 < driverPath.Count ? driverPath[rangeLastPathIndex + 1] : null;
            ProcessDriverEndRange(tripAfterRange, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref partialPenalty, ref workedTime, ref shiftCount, driver, info, info.Instance, debugIsNew);
            return (costWithoutPenalty, partialPenalty, workedTime, shiftCount);
        }


        /* Process trips and shifts */

        static void ProcessDriverTrip(Trip searchTrip, ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, Func<Trip, bool> isHotelAfterTrip, ref double costWithoutPenalty, ref double penalty, ref int workedTime, ref int shiftCount, Driver driver, SaInfo info, Instance instance, bool debugIsNew) {
            if (instance.AreSameShift(prevTrip, searchTrip)) {
                /* Same shift */
                // Check precedence
                penalty += PenaltyHelper.GetPrecedencePenalty(prevTrip, searchTrip, info, debugIsNew);

                // Check for invalid hotel stay
                if (isHotelAfterTrip(prevTrip)) {
                    penalty += PenaltyHelper.GetHotelPenalty(prevTrip, info, debugIsNew);
                }
            } else {
                /* Start of new shift */
                ProcessDriverEndNonFinalShift(searchTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref penalty, ref workedTime, ref shiftCount, driver, info, instance, debugIsNew);
            }

            prevTrip = searchTrip;
        }

        static void ProcessDriverEndRange(Trip tripAfterRange, ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, Func<Trip, bool> isHotelAfterTrip, ref double costWithoutPenalty, ref double penalty, ref int workedTime, ref int shiftCount, Driver driver, SaInfo info, Instance instance, bool debugIsNew) {
            // If the range is not empty, finish the last shift of the range
            if (shiftFirstTrip != null) {
                if (tripAfterRange == null) {
                    // This is the end of the driver path
                    ProcessDriverEndFinalShift(ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref penalty, ref workedTime, ref shiftCount, driver, info, info.Instance, debugIsNew);
                } else {
                    // This is the end of the range, but not the driver path
                    ProcessDriverEndNonFinalShift(tripAfterRange, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref penalty, ref workedTime, ref shiftCount, driver, info, info.Instance, debugIsNew);
                }
            }
        }

        static void ProcessDriverEndNonFinalShift(Trip searchTrip, ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, Func<Trip, bool> isHotelAfterTrip, ref double costWithoutPenalty, ref double penalty, ref int workedTime, ref int shiftCount, Driver driver, SaInfo info, Instance instance, bool debugIsNew) {
            shiftCount++;

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
            int shiftLengthWithoutTravel = instance.DrivingTime(shiftFirstTrip, prevTrip);
            float drivingCost = driver.DrivingCost(shiftFirstTrip, prevTrip);
            workedTime += shiftLengthWithoutTravel;

            // Get travel time after and rest time
            int travelTimeAfter, restTime;
            if (isHotelAfterTrip(prevTrip)) {
                // Hotel stay after
                travelTimeAfter = instance.HalfTravelTimeViaHotel(prevTrip, searchTrip);
                restTime = instance.RestTimeViaHotel(prevTrip, searchTrip);
                costWithoutPenalty += Config.HotelCosts;

                // Check if the hotel stay isn't too long
                if (restTime > Config.HotelMaxRestTime) {
                    penalty += PenaltyHelper.GetHotelPenalty(prevTrip, info, debugIsNew);
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
            penalty += PenaltyHelper.GetShiftLengthPenalty(shiftLengthWithoutTravel, shiftLengthWithTravel, debugIsNew);

            // Check rest time
            penalty += PenaltyHelper.GetRestTimePenalty(restTime, debugIsNew);

            // Start new shift
            shiftFirstTrip = searchTrip;
        }

        static void ProcessDriverEndFinalShift(ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, Func<Trip, bool> isHotelAfterTrip, ref double costWithoutPenalty, ref double penalty, ref int workedTime, ref int shiftCount, Driver driver, SaInfo info, Instance instance, bool debugIsNew) {
            shiftCount++;

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
            int shiftLengthWithoutTravel = instance.DrivingTime(shiftFirstTrip, prevTrip);
            float drivingCost = driver.DrivingCost(shiftFirstTrip, prevTrip);
            workedTime += shiftLengthWithoutTravel;

            // Get travel time after
            int travelTimeAfter = instance.CarTravelTime(prevTrip, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);

            // Get shift length
            int travelTime = travelTimeBefore + travelTimeAfter;
            int shiftLengthWithTravel = shiftLengthWithoutTravel + travelTime;

            // Get shift cost
            float travelCost = driver.GetPaidTravelCost(travelTime);
            float shiftCost = drivingCost + travelCost;
            costWithoutPenalty += shiftCost;

            // Check shift length
            penalty += PenaltyHelper.GetShiftLengthPenalty(shiftLengthWithoutTravel, shiftLengthWithTravel, debugIsNew);

            // Check for invalid hotel stay
            if (isHotelAfterTrip(prevTrip)) {
                penalty += PenaltyHelper.GetHotelPenalty(prevTrip, info, debugIsNew);
            }
        }


        /* Debugging */

        static void CheckErrors(double costDiff, double costWithoutPenaltyDiff, double penaltyDiff, string relevantRangeInfo, Trip unassignedTrip, Trip assignedTrip, Trip addedHotel, Trip removedHotel, Driver driver, SaInfo info) {
            SaDebugger.GetCurrentNormalDiff().CostDiff = costDiff;
            SaDebugger.GetCurrentNormalDiff().CostWithoutPenaltyDiff = costWithoutPenaltyDiff;
            SaDebugger.GetCurrentNormalDiff().PenaltyDiff = penaltyDiff;
            SaDebugger.GetCurrentNormalDiff().RelevantRangeInfo = relevantRangeInfo;

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

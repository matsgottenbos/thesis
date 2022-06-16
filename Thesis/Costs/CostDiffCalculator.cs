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

        public static SaDriverInfo GetUnassignDriverCostDiff(Trip unassignedTrip, Driver driver, SaDriverInfo oldDriverInfo, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Unassign trip {0} from driver {1}", unassignedTrip.Index, driver.GetId()), driver);
            }
            #endif

            List<Trip> driverPath = info.DriverPaths[driver.AllDriversIndex];
            (Trip firstRelevantTrip, Trip lastRelevantTrip) = GetTripRelevantRange(unassignedTrip, driverPath, info);
            Func<Trip, bool> newIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];
            SaDriverInfo driverInfoDiff = RangeCostDiffCalculator.GetRangeCostDiffWithUnassign(firstRelevantTrip, lastRelevantTrip, oldDriverInfo, unassignedTrip, newIsHotelAfterTrip, driver, driverPath, info);

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string relevantRangeInfo = GetNormalRangeInfo(firstRelevantTrip, lastRelevantTrip);
                CheckErrors(driverInfoDiff, unassignedTrip, null, null, null, driver, info);
            }
            #endif

            return driverInfoDiff;
        }

        public static SaDriverInfo GetAssignDriverCostDiff(Trip assignedTrip, Driver driver, SaDriverInfo oldDriverInfo, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Assign trip {0} to driver {1}", assignedTrip.Index, driver.GetId()), driver);
            }
            #endif

            List<Trip> driverPath = info.DriverPaths[driver.AllDriversIndex];
            (Trip firstRelevantTrip, Trip lastRelevantTrip) = GetTripRelevantRangeWithAssign(assignedTrip, driverPath, info);
            Func<Trip, bool> newIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];
            SaDriverInfo driverInfoDiff = RangeCostDiffCalculator.GetRangeCostDiffWithAssign(firstRelevantTrip, lastRelevantTrip, oldDriverInfo, assignedTrip, newIsHotelAfterTrip, driver, driverPath, info);

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string relevantRangeInfo = GetNormalRangeInfo(firstRelevantTrip, lastRelevantTrip);
                CheckErrors(driverInfoDiff, null, assignedTrip, null, null, driver, info);
            }
            #endif

            return driverInfoDiff;
        }

        public static SaDriverInfo GetSwapDriverCostDiff(Trip unassignedTrip, Trip assignedTrip, Driver driver, SaDriverInfo oldDriverInfo, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Unassign trip {0} from and assign trip {1} to driver {2}", unassignedTrip.Index, assignedTrip.Index, driver.GetId()), driver);
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
                SaDriverInfo driverInfoDiff = RangeCostDiffCalculator.GetRangeCostDiffWithSwap(combinedFirstRelevantTrip, combinedLastRelevantTrip, oldDriverInfo, unassignedTrip, assignedTrip, combinedNewIsHotelAfterTrip, driver, driverPath, info);

                #if DEBUG
                if (Config.DebugCheckAndLogOperations) {
                    string combinedRangeString = GetRangeString(combinedFirstRelevantTrip, combinedLastRelevantTrip);
                    string relevantRangeInfo = string.Format("Unassign relevant range: {0}; Assign relevant range: {1}; Combined relevant range: {2}", unassignRangeString, assignRangeString, combinedRangeString);
                    CheckErrors(driverInfoDiff, unassignedTrip, assignedTrip, null, null, driver, info);
                }
                #endif

                return driverInfoDiff;
            } else {
                // No overlap, so calculate diffs separately
                // Unassign diff
                Func<Trip, bool> unassignNewIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];
                SaDriverInfo unassignDriverInfoDiff = RangeCostDiffCalculator.GetRangeCostDiffWithUnassign(unassignFirstRelevantTrip, unassignLastRelevantTrip, oldDriverInfo, unassignedTrip, unassignNewIsHotelAfterTrip, driver, driverPath, info);

                // Assign diff
                Func<Trip, bool> assignNewIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];
                SaDriverInfo driverInfoAfterUnassign = oldDriverInfo + unassignDriverInfoDiff;
                SaDriverInfo assignDriverInfoDiff = RangeCostDiffCalculator.GetRangeCostDiffWithAssign(assignFirstRelevantTrip, assignLastRelevantTrip, driverInfoAfterUnassign, assignedTrip, assignNewIsHotelAfterTrip, driver, driverPath, info);

                // Total diff
                SaDriverInfo driverInfoDiff = unassignDriverInfoDiff + assignDriverInfoDiff;

                #if DEBUG
                if (Config.DebugCheckAndLogOperations) {
                    string relevantRangeInfo = string.Format("Unassign relevant range: {0}; Assign relevant range: {1}; Calculated separately", unassignRangeString, assignRangeString);
                    CheckErrors(driverInfoDiff, unassignedTrip, assignedTrip, null, null, driver, info);
                }
                #endif

                return driverInfoDiff;
            }
        }

        public static SaDriverInfo GetAddHotelDriverCostDiff(Trip addedHotelTrip, Driver driver, SaDriverInfo oldDriverInfo, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Add hotel after {0} for driver {1}", addedHotelTrip.Index, driver.GetId()), driver);
            }
            #endif

            List<Trip> driverPath = info.DriverPaths[driver.AllDriversIndex];
            (Trip firstRelevantTrip, Trip lastRelevantTrip) = GetTripRelevantRange(addedHotelTrip, driverPath, info);
            Func<Trip, bool> newIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index] || trip == addedHotelTrip;
            SaDriverInfo driverInfoDiff = RangeCostDiffCalculator.GetRangeCostDiff(firstRelevantTrip, lastRelevantTrip, oldDriverInfo, newIsHotelAfterTrip, driver, driverPath, info);

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string relevantRangeInfo = GetNormalRangeInfo(firstRelevantTrip, lastRelevantTrip);
                CheckErrors(driverInfoDiff, null, null, addedHotelTrip, null, driver, info);
            }
            #endif

            return driverInfoDiff;
        }

        public static SaDriverInfo GetRemoveHotelDriverCostDiff(Trip removedHotelTrip, Driver driver, SaDriverInfo oldDriverInfo, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Remove hotel after {0} for driver {1}", removedHotelTrip.Index, driver.GetId()), driver);
            }
            #endif

            List<Trip> driverPath = info.DriverPaths[driver.AllDriversIndex];
            (Trip firstRelevantTrip, Trip lastRelevantTrip) = GetTripRelevantRange(removedHotelTrip, driverPath, info);
            Func<Trip, bool> newIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index] && trip != removedHotelTrip;
            SaDriverInfo driverInfoDiff = RangeCostDiffCalculator.GetRangeCostDiff(firstRelevantTrip, lastRelevantTrip, oldDriverInfo, newIsHotelAfterTrip, driver, driverPath, info);

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string relevantRangeInfo = GetNormalRangeInfo(firstRelevantTrip, lastRelevantTrip);
                CheckErrors(driverInfoDiff, null, null, null, removedHotelTrip, driver, info);
            }
            #endif

            return driverInfoDiff;
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


        /* Debugging */

        static void CheckErrors(SaDriverInfo operationDriverInfoDiff, Trip unassignedTrip, Trip assignedTrip, Trip addedHotel, Trip removedHotel, Driver driver, SaInfo info) {
            List<Trip> oldDriverPath = info.DriverPaths[driver.AllDriversIndex];

            // Old operation info
            SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.OldNormal);
            SaDriverInfo oldOperationDriverInfo = info.DriverInfos[driver.AllDriversIndex];
            SaDebugger.GetCurrentStageInfo().SetDriverInfo(oldOperationDriverInfo);

            // New operation info
            SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewNormal);
            SaDriverInfo newOperationDriverInfo = oldOperationDriverInfo + operationDriverInfoDiff;
            SaDebugger.GetCurrentStageInfo().SetDriverInfo(newOperationDriverInfo);

            // Old checked info
            SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.OldChecked);
            SaDriverInfo oldCheckedDriverInfo = TotalCostCalculator.GetDriverInfo(oldDriverPath, info.IsHotelStayAfterTrip, driver, info);
            SaDebugger.GetCurrentStageInfo().SetDriverInfo(oldCheckedDriverInfo);

            // Get driver path after
            List<Trip> newDriverPath = oldDriverPath.Copy();
            if (unassignedTrip != null) {
                int removedCount = newDriverPath.RemoveAll(searchTrip => searchTrip.Index == unassignedTrip.Index);
                if (removedCount != 1) throw new Exception("Error removing trip from driver path");
            }
            if (assignedTrip != null) {
                newDriverPath.Add(assignedTrip);
                newDriverPath = newDriverPath.OrderBy(searchTrip => searchTrip.Index).ToList();
            }

            // Get hotel stays after
            bool[] newIsHotelStayAfterTrip = info.IsHotelStayAfterTrip.Copy();
            if (addedHotel != null) newIsHotelStayAfterTrip[addedHotel.Index] = true;
            if (removedHotel != null) newIsHotelStayAfterTrip[removedHotel.Index] = false;

            // New checked info
            SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewChecked);
            SaDriverInfo newCheckedDriverInfo = TotalCostCalculator.GetDriverInfo(newDriverPath, newIsHotelStayAfterTrip, driver, info);
            SaDebugger.GetCurrentStageInfo().SetDriverInfo(newCheckedDriverInfo);

            // Check for errors
            SaDebugger.GetCurrentOperationPart().CheckDriverErrors();
        }
    }
}

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

        public static (double, double, double, double, DriverInfo) GetUnassignDriverCostDiff(Trip unassignedTrip, Driver driver, DriverInfo oldDriverInfo, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Unassign trip {0} from driver {1}", unassignedTrip.Index, driver.GetId()), false, driver);
            }
            #endif

            List<Trip> driverPath = info.DriverPaths[driver.AllDriversIndex];
            (Trip firstRelevantTrip, Trip lastRelevantTrip) = GetTripRelevantRange(unassignedTrip, driverPath, info);
            Func<Trip, bool> newIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];
            (double costDiff, double costWithoutPenaltyDiff, double penaltyDiff, double satisfactionDiff, DriverInfo driverInfoDiff) = RangeCostDiffCalculator.GetRangeCostDiffWithUnassign(firstRelevantTrip, lastRelevantTrip, oldDriverInfo, unassignedTrip, newIsHotelAfterTrip, driver, driverPath, info);

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string relevantRangeInfo = GetNormalRangeInfo(firstRelevantTrip, lastRelevantTrip);
                CheckErrors(costDiff, costWithoutPenaltyDiff, penaltyDiff, satisfactionDiff, relevantRangeInfo, unassignedTrip, null, null, null, driver, info);
            }
            #endif

            return (costDiff, costWithoutPenaltyDiff, penaltyDiff, satisfactionDiff, driverInfoDiff);
        }

        public static (double, double, double, double, DriverInfo) GetAssignDriverCostDiff(Trip assignedTrip, Driver driver, DriverInfo oldDriverInfo, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Assign trip {0} to driver {1}", assignedTrip.Index, driver.GetId()), false, driver);
            }
            #endif

            List<Trip> driverPath = info.DriverPaths[driver.AllDriversIndex];
            (Trip firstRelevantTrip, Trip lastRelevantTrip) = GetTripRelevantRangeWithAssign(assignedTrip, driverPath, info);
            Func<Trip, bool> newIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];
            (double costDiff, double costWithoutPenaltyDiff, double penaltyDiff, double satisfactionDiff, DriverInfo driverInfoDiff) = RangeCostDiffCalculator.GetRangeCostDiffWithAssign(firstRelevantTrip, lastRelevantTrip, oldDriverInfo, assignedTrip, newIsHotelAfterTrip, driver, driverPath, info);

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string relevantRangeInfo = GetNormalRangeInfo(firstRelevantTrip, lastRelevantTrip);
                CheckErrors(costDiff, costWithoutPenaltyDiff, penaltyDiff, satisfactionDiff, relevantRangeInfo, null, assignedTrip, null, null, driver, info);
            }
            #endif

            return (costDiff, costWithoutPenaltyDiff, penaltyDiff, satisfactionDiff, driverInfoDiff);
        }

        public static (double, double, double, double, DriverInfo) GetSwapDriverCostDiff(Trip unassignedTrip, Trip assignedTrip, Driver driver, DriverInfo oldDriverInfo, SaInfo info) {
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
                (double costDiff, double costWithoutPenaltyDiff, double penaltyDiff, double satisfactionDiff, DriverInfo driverInfoDiff) = RangeCostDiffCalculator.GetRangeCostDiffWithSwap(combinedFirstRelevantTrip, combinedLastRelevantTrip, oldDriverInfo, unassignedTrip, assignedTrip, combinedNewIsHotelAfterTrip, driver, driverPath, info);

                #if DEBUG
                if (Config.DebugCheckAndLogOperations) {
                    string combinedRangeString = GetRangeString(combinedFirstRelevantTrip, combinedLastRelevantTrip);
                    string relevantRangeInfo = string.Format("Unassign relevant range: {0}; Assign relevant range: {1}; Combined relevant range: {2}", unassignRangeString, assignRangeString, combinedRangeString);
                    CheckErrors(costDiff, costWithoutPenaltyDiff, penaltyDiff, satisfactionDiff, relevantRangeInfo, unassignedTrip, assignedTrip, null, null, driver, info);
                }
                #endif

                return (costDiff, costWithoutPenaltyDiff, penaltyDiff, satisfactionDiff, driverInfoDiff);
            } else {
                // No overlap, so calculate diffs separately
                // Unassign diff
                Func<Trip, bool> unassignNewIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];
                (double unassignCostDiff, double unassignCostWithoutPenaltyDiff, double unassignPenaltyDiff, double unassignSatisfactionDiff, DriverInfo unassignDriverInfoDiff) = RangeCostDiffCalculator.GetRangeCostDiffWithUnassign(unassignFirstRelevantTrip, unassignLastRelevantTrip, oldDriverInfo, unassignedTrip, unassignNewIsHotelAfterTrip, driver, driverPath, info);

                // Assign diff
                Func<Trip, bool> assignNewIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];
                DriverInfo driverInfoAfterUnassign = oldDriverInfo + unassignDriverInfoDiff;
                (double assignCostDiff, double assignCostWithoutPenaltyDiff, double assignPenaltyDiff, double assignSatisfactionDiff, DriverInfo assignDriverInfoDiff) = RangeCostDiffCalculator.GetRangeCostDiffWithAssign(assignFirstRelevantTrip, assignLastRelevantTrip, driverInfoAfterUnassign, assignedTrip, assignNewIsHotelAfterTrip, driver, driverPath, info);

                // Total diff
                double costDiff = unassignCostDiff + assignCostDiff;
                double costWithoutPenaltyDiff = unassignCostWithoutPenaltyDiff + assignCostWithoutPenaltyDiff;
                double penaltyDiff = unassignPenaltyDiff + assignPenaltyDiff;
                double satisfactionDiff = unassignSatisfactionDiff + assignSatisfactionDiff;
                DriverInfo driverInfoDiff = unassignDriverInfoDiff + assignDriverInfoDiff;

                #if DEBUG
                if (Config.DebugCheckAndLogOperations) {
                    string relevantRangeInfo = string.Format("Unassign relevant range: {0}; Assign relevant range: {1}; Calculated separately", unassignRangeString, assignRangeString);
                    CheckErrors(costDiff, costWithoutPenaltyDiff, penaltyDiff, satisfactionDiff, relevantRangeInfo, unassignedTrip, assignedTrip, null, null, driver, info);
                }
                #endif

                return (costDiff, costWithoutPenaltyDiff, penaltyDiff, satisfactionDiff, driverInfoDiff);
            }
        }

        public static (double, double, double, double, DriverInfo) GetAddHotelDriverCostDiff(Trip addedHotelTrip, Driver driver, DriverInfo oldDriverInfo, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Add hotel after {0} for driver {1}", addedHotelTrip.Index, driver.GetId()), false, driver);
            }
            #endif

            List<Trip> driverPath = info.DriverPaths[driver.AllDriversIndex];
            (Trip firstRelevantTrip, Trip lastRelevantTrip) = GetTripRelevantRange(addedHotelTrip, driverPath, info);
            Func<Trip, bool> newIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index] || trip == addedHotelTrip;
            (double costDiff, double costWithoutPenaltyDiff, double penaltyDiff, double satisfactionDiff, DriverInfo driverInfoDiff) = RangeCostDiffCalculator.GetRangeCostDiff(firstRelevantTrip, lastRelevantTrip, oldDriverInfo, newIsHotelAfterTrip, driver, driverPath, info);

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string relevantRangeInfo = GetNormalRangeInfo(firstRelevantTrip, lastRelevantTrip);
                CheckErrors(costDiff, costWithoutPenaltyDiff, penaltyDiff, satisfactionDiff, relevantRangeInfo, null, null, addedHotelTrip, null, driver, info);
            }
            #endif

            return (costDiff, costWithoutPenaltyDiff, penaltyDiff, satisfactionDiff, driverInfoDiff);
        }

        public static (double, double, double, double, DriverInfo) GetRemoveHotelDriverCostDiff(Trip removedHotelTrip, Driver driver, DriverInfo oldDriverInfo, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Remove hotel after {0} for driver {1}", removedHotelTrip.Index, driver.GetId()), false, driver);
            }
            #endif

            List<Trip> driverPath = info.DriverPaths[driver.AllDriversIndex];
            (Trip firstRelevantTrip, Trip lastRelevantTrip) = GetTripRelevantRange(removedHotelTrip, driverPath, info);
            Func<Trip, bool> newIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index] && trip != removedHotelTrip;
            (double costDiff, double costWithoutPenaltyDiff, double penaltyDiff, double satisfactionDiff, DriverInfo driverInfoDiff) = RangeCostDiffCalculator.GetRangeCostDiff(firstRelevantTrip, lastRelevantTrip, oldDriverInfo, newIsHotelAfterTrip, driver, driverPath, info);

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string relevantRangeInfo = GetNormalRangeInfo(firstRelevantTrip, lastRelevantTrip);
                CheckErrors(costDiff, costWithoutPenaltyDiff, penaltyDiff, satisfactionDiff, relevantRangeInfo, null, null, null, removedHotelTrip, driver, info);
            }
            #endif

            return (costDiff, costWithoutPenaltyDiff, penaltyDiff, satisfactionDiff, driverInfoDiff);
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

        static void CheckErrors(double costDiff, double costWithoutPenaltyDiff, double penaltyDiff, double satisfactionDiff, string relevantRangeInfo, Trip unassignedTrip, Trip assignedTrip, Trip addedHotel, Trip removedHotel, Driver driver, SaInfo info) {
            SaDebugger.GetCurrentNormalDiff().CostDiff = costDiff;
            SaDebugger.GetCurrentNormalDiff().CostWithoutPenaltyDiff = costWithoutPenaltyDiff;
            SaDebugger.GetCurrentNormalDiff().PenaltyDiff = penaltyDiff;
            SaDebugger.GetCurrentNormalDiff().DriverSatisfactionDiff = info.Instance.InternalDrivers.Length * satisfactionDiff;
            SaDebugger.GetCurrentNormalDiff().SatisfactionDiff = satisfactionDiff;
            SaDebugger.GetCurrentNormalDiff().RelevantRangeInfo = relevantRangeInfo;

            List<Trip> driverPathBefore = TotalCostCalculator.GetSingleDriverPath(driver, null, info);

            // Get total before
            TotalCostCalculator.GetDriverPathCost(driverPathBefore, info.IsHotelStayAfterTrip, driver, new PenaltyInfo(), info);
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
            TotalCostCalculator.GetDriverPathCost(driverPathAfter, isHotelStayAfterTripAfter, driver, new PenaltyInfo(), info);
            SaDebugger.GetCurrentOperationPart().FinishCheckAfter();

            // Check for errors
            SaDebugger.GetCurrentOperationPart().CheckErrors();
        }
    }
}

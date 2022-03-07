﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class CostDiffCalculator {
        public static (double, double, double, int) AssignOrUnassignTrip(bool isAssign, Trip trip, Trip tripToIgnore, Driver driver, int driverOldWorkedTime, Driver[] assignment, Instance instance, float penaltyFactor, int debugIterationNum) {
            if (Config.DebugCheckAndLogOperations) {
                string templateStr = isAssign ? "Assign trip {0} to driver {1}" : "Unassign trip {0} from driver {1}";
                SaDebugger.GetCurrentOperation().StartPart(string.Format(templateStr, trip.Index, driver.Index), isAssign, driver);
            }

            // Get related trips
            (Trip prevTripInternal, Trip nextTripInternal, Trip firstTripInternal, Trip lastTripInternal, Trip prevShiftFirstTrip, Trip prevShiftLastTrip, Trip nextShiftFirstTrip) = GetRelatedTrips(trip, tripToIgnore, driver, assignment, instance);

            // Cost diffs
            int unassignShiftLengthDiff;
            float unassignBasePenaltyDiff;
            if (prevTripInternal == null) {
                if (nextTripInternal == null) {
                    // This is the only trip of this shift
                    (unassignShiftLengthDiff, unassignBasePenaltyDiff) = CostDiffByTripPosition.UnassignOnlyTrip(trip, firstTripInternal, lastTripInternal, prevShiftFirstTrip, prevShiftLastTrip, nextShiftFirstTrip, driver, assignment, instance);
                } else {
                    // This is the first trip of this shift
                    (unassignShiftLengthDiff, unassignBasePenaltyDiff) = CostDiffByTripPosition.UnassignFirstTrip(trip, nextTripInternal, firstTripInternal, lastTripInternal, prevShiftFirstTrip, prevShiftLastTrip, nextShiftFirstTrip, driver, assignment, instance);
                }
            } else {
                if (nextTripInternal == null) {
                    // This is the last trip of this shift
                    (unassignShiftLengthDiff, unassignBasePenaltyDiff) = CostDiffByTripPosition.UnassignLastTrip(trip, prevTripInternal, firstTripInternal, lastTripInternal, prevShiftFirstTrip, prevShiftLastTrip, nextShiftFirstTrip, driver, assignment, instance);
                } else {
                    // This is a middle trip of this shift
                    (unassignShiftLengthDiff, unassignBasePenaltyDiff) = CostDiffByTripPosition.UnassignMiddleTrip(trip, prevTripInternal, nextTripInternal, firstTripInternal, driver, instance);
                }
            }

            // Get correct values for this type of operation
            int shiftLengthDiff;
            float basePenaltyDiff;
            if (isAssign) {
                shiftLengthDiff = -unassignShiftLengthDiff;
                basePenaltyDiff = -unassignBasePenaltyDiff;
            } else {
                shiftLengthDiff = unassignShiftLengthDiff;
                basePenaltyDiff = unassignBasePenaltyDiff;
            }

            // Contract time penalty
            basePenaltyDiff += CostHelper.GetContractTimeBasePenaltyDiff(driverOldWorkedTime, driverOldWorkedTime + shiftLengthDiff, driver);

            // Get costs
            double costWithoutPenaltyDiff = shiftLengthDiff * Config.SalaryRate;
            double costDiff = costWithoutPenaltyDiff + basePenaltyDiff * penaltyFactor;

            // Debugger
            if (Config.DebugCheckAndLogOperations) {
                StoreDebuggerInfo(prevTripInternal, nextTripInternal, firstTripInternal, lastTripInternal, prevShiftFirstTrip, prevShiftLastTrip, nextShiftFirstTrip, costDiff, costWithoutPenaltyDiff, basePenaltyDiff, instance);
                CheckErrors(isAssign, trip, tripToIgnore, driver, assignment, instance, penaltyFactor, shiftLengthDiff);
            }

            return (costDiff, costWithoutPenaltyDiff, basePenaltyDiff, shiftLengthDiff);
        }

        /** Get required related trips for a given trip */
        static (Trip, Trip, Trip, Trip, Trip, Trip, Trip) GetRelatedTrips(Trip trip, Trip tripToIgnore, Driver driver, Driver[] assignment, Instance instance) {
            // Get if they exist: 1) previous internal trip, 2) first internal trip, 3) last trip of previous shift, and 4) first trip of previous shift
            (Trip prevTripInternal, Trip prevTripExternal) = CostHelper.GetPrevTrip(trip, driver, tripToIgnore, assignment, instance);
            Trip firstTripInternal, prevShiftLastTrip, prevShiftFirstTrip;
            if (prevTripInternal == null) {
                // We are unassigning first trip of shift
                firstTripInternal = trip;
                prevShiftLastTrip = prevTripExternal;

                if (prevShiftLastTrip == null) {
                    // There is no previous shift
                    prevShiftFirstTrip = null;
                } else {
                    // There is a previous shift
                    (prevShiftFirstTrip, _) = CostHelper.GetPrevTrip(trip, driver, tripToIgnore, assignment, instance);
                    if (prevShiftFirstTrip == null) prevShiftFirstTrip = prevShiftLastTrip;
                }
            } else {
                // We are unassigning non-first trip of shift
                (firstTripInternal, prevShiftLastTrip) = CostHelper.GetFirstTripInternalAndPrevShiftTrip(prevTripInternal, driver, tripToIgnore, assignment, instance);

                // There may be a previous shift, but we don't need it
                prevShiftFirstTrip = null;
            }

            // Get if they exist: 1) next internal trip, 2) last internal trip, and 3) first trip of next shift
            (Trip nextTripInternal, Trip nextTripExternal) = CostHelper.GetNextTrip(trip, driver, tripToIgnore, assignment, instance);
            Trip lastTripInternal, nextShiftFirstTrip;
            if (nextTripInternal == null) {
                // We are unassigning last trip of shift
                lastTripInternal = trip;
                nextShiftFirstTrip = nextTripExternal;
            } else {
                // We are unassigning non-last trip of shift
                (lastTripInternal, nextShiftFirstTrip) = CostHelper.GetLastTripInternalAndNextShiftTrip(nextTripInternal, driver, tripToIgnore, assignment, instance);
            }

            return (prevTripInternal, nextTripInternal, firstTripInternal, lastTripInternal, prevShiftFirstTrip, prevShiftLastTrip, nextShiftFirstTrip);
        }

        static void StoreDebuggerInfo(Trip prevTripInternal, Trip nextTripInternal, Trip firstTripInternal, Trip lastTripInternal, Trip prevShiftFirstTrip, Trip prevShiftLastTrip, Trip nextShiftFirstTrip, double costDiff, double costWithoutPenaltyDiff, double basePenaltyDiff, Instance instance) {
            // Related trips
            SaDebugger.GetCurrentNormalDiff().PrevTripInternal = prevTripInternal;
            SaDebugger.GetCurrentNormalDiff().NextTripInternal = nextTripInternal;
            SaDebugger.GetCurrentNormalDiff().FirstTripInternal = firstTripInternal;
            SaDebugger.GetCurrentNormalDiff().LastTripInternal = lastTripInternal;
            SaDebugger.GetCurrentNormalDiff().PrevShiftFirstTrip = prevShiftFirstTrip;
            SaDebugger.GetCurrentNormalDiff().PrevShiftLastTrip = prevShiftLastTrip;
            SaDebugger.GetCurrentNormalDiff().NextShiftFirstTrip = nextShiftFirstTrip;

            // Trip position
            if (prevTripInternal == null) {
                if (nextTripInternal == null) SaDebugger.GetCurrentNormalDiff().TripPosition = "Only";
                else SaDebugger.GetCurrentNormalDiff().TripPosition = "First";
            } else {
                if (nextTripInternal == null) SaDebugger.GetCurrentNormalDiff().TripPosition = "Last";
                else SaDebugger.GetCurrentNormalDiff().TripPosition = "Middle";
            }

            // Shift position
            if (prevShiftLastTrip == null) {
                if (nextShiftFirstTrip == null) SaDebugger.GetCurrentNormalDiff().ShiftPosition = "Only";
                else SaDebugger.GetCurrentNormalDiff().ShiftPosition = "First";
            } else {
                if (nextShiftFirstTrip == null) SaDebugger.GetCurrentNormalDiff().ShiftPosition = "Last";
                else SaDebugger.GetCurrentNormalDiff().ShiftPosition = "Middle";
            }

            // Merge/split info
            SaDebugger.GetCurrentNormalDiff().MergeSplitInfo = "N/A";
            if (prevTripInternal == null) {
                if (nextTripInternal == null) {
                    // Only trip
                    if (prevShiftLastTrip != null && nextShiftFirstTrip != null) {
                        if (CostHelper.AreSameShift(prevShiftLastTrip, nextShiftFirstTrip, instance)) {
                            SaDebugger.GetCurrentNormalDiff().MergeSplitInfo = "Merge";
                        } else {
                            SaDebugger.GetCurrentNormalDiff().MergeSplitInfo = "No merge";
                        }
                    }
                } else {
                    // First trip
                    if (prevShiftLastTrip != null) {
                        if (CostHelper.AreSameShift(prevShiftLastTrip, firstTripInternal, instance)) {
                            SaDebugger.GetCurrentNormalDiff().MergeSplitInfo = "Merge";
                        } else {
                            SaDebugger.GetCurrentNormalDiff().MergeSplitInfo = "No merge";
                        }
                    }
                }
            } else {
                if (nextTripInternal == null) {
                    // Last trip
                    if (nextShiftFirstTrip != null) {
                        if (CostHelper.AreSameShift(lastTripInternal, nextShiftFirstTrip, instance)) {
                            SaDebugger.GetCurrentNormalDiff().MergeSplitInfo = "Merge";
                        } else {
                            SaDebugger.GetCurrentNormalDiff().MergeSplitInfo = "No merge";
                        }
                    }
                } else {
                    // Middle trip
                    if (CostHelper.AreSameShift(prevTripInternal, nextTripInternal, instance)) {
                        SaDebugger.GetCurrentNormalDiff().MergeSplitInfo = "No split";
                    } else {
                        SaDebugger.GetCurrentNormalDiff().MergeSplitInfo = "Split";
                    }
                }
            }

            // Cost diffs
            SaDebugger.GetCurrentNormalDiff().CostDiff = costDiff;
            SaDebugger.GetCurrentNormalDiff().CostWithoutPenaltyDiff = costWithoutPenaltyDiff;
            SaDebugger.GetCurrentNormalDiff().BasePenaltyDiff = basePenaltyDiff;
        }

        static void CheckErrors(bool isAssign, Trip trip, Trip tripToIgnore, Driver driver, Driver[] assignment, Instance instance, float penaltyFactor, int iterationNum) {
            List<Trip> driverPathBefore = TotalCostCalculator.GetSingleDriverPath(driver, tripToIgnore, assignment, instance);

            // Get total before
            TotalCostCalculator.GetDriverPathCost(driverPathBefore, driver, instance, penaltyFactor);
            SaDebugger.GetCurrentOperationPart().FinishCheckBefore();

            // Get driver path after
            List<Trip> driverPathAfter = driverPathBefore.Copy();
            if (isAssign) {
                driverPathAfter.Add(trip);
                driverPathAfter = driverPathAfter.OrderBy(searchTrip => searchTrip.StartTime).ToList();
            } else {
                int removedCount = driverPathAfter.RemoveAll(searchTrip => searchTrip.Index == trip.Index);
                if (removedCount != 1) throw new Exception("Error removing trip from driver path");
            }

            // Get total after
            TotalCostCalculator.GetDriverPathCost(driverPathAfter, driver, instance, penaltyFactor);
            SaDebugger.GetCurrentOperationPart().FinishCheckAfter();

            // Check for errors
            SaDebugger.GetCurrentOperationPart().CheckErrors();
        }
    }
}

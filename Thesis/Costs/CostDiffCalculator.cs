﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class CostDiffCalculator {
        public static (double, double, double, int) AssignOrUnassignTrip(bool isAssign, Trip trip, Trip tripToIgnore, Driver driver, int driverOldWorkedTime, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string templateStr = isAssign ? "Assign trip {0} to driver {1}" : "Unassign trip {0} from driver {1}";
                SaDebugger.GetCurrentOperation().StartPart(string.Format(templateStr, trip.Index, driver.GetId()), isAssign, driver);
            }
            #endif

            // Get related trips
            (Trip prevTripInternal, Trip nextTripInternal, Trip firstTripInternal, Trip lastTripInternal, Trip prevShiftFirstTrip, Trip prevShiftLastTrip, Trip nextShiftFirstTrip) = GetRelatedTrips(trip, tripToIgnore, driver, info);

            // Cost diffs
            int unassignShiftLengthDiff;
            float unassignCostWithoutPenaltyDiff, unassignBasePenaltyDiff;
            if (prevTripInternal == null) {
                if (nextTripInternal == null) {
                    // This is the only trip of this shift
                    (unassignShiftLengthDiff, unassignCostWithoutPenaltyDiff, unassignBasePenaltyDiff) = CostDiffByTripPosition.UnassignOnlyTrip(trip, tripToIgnore, firstTripInternal, lastTripInternal, prevShiftFirstTrip, prevShiftLastTrip, nextShiftFirstTrip, driver, info);
                } else {
                    // This is the first trip of this shift
                    (unassignShiftLengthDiff, unassignCostWithoutPenaltyDiff, unassignBasePenaltyDiff) = CostDiffByTripPosition.UnassignFirstTrip(trip, nextTripInternal, firstTripInternal, lastTripInternal, prevShiftFirstTrip, prevShiftLastTrip, nextShiftFirstTrip, driver, info);
                }
            } else {
                if (nextTripInternal == null) {
                    // This is the last trip of this shift
                    (unassignShiftLengthDiff, unassignCostWithoutPenaltyDiff, unassignBasePenaltyDiff) = CostDiffByTripPosition.UnassignLastTrip(trip, tripToIgnore, prevTripInternal, firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, info);
                } else {
                    // This is a middle trip of this shift
                    (unassignShiftLengthDiff, unassignCostWithoutPenaltyDiff, unassignBasePenaltyDiff) = CostDiffByTripPosition.UnassignMiddleTrip(trip, prevTripInternal, nextTripInternal, firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, info);
                }
            }

            // Get correct values for this type of operation
            int shiftLengthDiff;
            float costWithoutPenaltyDiff, basePenaltyDiff;
            if (isAssign) {
                shiftLengthDiff = -unassignShiftLengthDiff;
                costWithoutPenaltyDiff = -unassignCostWithoutPenaltyDiff;
                basePenaltyDiff = -unassignBasePenaltyDiff;
            } else {
                shiftLengthDiff = unassignShiftLengthDiff;
                costWithoutPenaltyDiff = unassignCostWithoutPenaltyDiff;
                basePenaltyDiff = unassignBasePenaltyDiff;
            }

            // Contract time penalty
            basePenaltyDiff += driver.GetContractTimeBasePenalty(driverOldWorkedTime + shiftLengthDiff, true) - driver.GetContractTimeBasePenalty(driverOldWorkedTime, false);

            // Get costs
            double costDiff = costWithoutPenaltyDiff + basePenaltyDiff * info.PenaltyFactor;

            // TODO: deal with hotel stays

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                StoreDebuggerInfo(prevTripInternal, nextTripInternal, firstTripInternal, lastTripInternal, prevShiftFirstTrip, prevShiftLastTrip, nextShiftFirstTrip, costDiff, costWithoutPenaltyDiff, basePenaltyDiff, info);
                CheckErrors(isAssign, trip, tripToIgnore, driver, info);
            }
            #endif

            return (costDiff, costWithoutPenaltyDiff, basePenaltyDiff, shiftLengthDiff);
        }

        /** Get required related trips for a given trip */
        static (Trip, Trip, Trip, Trip, Trip, Trip, Trip) GetRelatedTrips(Trip trip, Trip tripToIgnore, Driver driver, SaInfo info) {
            // Get if they exist: 1) previous internal trip, 2) first internal trip, 3) last trip of previous shift, and 4) first trip of previous shift
            (Trip prevTripInternal, Trip prevTripExternal) = AssignmentHelper.GetPrevTrip(trip, driver, tripToIgnore, info);
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
                    (prevShiftFirstTrip, _) = AssignmentHelper.GetFirstTripInternalAndPrevShiftTrip(prevShiftLastTrip, driver, tripToIgnore, info);
                    if (prevShiftFirstTrip == null) prevShiftFirstTrip = prevShiftLastTrip;
                }
            } else {
                // We are unassigning non-first trip of shift
                (firstTripInternal, prevShiftLastTrip) = AssignmentHelper.GetFirstTripInternalAndPrevShiftTrip(prevTripInternal, driver, tripToIgnore, info);

                // There may be a previous shift, but we don't need it
                prevShiftFirstTrip = null;
            }

            // Get if they exist: 1) next internal trip, 2) last internal trip, and 3) first trip of next shift
            (Trip nextTripInternal, Trip nextTripExternal) = AssignmentHelper.GetNextTrip(trip, driver, tripToIgnore, info);
            Trip lastTripInternal, nextShiftFirstTrip;
            if (nextTripInternal == null) {
                // We are unassigning last trip of shift
                lastTripInternal = trip;
                nextShiftFirstTrip = nextTripExternal;
            } else {
                // We are unassigning non-last trip of shift
                (lastTripInternal, nextShiftFirstTrip) = AssignmentHelper.GetLastTripInternalAndNextShiftTrip(nextTripInternal, driver, tripToIgnore, info);
            }

            return (prevTripInternal, nextTripInternal, firstTripInternal, lastTripInternal, prevShiftFirstTrip, prevShiftLastTrip, nextShiftFirstTrip);
        }

        static void StoreDebuggerInfo(Trip prevTripInternal, Trip nextTripInternal, Trip firstTripInternal, Trip lastTripInternal, Trip prevShiftFirstTrip, Trip prevShiftLastTrip, Trip nextShiftFirstTrip, double costDiff, double costWithoutPenaltyDiff, double basePenaltyDiff, SaInfo info) {
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
                        if (info.Instance.AreSameShift(prevShiftLastTrip, nextShiftFirstTrip)) {
                            SaDebugger.GetCurrentNormalDiff().MergeSplitInfo = "Merge";
                        } else {
                            SaDebugger.GetCurrentNormalDiff().MergeSplitInfo = "No merge";
                        }
                    }
                } else {
                    // First trip
                    if (prevShiftLastTrip != null) {
                        if (info.Instance.AreSameShift(prevShiftLastTrip, nextTripInternal)) {
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
                        if (info.Instance.AreSameShift(prevTripInternal, nextShiftFirstTrip)) {
                            SaDebugger.GetCurrentNormalDiff().MergeSplitInfo = "Merge";
                        } else {
                            SaDebugger.GetCurrentNormalDiff().MergeSplitInfo = "No merge";
                        }
                    }
                } else {
                    // Middle trip
                    if (info.Instance.AreSameShift(prevTripInternal, nextTripInternal)) {
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

        static void CheckErrors(bool isAssign, Trip trip, Trip tripToIgnore, Driver driver, SaInfo info) {
            List<Trip> driverPathBefore = TotalCostCalculator.GetSingleDriverPath(driver, tripToIgnore, info);

            // Get total before
            TotalCostCalculator.GetDriverPathCost(driverPathBefore, info.IsHotelStayAfterTrip, driver, info);
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

            // Get hotel stays after
            bool[] isHotelStayAfterTripAfter = info.IsHotelStayAfterTrip; // WIP

            // Get total after
            TotalCostCalculator.GetDriverPathCost(driverPathAfter, isHotelStayAfterTripAfter, driver, info);
            SaDebugger.GetCurrentOperationPart().FinishCheckAfter();

            // Check for errors
            SaDebugger.GetCurrentOperationPart().CheckErrors();
        }
    }
}

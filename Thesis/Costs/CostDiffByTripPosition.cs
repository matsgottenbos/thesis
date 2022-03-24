using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class CostDiffByTripPosition {
        /** Unassign the only trip in a shift; returns 1) shift length diff, 2) shift cost diff, and 3) base penalty diff */
        public static (int, float, float) UnassignOnlyTrip(Trip tripToUnassign, Trip tripToIgnore, Trip firstTripInternal, Trip lastTripInternal, Trip prevShiftFirstTrip, Trip prevShiftLastTrip, Trip nextShiftFirstTrip, Driver driver, SaInfo info) {
            // Always: internal shift length diff (-S1)
            (int shiftLengthDiff, float costWithoutPenaltyDiff, float basePenaltyDiff) = CostDiffBasic.UnassignOnlyTripInternal(tripToUnassign, driver);

            if (prevShiftLastTrip != null) {
                // Prev shift: remove rest before-current (-R1)
                basePenaltyDiff -= PenaltyHelper.GetRestTimeBasePenalty(prevShiftFirstTrip, prevShiftLastTrip, firstTripInternal, driver, false);

                if (nextShiftFirstTrip != null) {
                    // Next shift: remove rest current-after (-R2)
                    basePenaltyDiff -= PenaltyHelper.GetRestTimeBasePenalty(firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, false);

                    if (info.Instance.AreSameShift(prevShiftLastTrip, nextShiftFirstTrip)) {
                        // Prev shift + next shift + merge: merge previous and next shifts (M1)
                        (Trip nextShiftLastTrip, _) = AssignmentHelper.GetLastTripInternalAndNextShiftTrip(nextShiftFirstTrip, driver, tripToIgnore, info);
                        (int externalShiftLengthDiff, float externalShiftCostWithoutPenaltyDiff, float externalBasePenaltyDiff) = CostDiffBasic.MergeShifts(prevShiftFirstTrip, prevShiftLastTrip, nextShiftFirstTrip, nextShiftLastTrip, driver);
                        shiftLengthDiff += externalShiftLengthDiff;
                        costWithoutPenaltyDiff += externalShiftCostWithoutPenaltyDiff;
                        basePenaltyDiff += externalBasePenaltyDiff;
                        basePenaltyDiff += PenaltyHelper.GetPrecedenceBasePenalty(prevShiftLastTrip, nextShiftFirstTrip, info, true);
                    } else {
                        // Prev shift + next shift + no merge: add rest previous-next (R3)
                        basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(prevShiftFirstTrip, prevShiftLastTrip, nextShiftFirstTrip, driver, true);
                    }
                }
            } else {
                if (nextShiftFirstTrip != null) {
                    // Next shift: remove rest after (-R2)
                    basePenaltyDiff -= PenaltyHelper.GetRestTimeBasePenalty(firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, false);
                }
            }

            return (shiftLengthDiff, costWithoutPenaltyDiff, basePenaltyDiff);
        }

        /** Unassign the first trip in a shift; returns 1) shift length diff, 2) shift cost diff, and 3) base penalty diff */
        public static (int, float, float) UnassignFirstTrip(Trip tripToUnassign, Trip nextTripInternal, Trip firstTripInternal, Trip lastTripInternal, Trip prevShiftFirstTrip, Trip prevShiftLastTrip, Trip nextShiftFirstTrip, Driver driver, SaInfo info) {
            int shiftLengthDiff;
            float costWithoutPenaltyDiff;
            float basePenaltyDiff = 0;

            if (prevShiftLastTrip != null) {
                // Prev shift: remove old rest before-current (-R1)
                basePenaltyDiff -= PenaltyHelper.GetRestTimeBasePenalty(prevShiftFirstTrip, prevShiftLastTrip, firstTripInternal, driver, false);

                if (nextShiftFirstTrip != null) {
                    // Next shift: remove old rest current-after (-R2)
                    basePenaltyDiff -= PenaltyHelper.GetRestTimeBasePenalty(firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, false);

                    if (info.Instance.AreSameShift(prevShiftLastTrip, nextTripInternal)) {
                        // Prev shift + merge: merge previous and current shifts (M2)
                        float externalBasePenaltyDiff;
                        (shiftLengthDiff, costWithoutPenaltyDiff, externalBasePenaltyDiff) = CostDiffBasic.MergeShifts(prevShiftFirstTrip, prevShiftLastTrip, firstTripInternal, lastTripInternal, driver);
                        basePenaltyDiff += externalBasePenaltyDiff;
                        basePenaltyDiff += PenaltyHelper.GetPrecedenceBasePenalty(tripToUnassign, nextTripInternal, info, true) - PenaltyHelper.GetPrecedenceBasePenalty(prevShiftLastTrip, nextTripInternal, info, false);

                        // Prev shift + next shift + merge: add new rest prev/current-next (R4)
                        basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(prevShiftFirstTrip, lastTripInternal, nextShiftFirstTrip, driver, true);
                    } else {
                        // No prev and/or no merge: internal shift length diff (S2 - S1)
                        float internalBasePenaltyDiff;
                        (shiftLengthDiff, costWithoutPenaltyDiff, internalBasePenaltyDiff) = CostDiffBasic.UnassignFirstTripInternal(tripToUnassign, nextTripInternal, lastTripInternal, driver, info);
                        basePenaltyDiff += internalBasePenaltyDiff;

                        // Prev shift + no merge: add new rest before-current (R5)
                        basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(prevShiftFirstTrip, prevShiftLastTrip, nextTripInternal, driver, true);

                        // Next shift: add new rest current-after (R6)
                        basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(nextTripInternal, lastTripInternal, nextShiftFirstTrip, driver, true);
                    }
                } else {
                    if (info.Instance.AreSameShift(prevShiftLastTrip, nextTripInternal)) {
                        // Prev shift + merge: merge previous and current shifts (M2)
                        float externalBasePenaltyDiff;
                        (shiftLengthDiff, costWithoutPenaltyDiff, externalBasePenaltyDiff) = CostDiffBasic.MergeShifts(prevShiftFirstTrip, prevShiftLastTrip, firstTripInternal, lastTripInternal, driver);
                        basePenaltyDiff += externalBasePenaltyDiff;
                        basePenaltyDiff += PenaltyHelper.GetPrecedenceBasePenalty(tripToUnassign, nextTripInternal, info, true) - PenaltyHelper.GetPrecedenceBasePenalty(prevShiftLastTrip, nextTripInternal, info, false);
                    } else {
                        // No prev shift and/or no merge: internal shift length diff (S2 - S1)
                        float internalBasePenaltyDiff;
                        (shiftLengthDiff, costWithoutPenaltyDiff, internalBasePenaltyDiff) = CostDiffBasic.UnassignFirstTripInternal(tripToUnassign, nextTripInternal, lastTripInternal, driver, info);
                        basePenaltyDiff += internalBasePenaltyDiff;

                        // Prev shift + no merge: add new rest before-current (R5)
                        basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(prevShiftFirstTrip, prevShiftLastTrip, nextTripInternal, driver, true);
                    }
                }
            } else {
                // No prev shift and/or no merge: internal shift length diff (S2 - S1)
                float internalBasePenaltyDiff;
                (shiftLengthDiff, costWithoutPenaltyDiff, internalBasePenaltyDiff) = CostDiffBasic.UnassignFirstTripInternal(tripToUnassign, nextTripInternal, lastTripInternal, driver, info);
                basePenaltyDiff += internalBasePenaltyDiff;

                if (nextShiftFirstTrip != null) {
                    // Next shift: remove rest after (-R2)
                    basePenaltyDiff -= PenaltyHelper.GetRestTimeBasePenalty(firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, false);

                    // Next shift + no merge: add new rest current-after (R6)
                    basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(nextTripInternal, lastTripInternal, nextShiftFirstTrip, driver, true);
                }
            }

            return (shiftLengthDiff, costWithoutPenaltyDiff, basePenaltyDiff);
        }

        /** Unassign the last trip in a shift; returns 1) shift length diff, 2) shift cost diff, and 3) base penalty diff */
        public static (int, float, float) UnassignLastTrip(Trip tripToUnassign, Trip tripToIgnore, Trip prevTripInternal, Trip firstTripInternal, Trip lastTripInternal, Trip nextShiftFirstTrip, Driver driver, SaInfo info) {
            int shiftLengthDiff;
            float costWithoutPenaltyDiff, basePenaltyDiff;
            if (nextShiftFirstTrip != null) {
                // Next shift: remove old rest current-after (-R2)
                basePenaltyDiff = -PenaltyHelper.GetRestTimeBasePenalty(firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, false);

                if (info.Instance.AreSameShift(prevTripInternal, nextShiftFirstTrip)) {
                    // Next shift + merge: merge current and previous shifts (M3)
                    (Trip nextShiftLastTrip, Trip secondNextShiftFirstTrip) = AssignmentHelper.GetLastTripInternalAndNextShiftTrip(nextShiftFirstTrip, driver, tripToIgnore, info);
                    float externalBasePenaltyDiff;
                    (shiftLengthDiff, costWithoutPenaltyDiff, externalBasePenaltyDiff) = CostDiffBasic.MergeShifts(firstTripInternal, lastTripInternal, nextShiftFirstTrip, nextShiftLastTrip, driver);
                    basePenaltyDiff += externalBasePenaltyDiff;
                    basePenaltyDiff += PenaltyHelper.GetPrecedenceBasePenalty(prevTripInternal, nextShiftFirstTrip, info, true) - PenaltyHelper.GetPrecedenceBasePenalty(prevTripInternal, tripToUnassign, info, false);

                    if (secondNextShiftFirstTrip != null) {
                        // Next shift + second next shift + merge: replace rest next-next2 with current-next2
                        basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(firstTripInternal, nextShiftLastTrip, secondNextShiftFirstTrip, driver, true) - PenaltyHelper.GetRestTimeBasePenalty(nextShiftFirstTrip, nextShiftLastTrip, secondNextShiftFirstTrip, driver, false);
                    }
                } else {
                    // No next shift and/or no merge: internal shift length diff (S3 - S1)
                    float internalBasePenaltyDiff;
                    (shiftLengthDiff, costWithoutPenaltyDiff, internalBasePenaltyDiff) = CostDiffBasic.UnassignLastTripInternal(tripToUnassign, prevTripInternal, firstTripInternal, driver, info);
                    basePenaltyDiff += internalBasePenaltyDiff;

                    // Next shift + no merge: add new rest current-after (R7)
                    basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(firstTripInternal, prevTripInternal, nextShiftFirstTrip, driver, true);
                }
            } else {
                // No next shift and/or no merge: internal shift length diff (S3 - S1)
                (shiftLengthDiff, costWithoutPenaltyDiff, basePenaltyDiff) = CostDiffBasic.UnassignLastTripInternal(tripToUnassign, prevTripInternal, firstTripInternal, driver, info);
            }

            return (shiftLengthDiff, costWithoutPenaltyDiff, basePenaltyDiff);
        }

        /** Unassign a middle trip in a shift; returns 1) shift length diff, 2) shift cost diff, and 3) base penalty diff */
        public static (int, float, float) UnassignMiddleTrip(Trip tripToUnassign, Trip prevTripInternal, Trip nextTripInternal, Trip firstTripInternal, Trip lastTripInternal, Trip nextShiftFirstTrip, Driver driver, SaInfo info) {
            int shiftLengthDiff;
            float costWithoutPenaltyDiff, basePenaltyDiff;
            if (info.Instance.AreSameShift(prevTripInternal, nextTripInternal)) {
                // No split: only precedence penalty changes
                (shiftLengthDiff, costWithoutPenaltyDiff, basePenaltyDiff) = CostDiffBasic.UnassignMiddleTripInternal(tripToUnassign, prevTripInternal, nextTripInternal, info);
            } else {
                // Split: split shift (Sp1)
                (shiftLengthDiff, costWithoutPenaltyDiff, basePenaltyDiff) = CostDiffBasic.SplitShift(firstTripInternal, prevTripInternal, nextTripInternal, lastTripInternal, driver);
                basePenaltyDiff -= PenaltyHelper.GetPrecedenceBasePenalty(prevTripInternal, tripToUnassign, info, false) + PenaltyHelper.GetPrecedenceBasePenalty(tripToUnassign, nextTripInternal, info, false);

                // Split: add new rest between split parts (R8)
                basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(firstTripInternal, prevTripInternal, nextTripInternal, driver, true);

                if (nextShiftFirstTrip != null) {
                    // Next shift + split: replace rest current-next with part2-next
                    basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(nextTripInternal, lastTripInternal, nextShiftFirstTrip, driver, true) - PenaltyHelper.GetRestTimeBasePenalty(firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, false);
                }
            }

            return (shiftLengthDiff, costWithoutPenaltyDiff, basePenaltyDiff);
        }
    }
}

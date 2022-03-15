﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class CostDiffByTripPosition {
        /** Unassign the only trip in a shift; returns 1) shift length diff, 2) shift cost diff, and 3) base penalty diff */
        public static (int, float, float) UnassignOnlyTrip(Trip tripToUnassign, Trip tripToIgnore, Trip firstTripInternal, Trip lastTripInternal, Trip prevShiftFirstTrip, Trip prevShiftLastTrip, Trip nextShiftFirstTrip, Driver driver, Driver[] assignment, Instance instance) {
            // Always: internal shift length diff (-S1)
            (int shiftLengthDiff, float costWithoutPenaltyDiff, float basePenaltyDiff) = CostDiffBasic.UnassignOnlyTripInternal(tripToUnassign, driver, instance);

            if (prevShiftLastTrip != null) {
                // Prev shift: remove rest before-current (-R1)
                basePenaltyDiff -= PenaltyHelper.GetRestTimeBasePenalty(prevShiftFirstTrip, prevShiftLastTrip, firstTripInternal, driver, instance, false);

                if (nextShiftFirstTrip != null) {
                    // Next shift: remove rest current-after (-R2)
                    basePenaltyDiff -= PenaltyHelper.GetRestTimeBasePenalty(firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, instance, false);

                    if (instance.AreSameShift(prevShiftLastTrip, nextShiftFirstTrip)) {
                        // Prev shift + next shift + merge: merge previous and next shifts (M1)
                        (Trip nextShiftLastTrip, _) = AssignmentHelper.GetLastTripInternalAndNextShiftTrip(nextShiftFirstTrip, driver, tripToIgnore, assignment, instance);
                        (int externalShiftLengthDiff, float externalShiftCostWithoutPenaltyDiff, float externalBasePenaltyDiff) = CostDiffBasic.MergeShifts(prevShiftFirstTrip, prevShiftLastTrip, nextShiftFirstTrip, nextShiftLastTrip, driver, instance);
                        shiftLengthDiff += externalShiftLengthDiff;
                        costWithoutPenaltyDiff += externalShiftCostWithoutPenaltyDiff;
                        basePenaltyDiff += externalBasePenaltyDiff;
                        basePenaltyDiff += PenaltyHelper.GetPrecedenceBasePenalty(prevShiftLastTrip, nextShiftFirstTrip, instance, true);
                    } else {
                        // Prev shift + next shift + no merge: add rest previous-next (R3)
                        basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(prevShiftFirstTrip, prevShiftLastTrip, nextShiftFirstTrip, driver, instance, true);
                    }
                }
            } else {
                if (nextShiftFirstTrip != null) {
                    // Next shift: remove rest after (-R2)
                    basePenaltyDiff -= PenaltyHelper.GetRestTimeBasePenalty(firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, instance, false);
                }
            }

            return (shiftLengthDiff, costWithoutPenaltyDiff, basePenaltyDiff);
        }

        /** Unassign the first trip in a shift; returns 1) shift length diff, 2) shift cost diff, and 3) base penalty diff */
        public static (int, float, float) UnassignFirstTrip(Trip tripToUnassign, Trip nextTripInternal, Trip firstTripInternal, Trip lastTripInternal, Trip prevShiftFirstTrip, Trip prevShiftLastTrip, Trip nextShiftFirstTrip, Driver driver, Driver[] assignment, Instance instance) {
            int shiftLengthDiff;
            float costWithoutPenaltyDiff;
            float basePenaltyDiff = 0;

            if (prevShiftLastTrip != null) {
                // Prev shift: remove old rest before-current (-R1)
                basePenaltyDiff -= PenaltyHelper.GetRestTimeBasePenalty(prevShiftFirstTrip, prevShiftLastTrip, firstTripInternal, driver, instance, false);

                if (nextShiftFirstTrip != null) {
                    // Next shift: remove old rest current-after (-R2)
                    basePenaltyDiff -= PenaltyHelper.GetRestTimeBasePenalty(firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, instance, false);

                    if (instance.AreSameShift(prevShiftLastTrip, nextTripInternal)) {
                        // Prev shift + merge: merge previous and current shifts (M2)
                        float externalBasePenaltyDiff;
                        (shiftLengthDiff, costWithoutPenaltyDiff, externalBasePenaltyDiff) = CostDiffBasic.MergeShifts(prevShiftFirstTrip, prevShiftLastTrip, firstTripInternal, lastTripInternal, driver, instance);
                        basePenaltyDiff += externalBasePenaltyDiff;
                        basePenaltyDiff += PenaltyHelper.GetPrecedenceBasePenalty(tripToUnassign, nextTripInternal, instance, true) - PenaltyHelper.GetPrecedenceBasePenalty(prevShiftLastTrip, nextTripInternal, instance, false);

                        // Prev shift + next shift + merge: add new rest prev/current-next (R4)
                        basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(prevShiftFirstTrip, lastTripInternal, nextShiftFirstTrip, driver, instance, true);
                    } else {
                        // No prev and/or no merge: internal shift length diff (S2 - S1)
                        float internalBasePenaltyDiff;
                        (shiftLengthDiff, costWithoutPenaltyDiff, internalBasePenaltyDiff) = CostDiffBasic.UnassignFirstTripInternal(tripToUnassign, nextTripInternal, lastTripInternal, driver, instance);
                        basePenaltyDiff += internalBasePenaltyDiff;

                        // Prev shift + no merge: add new rest before-current (R5)
                        basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(prevShiftFirstTrip, prevShiftLastTrip, nextTripInternal, driver, instance, true);

                        // Next shift: add new rest current-after (R6)
                        basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(nextTripInternal, lastTripInternal, nextShiftFirstTrip, driver, instance, true);
                    }
                } else {
                    if (instance.AreSameShift(prevShiftLastTrip, nextTripInternal)) {
                        // Prev shift + merge: merge previous and current shifts (M2)
                        float externalBasePenaltyDiff;
                        (shiftLengthDiff, costWithoutPenaltyDiff, externalBasePenaltyDiff) = CostDiffBasic.MergeShifts(prevShiftFirstTrip, prevShiftLastTrip, firstTripInternal, lastTripInternal, driver, instance);
                        basePenaltyDiff += externalBasePenaltyDiff;
                        basePenaltyDiff += PenaltyHelper.GetPrecedenceBasePenalty(tripToUnassign, nextTripInternal, instance, true) - PenaltyHelper.GetPrecedenceBasePenalty(prevShiftLastTrip, nextTripInternal, instance, false);
                    } else {
                        // No prev shift and/or no merge: internal shift length diff (S2 - S1)
                        float internalBasePenaltyDiff;
                        (shiftLengthDiff, costWithoutPenaltyDiff, internalBasePenaltyDiff) = CostDiffBasic.UnassignFirstTripInternal(tripToUnassign, nextTripInternal, lastTripInternal, driver, instance);
                        basePenaltyDiff += internalBasePenaltyDiff;

                        // Prev shift + no merge: add new rest before-current (R5)
                        basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(prevShiftFirstTrip, prevShiftLastTrip, nextTripInternal, driver, instance, true);
                    }
                }
            } else {
                // No prev shift and/or no merge: internal shift length diff (S2 - S1)
                float internalBasePenaltyDiff;
                (shiftLengthDiff, costWithoutPenaltyDiff, internalBasePenaltyDiff) = CostDiffBasic.UnassignFirstTripInternal(tripToUnassign, nextTripInternal, lastTripInternal, driver, instance);
                basePenaltyDiff += internalBasePenaltyDiff;

                if (nextShiftFirstTrip != null) {
                    // Next shift: remove rest after (-R2)
                    basePenaltyDiff -= PenaltyHelper.GetRestTimeBasePenalty(firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, instance, false);

                    // Next shift + no merge: add new rest current-after (R6)
                    basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(nextTripInternal, lastTripInternal, nextShiftFirstTrip, driver, instance, true);
                }
            }

            return (shiftLengthDiff, costWithoutPenaltyDiff, basePenaltyDiff);
        }

        /** Unassign the last trip in a shift; returns 1) shift length diff, 2) shift cost diff, and 3) base penalty diff */
        public static (int, float, float) UnassignLastTrip(Trip tripToUnassign, Trip tripToIgnore, Trip prevTripInternal, Trip firstTripInternal, Trip lastTripInternal, Trip nextShiftFirstTrip, Driver driver, Driver[] assignment, Instance instance) {
            int shiftLengthDiff;
            float costWithoutPenaltyDiff, basePenaltyDiff;
            if (nextShiftFirstTrip != null) {
                // Next shift: remove old rest current-after (-R2)
                basePenaltyDiff = -PenaltyHelper.GetRestTimeBasePenalty(firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, instance, false);

                if (instance.AreSameShift(prevTripInternal, nextShiftFirstTrip)) {
                    // Next shift + merge: merge current and previous shifts (M3)
                    (Trip nextShiftLastTrip, Trip secondNextShiftFirstTrip) = AssignmentHelper.GetLastTripInternalAndNextShiftTrip(nextShiftFirstTrip, driver, tripToIgnore, assignment, instance);
                    float externalBasePenaltyDiff;
                    (shiftLengthDiff, costWithoutPenaltyDiff, externalBasePenaltyDiff) = CostDiffBasic.MergeShifts(firstTripInternal, lastTripInternal, nextShiftFirstTrip, nextShiftLastTrip, driver, instance);
                    basePenaltyDiff += externalBasePenaltyDiff;
                    basePenaltyDiff += PenaltyHelper.GetPrecedenceBasePenalty(prevTripInternal, nextShiftFirstTrip, instance, true) - PenaltyHelper.GetPrecedenceBasePenalty(prevTripInternal, tripToUnassign, instance, false);

                    if (secondNextShiftFirstTrip != null) {
                        // Next shift + second next shift + merge: replace rest next-next2 with current-next2
                        basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(firstTripInternal, nextShiftLastTrip, secondNextShiftFirstTrip, driver, instance, true) - PenaltyHelper.GetRestTimeBasePenalty(nextShiftFirstTrip, nextShiftLastTrip, secondNextShiftFirstTrip, driver, instance, false);
                    }
                } else {
                    // No next shift and/or no merge: internal shift length diff (S3 - S1)
                    float internalBasePenaltyDiff;
                    (shiftLengthDiff, costWithoutPenaltyDiff, internalBasePenaltyDiff) = CostDiffBasic.UnassignLastTripInternal(tripToUnassign, prevTripInternal, firstTripInternal, driver, instance);
                    basePenaltyDiff += internalBasePenaltyDiff;

                    // Next shift + no merge: add new rest current-after (R7)
                    basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(firstTripInternal, prevTripInternal, nextShiftFirstTrip, driver, instance, true);
                }
            } else {
                // No next shift and/or no merge: internal shift length diff (S3 - S1)
                (shiftLengthDiff, costWithoutPenaltyDiff, basePenaltyDiff) = CostDiffBasic.UnassignLastTripInternal(tripToUnassign, prevTripInternal, firstTripInternal, driver, instance);
            }

            return (shiftLengthDiff, costWithoutPenaltyDiff, basePenaltyDiff);
        }

        /** Unassign a middle trip in a shift; returns 1) shift length diff, 2) shift cost diff, and 3) base penalty diff */
        public static (int, float, float) UnassignMiddleTrip(Trip tripToUnassign, Trip prevTripInternal, Trip nextTripInternal, Trip firstTripInternal, Trip lastTripInternal, Trip nextShiftFirstTrip, Driver driver, Instance instance) {
            int shiftLengthDiff;
            float costWithoutPenaltyDiff, basePenaltyDiff;
            if (instance.AreSameShift(prevTripInternal, nextTripInternal)) {
                // No split: only precedence penalty changes
                (shiftLengthDiff, costWithoutPenaltyDiff, basePenaltyDiff) = CostDiffBasic.UnassignMiddleTripInternal(tripToUnassign, prevTripInternal, nextTripInternal, driver, instance);
            } else {
                // Split: split shift (Sp1)
                (shiftLengthDiff, costWithoutPenaltyDiff, basePenaltyDiff) = CostDiffBasic.SplitShift(firstTripInternal, prevTripInternal, nextTripInternal, lastTripInternal, driver, instance);
                basePenaltyDiff -= PenaltyHelper.GetPrecedenceBasePenalty(prevTripInternal, tripToUnassign, instance, false) + PenaltyHelper.GetPrecedenceBasePenalty(tripToUnassign, nextTripInternal, instance, false);

                // Split: add new rest between split parts (R8)
                basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(firstTripInternal, prevTripInternal, nextTripInternal, driver, instance, true);

                if (nextShiftFirstTrip != null) {
                    // Next shift + split: replace rest current-next with part2-next
                    basePenaltyDiff += PenaltyHelper.GetRestTimeBasePenalty(nextTripInternal, lastTripInternal, nextShiftFirstTrip, driver, instance, true) - PenaltyHelper.GetRestTimeBasePenalty(firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, instance, false);
                }
            }

            return (shiftLengthDiff, costWithoutPenaltyDiff, basePenaltyDiff);
        }
    }
}
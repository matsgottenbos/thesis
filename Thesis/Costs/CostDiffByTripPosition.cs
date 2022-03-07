using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class CostDiffByTripPosition {
        /** Unassign the only trip in a shift; returns 1) shift length diff and 2) base penalty diff */
        public static (int, float) UnassignOnlyTrip(Trip tripToUnassign, Trip firstTripInternal, Trip lastTripInternal, Trip prevShiftFirstTrip, Trip prevShiftLastTrip, Trip nextShiftFirstTrip, Driver driver, Driver[] assignment, Instance instance) {
            // Always: internal shift length diff (-S1)
            (int shiftLengthDiff, float basePenaltyDiff) = CostDiffBasic.UnassignOnlyTripInternal(tripToUnassign, driver, instance);

            if (prevShiftLastTrip != null) {
                // Prev shift: remove rest before-current (-R1)
                basePenaltyDiff -= CostHelper.GetRestTimeBasePenalty(prevShiftFirstTrip, prevShiftLastTrip, firstTripInternal, driver, instance, false);

                if (Config.DebugCheckAndLogOperations) {
                    int oldRestTimeNext = CostHelper.RestTime(prevShiftFirstTrip, prevShiftLastTrip, firstTripInternal, driver, instance);
                    SaDebugger.GetCurrentNormalDiff().RestTime.AddOld(oldRestTimeNext, driver);
                }

                if (nextShiftFirstTrip != null) {
                    // Next shift: remove rest current-after (-R2)
                    basePenaltyDiff -= CostHelper.GetRestTimeBasePenalty(firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, instance, false);

                    if (Config.DebugCheckAndLogOperations) {
                        int oldRestTimeNext = CostHelper.RestTime(prevShiftFirstTrip, prevShiftLastTrip, firstTripInternal, driver, instance);
                        SaDebugger.GetCurrentNormalDiff().RestTime.AddOld(oldRestTimeNext, driver);
                    }

                    if (CostHelper.AreSameShift(prevShiftLastTrip, nextShiftFirstTrip, instance)) {
                        // Prev shift + next shift + merge: merge previous and next shifts (M1)
                        (Trip nextShiftLastTrip, _) = CostHelper.GetLastTripInternalAndNextShiftTrip(nextShiftFirstTrip, driver, null, assignment, instance);
                        (int externalShiftLengthDiff, float externalBasePenaltyDiff) = CostDiffBasic.MergeShifts(prevShiftFirstTrip, prevShiftLastTrip, nextShiftFirstTrip, nextShiftLastTrip, driver, instance);
                        shiftLengthDiff += externalShiftLengthDiff;
                        basePenaltyDiff += externalBasePenaltyDiff;
                    } else {
                        // Prev shift + next shift + no merge: add rest previous-next (R3)
                        basePenaltyDiff += CostHelper.GetRestTimeBasePenalty(prevShiftFirstTrip, prevShiftLastTrip, nextShiftFirstTrip, driver, instance, true);
                    }
                }
            } else {
                if (nextShiftFirstTrip != null) {
                    // Next shift: remove rest after (-R2)
                    basePenaltyDiff -= CostHelper.GetRestTimeBasePenalty(firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, instance, false);
                }
            }

            return (shiftLengthDiff, basePenaltyDiff);
        }

        /** Unassign the first trip in a shift; returns 1) shift length diff and 2) base penalty diff */
        public static (int, float) UnassignFirstTrip(Trip tripToUnassign, Trip nextTripInternal, Trip firstTripInternal, Trip lastTripInternal, Trip prevShiftFirstTrip, Trip prevShiftLastTrip, Trip nextShiftFirstTrip, Driver driver, Driver[] assignment, Instance instance) {
            int shiftLengthDiff;
            float basePenaltyDiff = 0;

            if (prevShiftLastTrip != null) {
                // Prev shift: remove old rest before-current (-R1)
                basePenaltyDiff -= CostHelper.GetRestTimeBasePenalty(prevShiftFirstTrip, prevShiftLastTrip, firstTripInternal, driver, instance, false);

                if (nextShiftFirstTrip != null) {
                    // Next shift: remove old rest current-after (-R2)
                    basePenaltyDiff -= CostHelper.GetRestTimeBasePenalty(firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, instance, false);

                    if (CostHelper.AreSameShift(prevShiftLastTrip, firstTripInternal, instance)) {
                        // Prev shift + merge: merge previous and current shifts (M2)
                        float externalBasePenaltyDiff;
                        (shiftLengthDiff, externalBasePenaltyDiff) = CostDiffBasic.MergeShifts(prevShiftFirstTrip, prevShiftLastTrip, firstTripInternal, lastTripInternal, driver, instance);
                        basePenaltyDiff += externalBasePenaltyDiff;

                        // Prev shift + next shift + merge: add new rest prev/current-next (R4)
                        basePenaltyDiff += CostHelper.GetRestTimeBasePenalty(prevShiftFirstTrip, lastTripInternal, nextShiftFirstTrip, driver, instance, true);
                    } else {
                        // No prev and/or no merge: internal shift length diff (S2 - S1)
                        float internalBasePenaltyDiff;
                        (shiftLengthDiff, internalBasePenaltyDiff) = CostDiffBasic.UnassignFirstTripInternal(tripToUnassign, nextTripInternal, lastTripInternal, driver, instance);
                        basePenaltyDiff += internalBasePenaltyDiff;

                        // Prev shift + no merge: add new rest before-current (R5)
                        basePenaltyDiff += CostHelper.GetRestTimeBasePenalty(prevShiftFirstTrip, prevShiftLastTrip, nextTripInternal, driver, instance, true);

                        // Next shift: add new rest current-after (R6)
                        basePenaltyDiff += CostHelper.GetRestTimeBasePenalty(nextTripInternal, lastTripInternal, nextShiftFirstTrip, driver, instance, true);
                    }
                } else {
                    if (CostHelper.AreSameShift(prevShiftLastTrip, firstTripInternal, instance)) {
                        // Prev shift + merge: merge previous and current shifts (M2)
                        float externalBasePenaltyDiff;
                        (shiftLengthDiff, externalBasePenaltyDiff) = CostDiffBasic.MergeShifts(prevShiftFirstTrip, prevShiftLastTrip, firstTripInternal, lastTripInternal, driver, instance);
                        basePenaltyDiff += externalBasePenaltyDiff;

                    } else {
                        // No prev shift and/or no merge: internal shift length diff (S2 - S1)
                        float internalBasePenaltyDiff;
                        (shiftLengthDiff, internalBasePenaltyDiff) = CostDiffBasic.UnassignFirstTripInternal(tripToUnassign, nextTripInternal, lastTripInternal, driver, instance);
                        basePenaltyDiff += internalBasePenaltyDiff;

                        // Prev shift + no merge: add new rest before-current (R5)
                        basePenaltyDiff += CostHelper.GetRestTimeBasePenalty(prevShiftFirstTrip, prevShiftLastTrip, nextTripInternal, driver, instance, true);
                    }
                }
            } else {
                // No prev shift and/or no merge: internal shift length diff (S2 - S1)
                float internalBasePenaltyDiff;
                (shiftLengthDiff, internalBasePenaltyDiff) = CostDiffBasic.UnassignFirstTripInternal(tripToUnassign, nextTripInternal, lastTripInternal, driver, instance);
                basePenaltyDiff += internalBasePenaltyDiff;

                if (nextShiftFirstTrip != null) {
                    // Next shift: remove rest after (-R2)
                    basePenaltyDiff -= CostHelper.GetRestTimeBasePenalty(firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, instance, false);

                    if (!CostHelper.AreSameShift(prevShiftLastTrip, firstTripInternal, instance)) {
                        // Next shift: add new rest current-after (R6)
                        basePenaltyDiff += CostHelper.GetRestTimeBasePenalty(nextTripInternal, lastTripInternal, nextShiftFirstTrip, driver, instance, true);
                    }
                }
            }

            return (shiftLengthDiff, basePenaltyDiff);
        }

        /** Unassign the last trip in a shift; returns 1) shift length diff and 2) base penalty diff */
        public static (int, float) UnassignLastTrip(Trip tripToUnassign, Trip prevTripInternal, Trip firstTripInternal, Trip lastTripInternal, Trip prevShiftFirstTrip, Trip prevShiftLastTrip, Trip nextShiftFirstTrip, Driver driver, Driver[] assignment, Instance instance) {
            int shiftLengthDiff;
            float basePenaltyDiff;
            if (nextShiftFirstTrip != null) {
                // Next shift: remove old rest current-after (-R2)
                basePenaltyDiff = -CostHelper.GetRestTimeBasePenalty(firstTripInternal, lastTripInternal, nextShiftFirstTrip, driver, instance, false);

                if (CostHelper.AreSameShift(lastTripInternal, nextShiftFirstTrip, instance)) {
                    // Next shift + merge: merge current and previous shifts (M3)
                    float externalBasePenaltyDiff;
                    (shiftLengthDiff, externalBasePenaltyDiff) = CostDiffBasic.MergeShifts(prevShiftFirstTrip, prevShiftLastTrip, firstTripInternal, lastTripInternal, driver, instance);
                    basePenaltyDiff += externalBasePenaltyDiff;
                } else {
                    // No next shift and/or no merge: internal shift length diff (S3 - S1)
                    float internalBasePenaltyDiff;
                    (shiftLengthDiff, internalBasePenaltyDiff) = CostDiffBasic.UnassignLastTripInternal(tripToUnassign, prevTripInternal, firstTripInternal, driver, instance);
                    basePenaltyDiff += internalBasePenaltyDiff;

                    // Next shift + no merge: add new rest current-after (R7)
                    basePenaltyDiff += CostHelper.GetRestTimeBasePenalty(firstTripInternal, prevTripInternal, nextShiftFirstTrip, driver, instance, true);
                }
            } else {
                // No next shift and/or no merge: internal shift length diff (S3 - S1)
                (shiftLengthDiff, basePenaltyDiff) = CostDiffBasic.UnassignLastTripInternal(tripToUnassign, prevTripInternal, firstTripInternal, driver, instance);
            }

            return (shiftLengthDiff, basePenaltyDiff);
        }

        /** Unassign a middle trip in a shift; returns 1) shift length diff and 2) base penalty diff */
        public static (int, float) UnassignMiddleTrip(Trip tripToUnassign, Trip prevTripInternal, Trip nextTripInternal, Trip firstTripInternal, Driver driver, Instance instance) {
            // Always: internal shift length diff (Sp1)
            (int shiftLengthDiff, float basePenaltyDiff) = CostDiffBasic.UnassignMiddleTripInternal(tripToUnassign, prevTripInternal, nextTripInternal, driver, instance);

            if (!CostHelper.AreSameShift(prevTripInternal, nextTripInternal, instance)) {
                // Split: add new rest between split parts (R8)
                basePenaltyDiff += CostHelper.GetRestTimeBasePenalty(firstTripInternal, prevTripInternal, nextTripInternal, driver, instance, true);
            }

            return (shiftLengthDiff, basePenaltyDiff);
        }
    }
}

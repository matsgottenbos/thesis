using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class CostDiffBasic {
        /* Internal differences */

        /** Get internal differences from unassigning the only trip in a shift; returns 1) shift length diff and 2) base penalty diff */
        public static (int, float) UnassignOnlyTripInternal(Trip tripToUnassign, Driver driver, Instance instance) {
            int oldShiftLength = CostHelper.ShiftLength(tripToUnassign, tripToUnassign, driver, instance);
            int shiftLengthDiff = -oldShiftLength;
            float shiftLengthBasePenaltyDiff = -CostHelper.GetShiftLengthPenaltyBase(oldShiftLength);

            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentNormalDiff().ShiftLength.Add(oldShiftLength, 0, driver);
            }

            return (shiftLengthDiff, shiftLengthBasePenaltyDiff);
        }

        /** Get internal differences from unassigning the first trip in a shift; returns 1) shift length diff and 2) base penalty diff */
        public static (int, float) UnassignFirstTripInternal(Trip tripToUnassign, Trip nextTripInternal, Trip lastTripInternal, Driver driver, Instance instance) {
            int oldShiftLength = CostHelper.ShiftLength(tripToUnassign, lastTripInternal, driver, instance);
            int newShiftLength = CostHelper.ShiftLength(nextTripInternal, lastTripInternal, driver, instance);
            int shiftLengthDiff = newShiftLength - oldShiftLength;
            float shiftLengthBasePenaltyDiff = CostHelper.GetShiftLengthPenaltyBase(newShiftLength) - CostHelper.GetShiftLengthPenaltyBase(oldShiftLength);
            float precedenceBasePenaltyDiff = -CostHelper.GetPrecedenceBasePenalty(tripToUnassign, nextTripInternal, instance);

            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentNormalDiff().ShiftLength.Add(oldShiftLength, newShiftLength, driver);
                SaDebugger.GetCurrentNormalDiff().Precedence.AddOld((tripToUnassign, nextTripInternal), driver);
            }

            return (shiftLengthDiff, shiftLengthBasePenaltyDiff + precedenceBasePenaltyDiff);
        }

        /** Get internal differences from unassigning the last trip in a shift; returns 1) shift length diff and 2) base penalty diff */
        public static (int, float) UnassignLastTripInternal(Trip tripToUnassign, Trip prevTripInternal, Trip firstTripInternal, Driver driver, Instance instance) {
            int oldShiftLength = CostHelper.ShiftLength(firstTripInternal, tripToUnassign, driver, instance);
            int newShiftLength = CostHelper.ShiftLength(firstTripInternal, prevTripInternal, driver, instance);
            int shiftLengthDiff = newShiftLength - oldShiftLength;
            float shiftLengthBasePenaltyDiff = CostHelper.GetShiftLengthPenaltyBase(newShiftLength) - CostHelper.GetShiftLengthPenaltyBase(oldShiftLength);
            float precedenceBasePenaltyDiff = -CostHelper.GetPrecedenceBasePenalty(prevTripInternal, tripToUnassign, instance);

            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentNormalDiff().ShiftLength.Add(oldShiftLength, newShiftLength, driver);
                SaDebugger.GetCurrentNormalDiff().Precedence.AddOld((prevTripInternal, tripToUnassign), driver);
            }

            return (shiftLengthDiff, shiftLengthBasePenaltyDiff + precedenceBasePenaltyDiff);
        }

        /** Get internal differences from unassigning a middle trip in a shift; returns 1) shift length diff and 2) base penalty diff */
        public static (int, float) UnassignMiddleTripInternal(Trip tripToUnassign, Trip prevTripInternal, Trip nextTripInternal, Driver driver, Instance instance) {
            float precedenceBasePenaltyDiff = CostHelper.GetPrecedenceBasePenalty(prevTripInternal, nextTripInternal, instance) - CostHelper.GetPrecedenceBasePenalty(prevTripInternal, tripToUnassign, instance) - CostHelper.GetPrecedenceBasePenalty(tripToUnassign, nextTripInternal, instance);

            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentNormalDiff().Precedence.AddOld((prevTripInternal, tripToUnassign), driver);
                SaDebugger.GetCurrentNormalDiff().Precedence.AddOld((tripToUnassign, nextTripInternal), driver);
                SaDebugger.GetCurrentNormalDiff().Precedence.AddNew((prevTripInternal, nextTripInternal), driver);
            }

            return (0, precedenceBasePenaltyDiff);
        }


        /* Merges and splits */

        public static (int, float) MergeShifts(Trip shift1FirstTrip, Trip shift1LastTrip, Trip shift2FirstTrip, Trip shift2LastTrip, Driver driver, Instance instance, bool debugIsSplit = false) {
            int oldShift1Length = CostHelper.ShiftLength(shift1FirstTrip, shift1LastTrip, driver, instance);
            int oldShift2Length = CostHelper.ShiftLength(shift2FirstTrip, shift2LastTrip, driver, instance);
            int newShiftLength = CostHelper.ShiftLength(shift1FirstTrip, shift2LastTrip, driver, instance);
            int shiftLengthDiff = newShiftLength - oldShift1Length - oldShift2Length;
            float shiftLengthBasePenaltyDiff = CostHelper.GetShiftLengthPenaltyBase(newShiftLength) - CostHelper.GetShiftLengthPenaltyBase(oldShift1Length) - CostHelper.GetShiftLengthPenaltyBase(oldShift2Length);
            float precedenceBasePenaltyDiff = CostHelper.GetPrecedenceBasePenalty(shift1LastTrip, shift2FirstTrip, instance);

            if (Config.DebugCheckAndLogOperations) {
                if (debugIsSplit) {
                    SaDebugger.GetCurrentNormalDiff().ShiftLength.AddOld(oldShift1Length, driver);
                    SaDebugger.GetCurrentNormalDiff().ShiftLength.AddOld(oldShift2Length, driver);
                    SaDebugger.GetCurrentNormalDiff().ShiftLength.AddNew(newShiftLength, driver);
                    SaDebugger.GetCurrentNormalDiff().Precedence.AddOld((shift1LastTrip, shift2FirstTrip), driver);
                } else {
                    SaDebugger.GetCurrentNormalDiff().ShiftLength.AddOld(newShiftLength, driver);
                    SaDebugger.GetCurrentNormalDiff().ShiftLength.AddNew(oldShift1Length, driver);
                    SaDebugger.GetCurrentNormalDiff().ShiftLength.AddNew(oldShift2Length, driver);
                    SaDebugger.GetCurrentNormalDiff().Precedence.AddNew((shift1LastTrip, shift2FirstTrip), driver);
                }
            }

            return (shiftLengthDiff, shiftLengthBasePenaltyDiff + precedenceBasePenaltyDiff);
        }

        public static (int, float) SplitShift(Trip shift1FirstTrip, Trip shift1LastTrip, Trip shift2FirstTrip, Trip shift2LastTrip, Driver driver, Instance instance) {
            (int shiftLengthDiff, float basePenaltyDiff) = MergeShifts(shift1FirstTrip, shift1LastTrip, shift2FirstTrip, shift2LastTrip, driver, instance, true);
            return (-shiftLengthDiff, -basePenaltyDiff);
        }
    }
}

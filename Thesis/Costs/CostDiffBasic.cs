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
            float shiftLengthBasePenaltyDiff = -CostHelper.GetShiftLengthBasePenalty(oldShiftLength, false);

            return (shiftLengthDiff, shiftLengthBasePenaltyDiff);
        }

        /** Get internal differences from unassigning the first trip in a shift; returns 1) shift length diff and 2) base penalty diff */
        public static (int, float) UnassignFirstTripInternal(Trip tripToUnassign, Trip nextTripInternal, Trip lastTripInternal, Driver driver, Instance instance) {
            int oldShiftLength = CostHelper.ShiftLength(tripToUnassign, lastTripInternal, driver, instance);
            int newShiftLength = CostHelper.ShiftLength(nextTripInternal, lastTripInternal, driver, instance);
            int shiftLengthDiff = newShiftLength - oldShiftLength;
            float shiftLengthBasePenaltyDiff = CostHelper.GetShiftLengthBasePenalty(newShiftLength, true) - CostHelper.GetShiftLengthBasePenalty(oldShiftLength, false);
            float precedenceBasePenaltyDiff = -CostHelper.GetPrecedenceBasePenalty(tripToUnassign, nextTripInternal, instance, false);

            return (shiftLengthDiff, shiftLengthBasePenaltyDiff + precedenceBasePenaltyDiff);
        }

        /** Get internal differences from unassigning the last trip in a shift; returns 1) shift length diff and 2) base penalty diff */
        public static (int, float) UnassignLastTripInternal(Trip tripToUnassign, Trip prevTripInternal, Trip firstTripInternal, Driver driver, Instance instance) {
            int oldShiftLength = CostHelper.ShiftLength(firstTripInternal, tripToUnassign, driver, instance);
            int newShiftLength = CostHelper.ShiftLength(firstTripInternal, prevTripInternal, driver, instance);
            int shiftLengthDiff = newShiftLength - oldShiftLength;
            float shiftLengthBasePenaltyDiff = CostHelper.GetShiftLengthBasePenalty(newShiftLength, true) - CostHelper.GetShiftLengthBasePenalty(oldShiftLength, false);
            float precedenceBasePenaltyDiff = -CostHelper.GetPrecedenceBasePenalty(prevTripInternal, tripToUnassign, instance, false);

            return (shiftLengthDiff, shiftLengthBasePenaltyDiff + precedenceBasePenaltyDiff);
        }

        /** Get internal differences from unassigning a middle trip in a shift; returns 1) shift length diff and 2) base penalty diff */
        public static (int, float) UnassignMiddleTripInternal(Trip tripToUnassign, Trip prevTripInternal, Trip nextTripInternal, Driver driver, Instance instance) {
            float precedenceBasePenaltyDiff = CostHelper.GetPrecedenceBasePenalty(prevTripInternal, nextTripInternal, instance, true) - CostHelper.GetPrecedenceBasePenalty(prevTripInternal, tripToUnassign, instance, false) - CostHelper.GetPrecedenceBasePenalty(tripToUnassign, nextTripInternal, instance, false);
            return (0, precedenceBasePenaltyDiff);
        }


        /* Merges and splits */

        public static (int, float) MergeShifts(Trip shift1FirstTrip, Trip shift1LastTrip, Trip shift2FirstTrip, Trip shift2LastTrip, Driver driver, Instance instance) {
            int oldShift1Length = CostHelper.ShiftLength(shift1FirstTrip, shift1LastTrip, driver, instance);
            int oldShift2Length = CostHelper.ShiftLength(shift2FirstTrip, shift2LastTrip, driver, instance);
            int newShiftLength = CostHelper.ShiftLength(shift1FirstTrip, shift2LastTrip, driver, instance);
            int shiftLengthDiff = newShiftLength - oldShift1Length - oldShift2Length;
            float shiftLengthBasePenaltyDiff = CostHelper.GetShiftLengthBasePenalty(newShiftLength, true) - CostHelper.GetShiftLengthBasePenalty(oldShift1Length, false) - CostHelper.GetShiftLengthBasePenalty(oldShift2Length, false);

            return (shiftLengthDiff, shiftLengthBasePenaltyDiff);
        }

        public static (int, float) SplitShift(Trip shift1FirstTrip, Trip shift1LastTrip, Trip shift2FirstTrip, Trip shift2LastTrip, Driver driver, Instance instance) {
            int oldShiftLength = CostHelper.ShiftLength(shift1FirstTrip, shift2LastTrip, driver, instance);
            int newShift1Length = CostHelper.ShiftLength(shift1FirstTrip, shift1LastTrip, driver, instance);
            int newShift2Length = CostHelper.ShiftLength(shift2FirstTrip, shift2LastTrip, driver, instance);
            int shiftLengthDiff = newShift1Length + newShift2Length - oldShiftLength;
            float shiftLengthBasePenaltyDiff = CostHelper.GetShiftLengthBasePenalty(newShift1Length, true) + CostHelper.GetShiftLengthBasePenalty(newShift2Length, true) - CostHelper.GetShiftLengthBasePenalty(oldShiftLength, false);
            return (shiftLengthDiff, shiftLengthBasePenaltyDiff);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class CostDiffBasic {
        /* Internal differences */

        /** Get internal differences from unassigning the only trip in a shift; returns 1) shift length diff, 2) shift cost diff, and 3) base penalty diff */
        public static (int, float, float) UnassignOnlyTripInternal(Trip tripToUnassign, Driver driver, Instance instance) {
            (int oldShiftLength, float oldShiftCost) = driver.ShiftLengthAndCost(tripToUnassign, tripToUnassign);
            int shiftLengthDiff = -oldShiftLength;
            float costWithoutPenaltyDiff = -oldShiftCost;
            float shiftLengthBasePenaltyDiff = -CostHelper.GetShiftLengthBasePenalty(oldShiftLength, false);

            return (shiftLengthDiff, costWithoutPenaltyDiff, shiftLengthBasePenaltyDiff);
        }

        /** Get internal differences from unassigning the first trip in a shift; returns 1) shift length diff, 2) shift cost diff, and 3) base penalty diff */
        public static (int, float, float) UnassignFirstTripInternal(Trip tripToUnassign, Trip nextTripInternal, Trip lastTripInternal, Driver driver, Instance instance) {
            (int oldShiftLength, float oldShiftCost) = driver.ShiftLengthAndCost(tripToUnassign, lastTripInternal);
            (int newShiftLength, float newShiftCost) = driver.ShiftLengthAndCost(nextTripInternal, lastTripInternal);
            int shiftLengthDiff = newShiftLength - oldShiftLength;
            float costWithoutPenaltyDiff = newShiftCost - oldShiftCost;
            float shiftLengthBasePenaltyDiff = CostHelper.GetShiftLengthBasePenalty(newShiftLength, true) - CostHelper.GetShiftLengthBasePenalty(oldShiftLength, false);
            float precedenceBasePenaltyDiff = -CostHelper.GetPrecedenceBasePenalty(tripToUnassign, nextTripInternal, instance, false);

            return (shiftLengthDiff, costWithoutPenaltyDiff, shiftLengthBasePenaltyDiff + precedenceBasePenaltyDiff);
        }

        /** Get internal differences from unassigning the last trip in a shift; returns 1) shift length diff, 2) shift cost diff, and 3) base penalty diff */
        public static (int, float, float) UnassignLastTripInternal(Trip tripToUnassign, Trip prevTripInternal, Trip firstTripInternal, Driver driver, Instance instance) {
            (int oldShiftLength, float oldShiftCost) = driver.ShiftLengthAndCost(firstTripInternal, tripToUnassign);
            (int newShiftLength, float newShiftCost) = driver.ShiftLengthAndCost(firstTripInternal, prevTripInternal);
            int shiftLengthDiff = newShiftLength - oldShiftLength;
            float costWithoutPenaltyDiff = newShiftCost - oldShiftCost;
            float shiftLengthBasePenaltyDiff = CostHelper.GetShiftLengthBasePenalty(newShiftLength, true) - CostHelper.GetShiftLengthBasePenalty(oldShiftLength, false);
            float precedenceBasePenaltyDiff = -CostHelper.GetPrecedenceBasePenalty(prevTripInternal, tripToUnassign, instance, false);

            return (shiftLengthDiff, costWithoutPenaltyDiff, shiftLengthBasePenaltyDiff + precedenceBasePenaltyDiff);
        }

        /** Get internal differences from unassigning a middle trip in a shift; returns 1) shift length diff, 2) shift cost diff, and 3) base penalty diff */
        public static (int, float, float) UnassignMiddleTripInternal(Trip tripToUnassign, Trip prevTripInternal, Trip nextTripInternal, Driver driver, Instance instance) {
            float precedenceBasePenaltyDiff = CostHelper.GetPrecedenceBasePenalty(prevTripInternal, nextTripInternal, instance, true) - CostHelper.GetPrecedenceBasePenalty(prevTripInternal, tripToUnassign, instance, false) - CostHelper.GetPrecedenceBasePenalty(tripToUnassign, nextTripInternal, instance, false);
            return (0, 0, precedenceBasePenaltyDiff);
        }


        /* Merges and splits */
        /** Get internal differences from merging two shifts into one; returns 1) shift length diff, 2) shift cost diff, and 3) base penalty diff */

        public static (int, float, float) MergeShifts(Trip shift1FirstTrip, Trip shift1LastTrip, Trip shift2FirstTrip, Trip shift2LastTrip, Driver driver, Instance instance) {
            (int oldShift1Length, float oldShift1Cost) = driver.ShiftLengthAndCost(shift1FirstTrip, shift1LastTrip);
            (int oldShift2Length, float oldShift2Cost) = driver.ShiftLengthAndCost(shift2FirstTrip, shift2LastTrip);
            (int newShiftLength, float newShiftCost) = driver.ShiftLengthAndCost(shift1FirstTrip, shift2LastTrip);
            int shiftLengthDiff = newShiftLength - oldShift1Length - oldShift2Length;
            float costWithoutPenaltyDiff = newShiftCost - oldShift1Cost - oldShift2Cost;
            float shiftLengthBasePenaltyDiff = CostHelper.GetShiftLengthBasePenalty(newShiftLength, true) - CostHelper.GetShiftLengthBasePenalty(oldShift1Length, false) - CostHelper.GetShiftLengthBasePenalty(oldShift2Length, false);

            return (shiftLengthDiff, costWithoutPenaltyDiff, shiftLengthBasePenaltyDiff);
        }

        /** Get internal differences from splitting one trip into two; returns 1) shift length diff, 2) shift cost diff, and 3) base penalty diff */

        public static (int, float, float) SplitShift(Trip shift1FirstTrip, Trip shift1LastTrip, Trip shift2FirstTrip, Trip shift2LastTrip, Driver driver, Instance instance) {
            (int oldShiftLength, float oldShiftCost) = driver.ShiftLengthAndCost(shift1FirstTrip, shift2LastTrip);
            (int newShift1Length, float newShift1Cost) = driver.ShiftLengthAndCost(shift1FirstTrip, shift1LastTrip);
            (int newShift2Length, float newShift2Cost) = driver.ShiftLengthAndCost(shift2FirstTrip, shift2LastTrip);
            int shiftLengthDiff = newShift1Length + newShift2Length - oldShiftLength;
            float costWithoutPenaltyDiff = newShift1Cost + newShift2Cost - oldShiftCost;
            float shiftLengthBasePenaltyDiff = CostHelper.GetShiftLengthBasePenalty(newShift1Length, true) + CostHelper.GetShiftLengthBasePenalty(newShift2Length, true) - CostHelper.GetShiftLengthBasePenalty(oldShiftLength, false);
            return (shiftLengthDiff, costWithoutPenaltyDiff, shiftLengthBasePenaltyDiff);
        }
    }
}

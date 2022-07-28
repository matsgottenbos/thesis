/*
 * Used to store calculated penalty amounts for a driver's activity path, or a range of it
*/

using System;

namespace DriverPlannerShared {
    public class SaDriverPenaltyInfo {
        public int OverlapViolationCount, ShiftLengthViolationCount, ShiftLengthViolationAmount, RestTimeViolationCount, RestTimeViolationAmount, ShiftCountViolationAmount, InvalidHotelCount, AvailabilityViolationCount, QualificationViolationCount;

        /* Adding violations */

        public void AddOverlapViolation() {
            OverlapViolationCount++;
        }

        public void AddPossibleShiftLengthViolation(int fullShiftLength, MainShiftInfo mainShiftInfo) {
            int shiftLengthViolationAmount = mainShiftInfo.MainShiftLengthViolationAmount + Math.Max(0, fullShiftLength - mainShiftInfo.MaxFullShiftLength);
            if (shiftLengthViolationAmount > 0) {
                ShiftLengthViolationCount++;
                ShiftLengthViolationAmount += shiftLengthViolationAmount;
            }
        }

        public void AddPossibleRestTimeViolation(int restTime, int minRestTime) {
            int shiftLengthViolation = Math.Max(0, minRestTime - restTime);
            if (shiftLengthViolation > 0) {
                RestTimeViolationCount++;
                RestTimeViolationAmount += shiftLengthViolation;
            }
        }

        public void AddPossibleShiftCountViolation(int shiftCount) {
            int shiftCountViolation = Math.Max(0, shiftCount - RulesConfig.DriverMaxShiftCount);
            if (shiftCountViolation > 0) {
                ShiftCountViolationAmount += shiftCountViolation;
            }
        }

        public void AddInvalidHotel() {
            InvalidHotelCount++;
        }

        public void AddPotentialAvailabilityViolation(int fullShiftStartTime, int fullShiftEndTime, Driver driver) {
            if (!driver.IsAvailableDuringRange(fullShiftStartTime, fullShiftEndTime)) {
                AvailabilityViolationCount++;
            }
        }

        public void AddPotentialQualificationViolation(Activity activity, Driver driver) {
            if (!driver.IsQualifiedForActivity(activity)) {
                QualificationViolationCount++;
            }
        }


        /* Calculating penalty */

        public double GetPenalty() {
            double penalty = 0;
            penalty += OverlapViolationCount * AlgorithmConfig.OverlapViolationPenalty;
            penalty += ShiftLengthViolationCount * AlgorithmConfig.ShiftLengthViolationPenalty + ShiftLengthViolationAmount * AlgorithmConfig.ShiftLengthViolationPenaltyPerMin;
            penalty += RestTimeViolationCount * AlgorithmConfig.RestTimeViolationPenalty + RestTimeViolationAmount * AlgorithmConfig.RestTimeViolationPenaltyPerMin;
            penalty += ShiftCountViolationAmount * AlgorithmConfig.InternalShiftCountViolationPenaltyPerShift;
            penalty += InvalidHotelCount * AlgorithmConfig.InvalidHotelPenalty;
            penalty += AvailabilityViolationCount * AlgorithmConfig.AvailabilityViolationPenalty;
            penalty += QualificationViolationCount * AlgorithmConfig.QualificationViolationPenalty;
            return penalty;
        }


        /* Operators */

        public static SaDriverPenaltyInfo operator -(SaDriverPenaltyInfo a) {
            return new SaDriverPenaltyInfo() {
                OverlapViolationCount = -a.OverlapViolationCount,
                ShiftLengthViolationCount = -a.ShiftLengthViolationCount,
                ShiftLengthViolationAmount = -a.ShiftLengthViolationAmount,
                RestTimeViolationCount = -a.RestTimeViolationCount,
                RestTimeViolationAmount = -a.RestTimeViolationAmount,
                ShiftCountViolationAmount = -a.ShiftCountViolationAmount,
                InvalidHotelCount = -a.InvalidHotelCount,
                AvailabilityViolationCount = -a.AvailabilityViolationCount,
                QualificationViolationCount = -a.QualificationViolationCount,
            };
        }
        public static SaDriverPenaltyInfo operator +(SaDriverPenaltyInfo a, SaDriverPenaltyInfo b) {
            return new SaDriverPenaltyInfo() {
                OverlapViolationCount = a.OverlapViolationCount + b.OverlapViolationCount,
                ShiftLengthViolationCount = a.ShiftLengthViolationCount + b.ShiftLengthViolationCount,
                ShiftLengthViolationAmount = a.ShiftLengthViolationAmount + b.ShiftLengthViolationAmount,
                RestTimeViolationCount = a.RestTimeViolationCount + b.RestTimeViolationCount,
                RestTimeViolationAmount = a.RestTimeViolationAmount + b.RestTimeViolationAmount,
                ShiftCountViolationAmount = a.ShiftCountViolationAmount + b.ShiftCountViolationAmount,
                InvalidHotelCount = a.InvalidHotelCount + b.InvalidHotelCount,
                AvailabilityViolationCount = a.AvailabilityViolationCount + b.AvailabilityViolationCount,
                QualificationViolationCount = a.QualificationViolationCount + b.QualificationViolationCount,
            };
        }
        public static SaDriverPenaltyInfo operator -(SaDriverPenaltyInfo a, SaDriverPenaltyInfo b) => a + -b;

        public static bool AreEqual(SaDriverPenaltyInfo a, SaDriverPenaltyInfo b) {
            return (
                a.OverlapViolationCount == b.OverlapViolationCount &&
                a.ShiftLengthViolationCount == b.ShiftLengthViolationCount &&
                a.ShiftLengthViolationAmount == b.ShiftLengthViolationAmount &&
                a.RestTimeViolationCount == b.RestTimeViolationCount &&
                a.RestTimeViolationAmount == b.RestTimeViolationAmount &&
                a.ShiftCountViolationAmount == b.ShiftCountViolationAmount &&
                a.InvalidHotelCount == b.InvalidHotelCount &&
                a.AvailabilityViolationCount == b.AvailabilityViolationCount &&
                a.QualificationViolationCount == b.QualificationViolationCount
            );
        }


        /* Debugging */

        public void DebugLog(bool isDiff, bool shouldLogZeros = true) {
            ToStringHelper.LogDebugValue(OverlapViolationCount, "Overlap violation count", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(ShiftLengthViolationCount, "Shift length violation count", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(ShiftLengthViolationAmount, "Shift length violation", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(RestTimeViolationCount, "Rest time violation count", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(RestTimeViolationAmount, "Rest time violation", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(ShiftCountViolationAmount, "Shift count violation amount", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(InvalidHotelCount, "Invalid hotel count", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(AvailabilityViolationCount, "Availability violation count", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(QualificationViolationCount, "Qualification violation count", isDiff, shouldLogZeros);
        }
    }
}

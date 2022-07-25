using System;

namespace Thesis {
    class SaDriverPenaltyInfo {
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

        public void AddPotentialAvailabilityViolation(Activity activity, Driver driver) {
            if (!driver.IsAvailableForActivity(activity)) {
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
            penalty += OverlapViolationCount * SaConfig.OverlapViolationPenalty;
            penalty += ShiftLengthViolationCount * SaConfig.ShiftLengthViolationPenalty + ShiftLengthViolationAmount * SaConfig.ShiftLengthViolationPenaltyPerMin;
            penalty += RestTimeViolationCount * SaConfig.RestTimeViolationPenalty + RestTimeViolationAmount * SaConfig.RestTimeViolationPenaltyPerMin;
            penalty += ShiftCountViolationAmount * SaConfig.InternalShiftCountViolationPenaltyPerShift;
            penalty += InvalidHotelCount * SaConfig.InvalidHotelPenalty;
            penalty += AvailabilityViolationCount * SaConfig.AvailabilityViolationPenalty;
            penalty += QualificationViolationCount * SaConfig.QualificationViolationPenalty;
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
            ParseHelper.LogDebugValue(OverlapViolationCount, "Overlap violation count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ShiftLengthViolationCount, "Shift length violation count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ShiftLengthViolationAmount, "Shift length violation", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(RestTimeViolationCount, "Rest time violation count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(RestTimeViolationAmount, "Rest time violation", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ShiftCountViolationAmount, "Shift count violation amount", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(InvalidHotelCount, "Invalid hotel count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(AvailabilityViolationCount, "Availability violation count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(QualificationViolationCount, "Qualification violation count", isDiff, shouldLogZeros);
        }
    }
}

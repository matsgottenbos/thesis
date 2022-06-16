using System;

namespace Thesis {
    class DriverPenaltyInfo {
        public int PrecedenceViolationCount, ShiftLengthViolationCount, ShiftLengthViolationAmount, RestTimeViolationCount, RestTimeViolationAmount, ShiftCountViolationAmount, InvalidHotelCount;

        /* Adding violations */

        public void AddPrecedenceViolation() {
            PrecedenceViolationCount++;
        }

        public void AddPossibleShiftLengthViolation(int shiftLengthWithoutTravel, int shiftLengthWithTravel, int maxShiftLengthWithoutTravel, int maxShiftLengthWithTravel) {
            int shiftLengthViolationAmount = Math.Max(0, shiftLengthWithoutTravel - maxShiftLengthWithoutTravel) + Math.Max(0, shiftLengthWithTravel - maxShiftLengthWithTravel);
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
            int shiftCountViolation = Math.Max(0, shiftCount - Config.DriverMaxShiftCount);
            if (shiftCountViolation > 0) {
                ShiftCountViolationAmount += shiftCountViolation;
            }
        }

        public void AddInvalidHotel() {
            InvalidHotelCount++;
        }


        /* Calculating penalty */

        public double GetPenalty() {
            double penalty = 0;
            penalty += PrecedenceViolationCount * Config.PrecendenceViolationPenalty;
            penalty += ShiftLengthViolationCount * Config.ShiftLengthViolationPenalty + ShiftLengthViolationAmount * Config.ShiftLengthViolationPenaltyPerMin;
            penalty += RestTimeViolationCount * Config.RestTimeViolationPenalty + RestTimeViolationAmount * Config.RestTimeViolationPenaltyPerMin;
            penalty += ShiftCountViolationAmount * Config.InternalShiftCountViolationPenaltyPerShift;
            penalty += InvalidHotelCount * Config.InvalidHotelPenalty;
            return penalty;
        }


        /* Operators */

        public static DriverPenaltyInfo operator -(DriverPenaltyInfo a) {
            return new DriverPenaltyInfo() {
                PrecedenceViolationCount = -a.PrecedenceViolationCount,
                ShiftLengthViolationCount = -a.ShiftLengthViolationCount,
                ShiftLengthViolationAmount = -a.ShiftLengthViolationAmount,
                RestTimeViolationCount = -a.RestTimeViolationCount,
                RestTimeViolationAmount = -a.RestTimeViolationAmount,
                ShiftCountViolationAmount = -a.ShiftCountViolationAmount,
                InvalidHotelCount = -a.InvalidHotelCount,
            };
        }
        public static DriverPenaltyInfo operator +(DriverPenaltyInfo a, DriverPenaltyInfo b) {
            return new DriverPenaltyInfo() {
                PrecedenceViolationCount = a.PrecedenceViolationCount + b.PrecedenceViolationCount,
                ShiftLengthViolationCount = a.ShiftLengthViolationCount + b.ShiftLengthViolationCount,
                ShiftLengthViolationAmount = a.ShiftLengthViolationAmount + b.ShiftLengthViolationAmount,
                RestTimeViolationCount = a.RestTimeViolationCount + b.RestTimeViolationCount,
                RestTimeViolationAmount = a.RestTimeViolationAmount + b.RestTimeViolationAmount,
                ShiftCountViolationAmount = a.ShiftCountViolationAmount + b.ShiftCountViolationAmount,
                InvalidHotelCount = a.InvalidHotelCount + b.InvalidHotelCount,
            };
        }
        public static DriverPenaltyInfo operator -(DriverPenaltyInfo a, DriverPenaltyInfo b) => a + -b;

        public static bool AreEqual(DriverPenaltyInfo a, DriverPenaltyInfo b) {
            return (
                a.PrecedenceViolationCount == b.PrecedenceViolationCount &&
                a.ShiftLengthViolationCount == b.ShiftLengthViolationCount &&
                a.ShiftLengthViolationAmount == b.ShiftLengthViolationAmount &&
                a.RestTimeViolationCount == b.RestTimeViolationCount &&
                a.RestTimeViolationAmount == b.RestTimeViolationAmount &&
                a.ShiftCountViolationAmount == b.ShiftCountViolationAmount &&
                a.InvalidHotelCount == b.InvalidHotelCount
            );
        }


        /* Debugging */

        public void DebugLog(bool isDiff, bool shouldLogZeros = true) {
            ParseHelper.LogDebugValue(PrecedenceViolationCount, "Precedence violation count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ShiftLengthViolationCount, "Shift length violation count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ShiftLengthViolationAmount, "Shift length violation", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(RestTimeViolationCount, "Rest time violation count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(RestTimeViolationAmount, "Rest time violation", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ShiftCountViolationAmount, "Shift count violation amount", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(InvalidHotelCount, "Invalid hotel count", isDiff, shouldLogZeros);
        }
    }
}

namespace Thesis {
    class PenaltyInfo {
        public int PrecedenceViolationCount, ShiftLengthViolationCount, ShiftLengthViolation, RestTimeViolationCount, RestTimeViolation, ContractTimeViolationCount, ContractTimeViolation, ShiftCountViolationAmount, InvalidHotelCount;

        public static PenaltyInfo operator -(PenaltyInfo a) {
            return new PenaltyInfo() {
                PrecedenceViolationCount = -a.PrecedenceViolationCount,
                ShiftLengthViolationCount = -a.ShiftLengthViolationCount,
                ShiftLengthViolation = -a.ShiftLengthViolation,
                RestTimeViolationCount = -a.RestTimeViolationCount,
                RestTimeViolation = -a.RestTimeViolation,
                ContractTimeViolationCount = -a.ContractTimeViolationCount,
                ContractTimeViolation = -a.ContractTimeViolation,
                ShiftCountViolationAmount = -a.ShiftCountViolationAmount,
                InvalidHotelCount = -a.InvalidHotelCount,
            };
        }
        public static PenaltyInfo operator +(PenaltyInfo a, PenaltyInfo b) {
            return new PenaltyInfo() {
                PrecedenceViolationCount = a.PrecedenceViolationCount + b.PrecedenceViolationCount,
                ShiftLengthViolationCount = a.ShiftLengthViolationCount + b.ShiftLengthViolationCount,
                ShiftLengthViolation = a.ShiftLengthViolation + b.ShiftLengthViolation,
                RestTimeViolationCount = a.RestTimeViolationCount + b.RestTimeViolationCount,
                RestTimeViolation = a.RestTimeViolation + b.RestTimeViolation,
                ContractTimeViolationCount = a.ContractTimeViolationCount + b.ContractTimeViolationCount,
                ContractTimeViolation = a.ContractTimeViolation + b.ContractTimeViolation,
                ShiftCountViolationAmount = a.ShiftCountViolationAmount + b.ShiftCountViolationAmount,
                InvalidHotelCount = a.InvalidHotelCount + b.InvalidHotelCount,
            };
        }
        public static PenaltyInfo operator -(PenaltyInfo a, PenaltyInfo b) => a + -b;

        public static bool AreEqual(PenaltyInfo a, PenaltyInfo b) {
            return (
                a.PrecedenceViolationCount == b.PrecedenceViolationCount &&
                a.ShiftLengthViolationCount == b.ShiftLengthViolationCount &&
                a.ShiftLengthViolation == b.ShiftLengthViolation &&
                a.RestTimeViolationCount == b.RestTimeViolationCount &&
                a.RestTimeViolation == b.RestTimeViolation &&
                a.ContractTimeViolationCount == b.ContractTimeViolationCount &&
                a.ContractTimeViolation == b.ContractTimeViolation &&
                a.ShiftCountViolationAmount == b.ShiftCountViolationAmount &&
                a.InvalidHotelCount == b.InvalidHotelCount
            );
        }

        public void DebugLog(bool isDiff, bool shouldLogZeros = true) {
            ParseHelper.LogDebugValue(PrecedenceViolationCount, "Precedence violation count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ShiftLengthViolationCount, "Shift length violation count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ShiftLengthViolation, "Shift length violation", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(RestTimeViolationCount, "Rest time violation count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(RestTimeViolation, "Rest time violation", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ContractTimeViolationCount, "Contract time violation count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ContractTimeViolation, "Contract time violation", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ShiftCountViolationAmount, "Shift count violation amount", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(InvalidHotelCount, "Invalid hotel count", isDiff, shouldLogZeros);
        }
    }
}

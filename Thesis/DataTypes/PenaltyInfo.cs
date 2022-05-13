using System;

namespace Thesis {
    class PenaltyInfo {
        public int PrecedenceViolationCount, ShiftLengthViolationCount, ShiftLengthViolation, RestTimeViolationCount, RestTimeViolation, ContractTimeViolationCount, ContractTimeViolation, ShiftCountViolationAmount, InvalidHotelCount;

        /* Adding violations */

        public void AddPrecedenceViolation() {
            PrecedenceViolationCount++;
        }

        public void AddPossibleShiftLengthViolation(int shiftLengthWithoutTravel, int shiftLengthWithTravel) {
            int shiftLengthViolationAmount = Math.Max(0, shiftLengthWithoutTravel - Config.MaxShiftLengthWithoutTravel) + Math.Max(0, shiftLengthWithTravel - Config.MaxShiftLengthWithTravel);
            if (shiftLengthViolationAmount > 0) {
                ShiftLengthViolationCount++;
                ShiftLengthViolation += shiftLengthViolationAmount;
            }
        }

        public void AddPossibleRestTimeViolation(int restTime) {
            int shiftLengthViolation = Math.Max(0, Config.MinRestTime - restTime);
            if (shiftLengthViolation > 0) {
                RestTimeViolationCount++;
                RestTimeViolation += shiftLengthViolation;
            }
        }

        public void AddPossibleContractTimeViolation(int workedTime, Driver driver) {
            int contractTimeViolation = driver.GetContractTimeViolation(workedTime);
            if (contractTimeViolation > 0) {
                ContractTimeViolationCount++;
                ContractTimeViolation += contractTimeViolation;
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
            penalty += ShiftLengthViolationCount * Config.ShiftLengthViolationPenalty + ShiftLengthViolation * Config.ShiftLengthViolationPenaltyPerMin;
            penalty += RestTimeViolationCount * Config.RestTimeViolationPenalty + RestTimeViolation * Config.RestTimeViolationPenaltyPerMin;
            penalty += ContractTimeViolationCount * Config.ContractTimeViolationPenalty + ContractTimeViolation * Config.ContractTimeViolationPenaltyPerMin;
            penalty += ShiftCountViolationAmount * Config.ShiftCountViolationPenaltyPerShift;
            penalty += InvalidHotelCount * Config.InvalidHotelPenalty;
            return penalty;
        }


        /* Operators */

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


        /* Debugging */

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

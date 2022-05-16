using System;

namespace Thesis {
    class PenaltyInfo {
        public int PrecedenceViolationCount, ShiftLengthViolationCount, ShiftLengthViolationAmount, RestTimeViolationCount, RestTimeViolationAmount, ContractTimeViolationCount, ContractTimeViolationAmount, ShiftCountViolationAmount, InvalidHotelCount;

        /* Adding violations */

        public void AddPrecedenceViolation() {
            PrecedenceViolationCount++;
        }

        public void AddPossibleShiftLengthViolation(int shiftLengthWithoutTravel, int shiftLengthWithTravel) {
            int shiftLengthViolationAmount = Math.Max(0, shiftLengthWithoutTravel - Config.MaxShiftLengthWithoutTravel) + Math.Max(0, shiftLengthWithTravel - Config.MaxShiftLengthWithTravel);
            if (shiftLengthViolationAmount > 0) {
                ShiftLengthViolationCount++;
                ShiftLengthViolationAmount += shiftLengthViolationAmount;
            }
        }

        public void AddPossibleRestTimeViolation(int restTime) {
            int shiftLengthViolation = Math.Max(0, Config.MinRestTime - restTime);
            if (shiftLengthViolation > 0) {
                RestTimeViolationCount++;
                RestTimeViolationAmount += shiftLengthViolation;
            }
        }

        public void AddPossibleContractTimeViolation(int workedTime, Driver driver) {
            int contractTimeViolation = driver.GetContractTimeViolation(workedTime);
            if (contractTimeViolation > 0) {
                ContractTimeViolationCount++;
                ContractTimeViolationAmount += contractTimeViolation;
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
            penalty += ContractTimeViolationCount * Config.ContractTimeViolationPenalty + ContractTimeViolationAmount * Config.ContractTimeViolationPenaltyPerMin;
            penalty += ShiftCountViolationAmount * Config.ShiftCountViolationPenaltyPerShift;
            penalty += InvalidHotelCount * Config.InvalidHotelPenalty;
            return penalty;
        }


        /* Operators */

        public static PenaltyInfo operator -(PenaltyInfo a) {
            return new PenaltyInfo() {
                PrecedenceViolationCount = -a.PrecedenceViolationCount,
                ShiftLengthViolationCount = -a.ShiftLengthViolationCount,
                ShiftLengthViolationAmount = -a.ShiftLengthViolationAmount,
                RestTimeViolationCount = -a.RestTimeViolationCount,
                RestTimeViolationAmount = -a.RestTimeViolationAmount,
                ContractTimeViolationCount = -a.ContractTimeViolationCount,
                ContractTimeViolationAmount = -a.ContractTimeViolationAmount,
                ShiftCountViolationAmount = -a.ShiftCountViolationAmount,
                InvalidHotelCount = -a.InvalidHotelCount,
            };
        }
        public static PenaltyInfo operator +(PenaltyInfo a, PenaltyInfo b) {
            return new PenaltyInfo() {
                PrecedenceViolationCount = a.PrecedenceViolationCount + b.PrecedenceViolationCount,
                ShiftLengthViolationCount = a.ShiftLengthViolationCount + b.ShiftLengthViolationCount,
                ShiftLengthViolationAmount = a.ShiftLengthViolationAmount + b.ShiftLengthViolationAmount,
                RestTimeViolationCount = a.RestTimeViolationCount + b.RestTimeViolationCount,
                RestTimeViolationAmount = a.RestTimeViolationAmount + b.RestTimeViolationAmount,
                ContractTimeViolationCount = a.ContractTimeViolationCount + b.ContractTimeViolationCount,
                ContractTimeViolationAmount = a.ContractTimeViolationAmount + b.ContractTimeViolationAmount,
                ShiftCountViolationAmount = a.ShiftCountViolationAmount + b.ShiftCountViolationAmount,
                InvalidHotelCount = a.InvalidHotelCount + b.InvalidHotelCount,
            };
        }
        public static PenaltyInfo operator -(PenaltyInfo a, PenaltyInfo b) => a + -b;

        public static bool AreEqual(PenaltyInfo a, PenaltyInfo b) {
            return (
                a.PrecedenceViolationCount == b.PrecedenceViolationCount &&
                a.ShiftLengthViolationCount == b.ShiftLengthViolationCount &&
                a.ShiftLengthViolationAmount == b.ShiftLengthViolationAmount &&
                a.RestTimeViolationCount == b.RestTimeViolationCount &&
                a.RestTimeViolationAmount == b.RestTimeViolationAmount &&
                a.ContractTimeViolationCount == b.ContractTimeViolationCount &&
                a.ContractTimeViolationAmount == b.ContractTimeViolationAmount &&
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
            ParseHelper.LogDebugValue(ContractTimeViolationCount, "Contract time violation count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ContractTimeViolationAmount, "Contract time violation", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ShiftCountViolationAmount, "Shift count violation amount", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(InvalidHotelCount, "Invalid hotel count", isDiff, shouldLogZeros);
        }
    }
}

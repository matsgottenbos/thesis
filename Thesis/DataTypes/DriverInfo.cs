using System;

namespace Thesis {
    class DriverInfo {
        public double Cost, CostWithoutPenalty, Penalty, DriverSatisfaction, Satisfaction;
        public int WorkedTime, ShiftCount, HotelCount, NightShiftCount, WeekendShiftCount, TravelTime;
        public PenaltyInfo PenaltyInfo;

        public DriverInfo() {
            PenaltyInfo = new PenaltyInfo();
        }

        public static DriverInfo operator -(DriverInfo a) {
            return new DriverInfo() {
                Cost =- a.Cost,
                CostWithoutPenalty = -a.CostWithoutPenalty,
                Penalty = -a.Penalty,
                DriverSatisfaction = -a.DriverSatisfaction,
                Satisfaction = -a.Satisfaction,
                WorkedTime = -a.WorkedTime,
                ShiftCount = -a.ShiftCount,
                HotelCount = -a.HotelCount,
                NightShiftCount = -a.NightShiftCount,
                WeekendShiftCount = -a.WeekendShiftCount,
                TravelTime = -a.TravelTime,
                PenaltyInfo = -a.PenaltyInfo,
            };
        }
        public static DriverInfo operator +(DriverInfo a, DriverInfo b) {
            return new DriverInfo() {
                Cost = a.Cost + b.Cost,
                CostWithoutPenalty = a.CostWithoutPenalty + b.CostWithoutPenalty,
                Penalty = a.Penalty + b.Penalty,
                DriverSatisfaction = a.DriverSatisfaction + b.DriverSatisfaction,
                Satisfaction = a.Satisfaction + b.Satisfaction,
                WorkedTime = a.WorkedTime + b.WorkedTime,
                ShiftCount = a.ShiftCount + b.ShiftCount,
                HotelCount = a.HotelCount + b.HotelCount,
                NightShiftCount = a.NightShiftCount + b.NightShiftCount,
                WeekendShiftCount = a.WeekendShiftCount + b.WeekendShiftCount,
                TravelTime = a.TravelTime + b.TravelTime,
                PenaltyInfo = a.PenaltyInfo + b.PenaltyInfo,
            };
        }
        public static DriverInfo operator -(DriverInfo a, DriverInfo b) => a + -b;

        public static bool AreEqual(DriverInfo a, DriverInfo b) {
            return (
                IsDoubleEqual(a.Cost, b.Cost) &&
                IsDoubleEqual(a.CostWithoutPenalty, b.CostWithoutPenalty) &&
                IsDoubleEqual(a.Penalty, b.Penalty) &&
                IsDoubleEqual(a.DriverSatisfaction, b.DriverSatisfaction) &&
                IsDoubleEqual(a.Satisfaction, b.Satisfaction) &&
                a.WorkedTime == b.WorkedTime &&
                a.ShiftCount == b.ShiftCount &&
                a.HotelCount == b.HotelCount &&
                a.NightShiftCount == b.NightShiftCount &&
                a.WeekendShiftCount == b.WeekendShiftCount &&
                a.TravelTime == b.TravelTime &&
                PenaltyInfo.AreEqual(a.PenaltyInfo, b.PenaltyInfo)
            );
        }

        static bool IsDoubleEqual(double? a, double? b) {
            return Math.Abs(a.Value - b.Value) < 0.01;
        }

        public void DebugLog(bool isDiff, bool shouldLogZeros = true) {
            ParseHelper.LogDebugValue(Cost, "Cost", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(CostWithoutPenalty, "Cost without penalty", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(Penalty, "Penalty", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(DriverSatisfaction, "Driver satisfaction", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(Satisfaction, "Satisfaction", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(WorkedTime, "Worked time", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ShiftCount, "Shift count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(HotelCount, "Hotel count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(NightShiftCount, "Night shift count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(WeekendShiftCount, "Weekend shift count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(TravelTime, "Travel time", isDiff, shouldLogZeros);
            PenaltyInfo.DebugLog(isDiff, shouldLogZeros);
        }
    }
}

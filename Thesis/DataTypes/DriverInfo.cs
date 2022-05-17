using System;
using System.Linq;

namespace Thesis {
    class DriverInfo {
        public readonly Instance Instance;
        public double Cost, CostWithoutPenalty, Penalty, DriverSatisfaction, Satisfaction;
        public int WorkedTime, ShiftCount, HotelCount, NightShiftCount, WeekendShiftCount, TravelTime, SingleFreeDays, DoubleFreeDays;
        public int[] SharedRouteCounts;
        public PenaltyInfo PenaltyInfo;

        public DriverInfo(Instance instance) {
            Instance = instance;
            PenaltyInfo = new PenaltyInfo();
            SharedRouteCounts = new int[instance.UniqueSharedRouteCount];
        }

        public static DriverInfo operator -(DriverInfo a) {
            return new DriverInfo(a.Instance) {
                Cost = -a.Cost,
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
                SingleFreeDays = -a.SingleFreeDays,
                DoubleFreeDays = -a.DoubleFreeDays,
                SharedRouteCounts = InvertArray(a.SharedRouteCounts),
                PenaltyInfo = -a.PenaltyInfo,
            };
        }
        public static DriverInfo operator +(DriverInfo a, DriverInfo b) {
            return new DriverInfo(a.Instance) {
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
                SingleFreeDays = a.SingleFreeDays + b.SingleFreeDays,
                DoubleFreeDays = a.DoubleFreeDays + b.DoubleFreeDays,
                SharedRouteCounts = AddArrays(a.SharedRouteCounts, b.SharedRouteCounts),
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
                a.SingleFreeDays == b.SingleFreeDays &&
                a.DoubleFreeDays == b.DoubleFreeDays &&
                AreArraysEqual(a.SharedRouteCounts, b.SharedRouteCounts) &&
                PenaltyInfo.AreEqual(a.PenaltyInfo, b.PenaltyInfo)
            );
        }

        static bool IsDoubleEqual(double? a, double? b) {
            return Math.Abs(a.Value - b.Value) < 0.01;
        }

        static int[] InvertArray(int[] array) {
            int[] invertedArray = new int[array.Length];
            for (int i = 0; i < array.Length; i++) {
                invertedArray[i] = -array[i];
            }
            return invertedArray;
        }

        static int[] AddArrays(int[] array1, int[] array2) {
            int[] addedArray = new int[array1.Length];
            for (int i = 0; i < array1.Length; i++) {
                addedArray[i] = array1[i] + array2[i];
            }
            return addedArray;
        }

        static bool AreArraysEqual(int[] array1, int[] array2) {
            for (int i = 0; i < array1.Length; i++) {
                if (array1[i] != array2[i]) return false;
            }
            return true;
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

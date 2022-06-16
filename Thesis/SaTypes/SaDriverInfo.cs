using System;
using System.Linq;

namespace Thesis {
    class SaDriverInfo {
        public readonly Instance Instance;
        public SaStats Stats;
        public SaDriverPenaltyInfo PenaltyInfo;
        public int WorkedTime, ShiftCount, HotelCount, NightShiftCountByCompanyRules, WeekendShiftCountByCompanyRules, TravelTime, SingleFreeDays, DoubleFreeDays;
        public int[] SharedRouteCounts;

        public SaDriverInfo(Instance instance) {
            Instance = instance;
            Stats = new SaStats();
            PenaltyInfo = new SaDriverPenaltyInfo();
            SharedRouteCounts = new int[instance.UniqueSharedRouteCount];
        }

        public static SaDriverInfo operator -(SaDriverInfo a) {
            return new SaDriverInfo(a.Instance) {
                Stats = -a.Stats,
                PenaltyInfo = -a.PenaltyInfo,
                WorkedTime = -a.WorkedTime,
                ShiftCount = -a.ShiftCount,
                HotelCount = -a.HotelCount,
                NightShiftCountByCompanyRules = -a.NightShiftCountByCompanyRules,
                WeekendShiftCountByCompanyRules = -a.WeekendShiftCountByCompanyRules,
                TravelTime = -a.TravelTime,
                SingleFreeDays = -a.SingleFreeDays,
                DoubleFreeDays = -a.DoubleFreeDays,
                SharedRouteCounts = InvertArray(a.SharedRouteCounts),
            };
        }
        public static SaDriverInfo operator +(SaDriverInfo a, SaDriverInfo b) {
            return new SaDriverInfo(a.Instance) {
                Stats = a.Stats + b.Stats,
                PenaltyInfo = a.PenaltyInfo + b.PenaltyInfo,
                WorkedTime = a.WorkedTime + b.WorkedTime,
                ShiftCount = a.ShiftCount + b.ShiftCount,
                HotelCount = a.HotelCount + b.HotelCount,
                NightShiftCountByCompanyRules = a.NightShiftCountByCompanyRules + b.NightShiftCountByCompanyRules,
                WeekendShiftCountByCompanyRules = a.WeekendShiftCountByCompanyRules + b.WeekendShiftCountByCompanyRules,
                TravelTime = a.TravelTime + b.TravelTime,
                SingleFreeDays = a.SingleFreeDays + b.SingleFreeDays,
                DoubleFreeDays = a.DoubleFreeDays + b.DoubleFreeDays,
                SharedRouteCounts = AddArrays(a.SharedRouteCounts, b.SharedRouteCounts),
            };
        }
        public static SaDriverInfo operator -(SaDriverInfo a, SaDriverInfo b) => a + -b;

        public static bool AreEqual(SaDriverInfo a, SaDriverInfo b) {
            return (
                SaStats.AreEqual(a.Stats, b.Stats) &&
                SaDriverPenaltyInfo.AreEqual(a.PenaltyInfo, b.PenaltyInfo) &&
                a.WorkedTime == b.WorkedTime &&
                a.ShiftCount == b.ShiftCount &&
                a.HotelCount == b.HotelCount &&
                a.NightShiftCountByCompanyRules == b.NightShiftCountByCompanyRules &&
                a.WeekendShiftCountByCompanyRules == b.WeekendShiftCountByCompanyRules &&
                a.TravelTime == b.TravelTime &&
                a.SingleFreeDays == b.SingleFreeDays &&
                a.DoubleFreeDays == b.DoubleFreeDays &&
                AreArraysEqual(a.SharedRouteCounts, b.SharedRouteCounts)
            );
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
            Stats.DebugLog(isDiff, shouldLogZeros);
            PenaltyInfo.DebugLog(isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(WorkedTime, "Worked time", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ShiftCount, "Shift count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(HotelCount, "Hotel count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(NightShiftCountByCompanyRules, "Night shift count (company rules)", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(WeekendShiftCountByCompanyRules, "Weekend shift count (company rules)", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(TravelTime, "Travel time", isDiff, shouldLogZeros);
        }
    }
}

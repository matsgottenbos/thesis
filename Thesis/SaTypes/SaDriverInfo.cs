using System;
using System.Linq;

namespace Thesis {
    class SaDriverInfo {
        public readonly Instance Instance;
        public SaStats Stats;
        public SaDriverPenaltyInfo PenaltyInfo;
        public int WorkedTime, ShiftCount, HotelCount, NightShiftCountByCompanyRules, WeekendShiftCountByCompanyRules, TravelTime, SingleFreeDayCount, DoubleFreeDayCount, IdealShiftLengthScore, IdealRestingTimeScore;
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
                SingleFreeDayCount = -a.SingleFreeDayCount,
                DoubleFreeDayCount = -a.DoubleFreeDayCount,
                IdealShiftLengthScore = -a.IdealShiftLengthScore,
                IdealRestingTimeScore = -a.IdealRestingTimeScore,
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
                SingleFreeDayCount = a.SingleFreeDayCount + b.SingleFreeDayCount,
                DoubleFreeDayCount = a.DoubleFreeDayCount + b.DoubleFreeDayCount,
                IdealShiftLengthScore = a.IdealShiftLengthScore + b.IdealShiftLengthScore,
                IdealRestingTimeScore = a.IdealRestingTimeScore + b.IdealRestingTimeScore,
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
                a.SingleFreeDayCount == b.SingleFreeDayCount &&
                a.DoubleFreeDayCount == b.DoubleFreeDayCount &&
                a.IdealShiftLengthScore == b.IdealShiftLengthScore &&
                a.IdealRestingTimeScore == b.IdealRestingTimeScore &&
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
            ParseHelper.LogDebugValue(SingleFreeDayCount, "Single free days", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(DoubleFreeDayCount, "Double free days", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(IdealShiftLengthScore, "Ideal shift length score", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(IdealRestingTimeScore, "Ideal resting time score", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ParseHelper.ToString(SharedRouteCounts), "Shared route counts", isDiff, SharedRouteCounts.Sum() == 0, shouldLogZeros);
        }
    }
}

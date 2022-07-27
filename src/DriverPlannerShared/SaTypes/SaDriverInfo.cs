/*
 * Used to store calculated information about a driver's activity path, or a range of it
*/

namespace DriverPlannerShared {
    public class SaDriverInfo {
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
                SharedRouteCounts = ArrayHelper.InvertArray(a.SharedRouteCounts),
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
                SharedRouteCounts = ArrayHelper.AddArrays(a.SharedRouteCounts, b.SharedRouteCounts),
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
                ArrayHelper.AreArraysEqual(a.SharedRouteCounts, b.SharedRouteCounts)
            );
        }

        public void DebugLog(bool isDiff, bool shouldLogZeros = true) {
            Stats.DebugLog(isDiff, shouldLogZeros);
            PenaltyInfo.DebugLog(isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(WorkedTime, "Worked time", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(ShiftCount, "Shift count", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(HotelCount, "Hotel count", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(NightShiftCountByCompanyRules, "Night shift count (company rules)", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(WeekendShiftCountByCompanyRules, "Weekend shift count (company rules)", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(TravelTime, "Travel time", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(SingleFreeDayCount, "Single free days", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(DoubleFreeDayCount, "Double free days", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(IdealShiftLengthScore, "Ideal shift length score", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(IdealRestingTimeScore, "Ideal resting time score", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(ToStringHelper.ToString(SharedRouteCounts), "Shared route counts", isDiff, SharedRouteCounts.Sum() == 0, shouldLogZeros);
        }
    }
}

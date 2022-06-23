using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class SatisfactionCalculator {
        /* Driver satisfaction */

        public static double GetDriverSatisfaction(InternalDriver driver, SaDriverInfo driverInfo, SaInfo info) {
            double averageCriteriumSatisfaction = 0;
            double minimumCriteriumSatisfaction = 1;

            ProcessCriterion(RulesConfig.SatCriterionRouteVariation, GetDuplicateRouteCount(driverInfo), driver, ref averageCriteriumSatisfaction, ref minimumCriteriumSatisfaction);
            ProcessCriterion(RulesConfig.SatCriterionTravelTime, driverInfo.TravelTime, driver, ref averageCriteriumSatisfaction, ref minimumCriteriumSatisfaction);
            ProcessCriterion(RulesConfig.SatCriterionContractTime, driverInfo.WorkedTime, driver, ref averageCriteriumSatisfaction, ref minimumCriteriumSatisfaction);
            ProcessCriterion(RulesConfig.SatCriterionShiftLengths, driverInfo.IdealShiftLengthScore, driver, ref averageCriteriumSatisfaction, ref minimumCriteriumSatisfaction);
            ProcessCriterion(RulesConfig.SatCriterionRobustness, (float)driverInfo.Stats.Robustness, driver, ref averageCriteriumSatisfaction, ref minimumCriteriumSatisfaction);
            ProcessCriterion(RulesConfig.SatCriterionNightShifts, driverInfo.NightShiftCountByCompanyRules, driver, ref averageCriteriumSatisfaction, ref minimumCriteriumSatisfaction);
            ProcessCriterion(RulesConfig.SatCriterionWeekendShifts, driverInfo.WeekendShiftCountByCompanyRules, driver, ref averageCriteriumSatisfaction, ref minimumCriteriumSatisfaction);
            ProcessCriterion(RulesConfig.SatCriterionHotels, driverInfo.HotelCount, driver, ref averageCriteriumSatisfaction, ref minimumCriteriumSatisfaction);
            // TBA: time off requests
            // TBA: consecutive shifts
            ProcessCriterion(RulesConfig.SatCriterionConsecutiveFreeDays, GetConsecutiveFreeDaysScore(driverInfo), driver, ref averageCriteriumSatisfaction, ref minimumCriteriumSatisfaction);
            ProcessCriterion(RulesConfig.SatCriterionRestingTime, driverInfo.IdealRestingTimeScore, driver, ref averageCriteriumSatisfaction, ref minimumCriteriumSatisfaction);

            return (averageCriteriumSatisfaction + minimumCriteriumSatisfaction) / 2;
        }

        static void ProcessCriterion<T>(AbstractSatisfactionCriterion<T> criterion, T value, InternalDriver driver, ref double averageCriteriumSatisfaction, ref double minimumCriteriumSatisfaction) {
            averageCriteriumSatisfaction += criterion.GetSatisfaction(value, driver);
            minimumCriteriumSatisfaction = Math.Min(minimumCriteriumSatisfaction, criterion.GetSatisfactionForMinimum(value, driver));
        }

        public static Dictionary<string, double> GetDriverSatisfactionPerCriterion(InternalDriver driver, SaDriverInfo driverInfo) {
            Dictionary<string, double> satisfactionPerCriterion = new Dictionary<string, double> {
                { "hotelStays", RulesConfig.SatCriterionHotels.GetUnweightedSatisfaction(driverInfo.HotelCount, driver) },
                { "nightShifts", RulesConfig.SatCriterionNightShifts.GetUnweightedSatisfaction(driverInfo.NightShiftCountByCompanyRules, driver) },
                { "weekendShifts", RulesConfig.SatCriterionWeekendShifts.GetUnweightedSatisfaction(driverInfo.WeekendShiftCountByCompanyRules, driver) },
                { "travelTime", RulesConfig.SatCriterionTravelTime.GetUnweightedSatisfaction(driverInfo.TravelTime, driver) },
                { "duplicateRoutes", RulesConfig.SatCriterionRouteVariation.GetUnweightedSatisfaction(GetDuplicateRouteCount(driverInfo), driver) },
                { "consecutiveFreeDays", RulesConfig.SatCriterionConsecutiveFreeDays.GetUnweightedSatisfaction(GetConsecutiveFreeDaysScore(driverInfo), driver) },
                { "contractTime", RulesConfig.SatCriterionContractTime.GetUnweightedSatisfaction(driverInfo.WorkedTime, driver) },
            };
            return satisfactionPerCriterion;
        }

        static int GetDuplicateRouteCount(SaDriverInfo driverInfo) {
            int duplicateRouteCount = 0;
            for (int sharedRouteIndex = 0; sharedRouteIndex < driverInfo.SharedRouteCounts.Length; sharedRouteIndex++) {
                int count = driverInfo.SharedRouteCounts[sharedRouteIndex];
                if (count > 1) {
                    duplicateRouteCount += count - 1;
                }
            }
            return duplicateRouteCount;
        }

        static float GetConsecutiveFreeDaysScore(SaDriverInfo driverInfo) {
            // Criterium score is 100% when there are two consecutive free days, or otherwise 25% per single free day
            if (driverInfo.DoubleFreeDayCount >= 1) return 1;
            return driverInfo.SingleFreeDayCount * 0.25f;
        }


        /* Satisfaction score */

        public static double GetSatisfactionScore(SaInfo info) {
            // Average satisfaction
            double averageDriverSatisfaction = info.TotalInfo.Stats.DriverSatisfaction / info.Instance.InternalDrivers.Length;

            // Minimum driver satisfaction
            double minDriverSatisfaction = double.MaxValue;
            for (int driverIndex = 0; driverIndex < info.Instance.InternalDrivers.Length; driverIndex++) {
                SaDriverInfo driverInfo = info.DriverInfos[driverIndex];
                if (driverInfo.Stats.DriverSatisfaction < minDriverSatisfaction) minDriverSatisfaction = driverInfo.Stats.DriverSatisfaction;
            }

            // Total satisfaction
            double totalSatisfactionDiff = (averageDriverSatisfaction + minDriverSatisfaction) / 2;
            return totalSatisfactionDiff;
        }

        public static double GetSatisfactionScoreDiff(SaTotalInfo totalInfoDiff, Driver driver1, SaDriverInfo driver1InfoDiff, Driver driver2, SaDriverInfo driver2InfoDiff, SaInfo info) {
            // Average satisfaction
            double averageDriverSatisfactionDiff = totalInfoDiff.Stats.DriverSatisfaction / info.Instance.InternalDrivers.Length;

            // Minimum driver satisfaction
            double oldMinDriverSatisfaction = double.MaxValue;
            double newMinDriverSatisfaction = double.MaxValue;
            for (int driverIndex = 0; driverIndex < info.Instance.InternalDrivers.Length; driverIndex++) {
                SaDriverInfo oldDriverInfo = info.DriverInfos[driverIndex];

                // Get new minimum
                double newDriverSatisfaction = oldDriverInfo.Stats.DriverSatisfaction;
                if (driverIndex == driver1.AllDriversIndex) newDriverSatisfaction += driver1InfoDiff.Stats.DriverSatisfaction;
                else if (driver2 != null && driverIndex == driver2.AllDriversIndex) newDriverSatisfaction += driver2InfoDiff.Stats.DriverSatisfaction;

                // Update minimums
                if (oldDriverInfo.Stats.DriverSatisfaction < oldMinDriverSatisfaction) oldMinDriverSatisfaction = oldDriverInfo.Stats.DriverSatisfaction;
                if (newDriverSatisfaction < newMinDriverSatisfaction) newMinDriverSatisfaction = newDriverSatisfaction;
            }
            double minDriverSatisfactionDiff = newMinDriverSatisfaction - oldMinDriverSatisfaction;

            // Total satisfaction
            double totalSatisfactionDiff = (averageDriverSatisfactionDiff + minDriverSatisfactionDiff) / 2;
            return totalSatisfactionDiff;
        }
    }
}

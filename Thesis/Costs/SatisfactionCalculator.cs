using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class SatisfactionCalculator {
        /* Driver satisfaction */

        public static double GetDriverSatisfaction(InternalDriver driver, SaDriverInfo driverInfo, SaInfo info) {
            double averageCriterionSatisfaction = 0;
            double minimumCriterionSatisfaction = 1;

            ProcessCriterion(RulesConfig.SatCriterionRouteVariation, GetDuplicateRouteCount(driverInfo), driver, ref averageCriterionSatisfaction, ref minimumCriterionSatisfaction);
            ProcessCriterion(RulesConfig.SatCriterionTravelTime, driverInfo.TravelTime, driver, ref averageCriterionSatisfaction, ref minimumCriterionSatisfaction);
            ProcessCriterion(RulesConfig.SatCriterionContractTimeAccuracy, driverInfo.WorkedTime, driver, ref averageCriterionSatisfaction, ref minimumCriterionSatisfaction);
            ProcessCriterion(RulesConfig.SatCriterionShiftLengths, driverInfo.IdealShiftLengthScore, driver, ref averageCriterionSatisfaction, ref minimumCriterionSatisfaction);
            ProcessCriterion(RulesConfig.SatCriterionRobustness, (float)driverInfo.Stats.Robustness, driver, ref averageCriterionSatisfaction, ref minimumCriterionSatisfaction);
            ProcessCriterion(RulesConfig.SatCriterionNightShifts, driverInfo.NightShiftCountByCompanyRules, driver, ref averageCriterionSatisfaction, ref minimumCriterionSatisfaction);
            ProcessCriterion(RulesConfig.SatCriterionWeekendShifts, driverInfo.WeekendShiftCountByCompanyRules, driver, ref averageCriterionSatisfaction, ref minimumCriterionSatisfaction);
            ProcessCriterion(RulesConfig.SatCriterionHotelStays, driverInfo.HotelCount, driver, ref averageCriterionSatisfaction, ref minimumCriterionSatisfaction);
            // TBA: time off requests
            // TBA: consecutive shifts
            ProcessCriterion(RulesConfig.SatCriterionConsecutiveFreeDays, GetConsecutiveFreeDaysScore(driverInfo), driver, ref averageCriterionSatisfaction, ref minimumCriterionSatisfaction);
            ProcessCriterion(RulesConfig.SatCriterionRestingTime, driverInfo.IdealRestingTimeScore, driver, ref averageCriterionSatisfaction, ref minimumCriterionSatisfaction);

            return (averageCriterionSatisfaction + minimumCriterionSatisfaction) / 2;
        }

        static void ProcessCriterion<T>(AbstractSatisfactionCriterion<T> criterion, T value, InternalDriver driver, ref double averageCriterionSatisfaction, ref double minimumCriterionSatisfaction) {
            averageCriterionSatisfaction += criterion.GetSatisfaction(value, driver);
            minimumCriterionSatisfaction = Math.Min(minimumCriterionSatisfaction, criterion.GetSatisfactionForMinimum(value, driver));
        }

        public static Dictionary<string, double> GetDriverSatisfactionPerCriterion(InternalDriver driver, SaDriverInfo driverInfo) {
            Dictionary<string, double> satisfactionPerCriterion = new Dictionary<string, double> {
                { "routeVariation", RulesConfig.SatCriterionRouteVariation.GetUnweightedSatisfaction(GetDuplicateRouteCount(driverInfo), driver) },
                { "travelTime", RulesConfig.SatCriterionTravelTime.GetUnweightedSatisfaction(driverInfo.TravelTime, driver) },
                { "contractTimeAccuracy", RulesConfig.SatCriterionContractTimeAccuracy.GetUnweightedSatisfaction(driverInfo.WorkedTime, driver) },
                { "shiftLengths", RulesConfig.SatCriterionShiftLengths.GetUnweightedSatisfaction(driverInfo.IdealShiftLengthScore, driver) },
                { "robustness", RulesConfig.SatCriterionRobustness.GetUnweightedSatisfaction((float)driverInfo.Stats.Robustness, driver) },
                { "nightShifts", RulesConfig.SatCriterionNightShifts.GetUnweightedSatisfaction(driverInfo.NightShiftCountByCompanyRules, driver) },
                { "weekendShifts", RulesConfig.SatCriterionWeekendShifts.GetUnweightedSatisfaction(driverInfo.WeekendShiftCountByCompanyRules, driver) },
                { "hotelStays", RulesConfig.SatCriterionHotelStays.GetUnweightedSatisfaction(driverInfo.HotelCount, driver) },
                { "consecutiveFreeDays", RulesConfig.SatCriterionConsecutiveFreeDays.GetUnweightedSatisfaction(GetConsecutiveFreeDaysScore(driverInfo), driver) },
                { "restingTime", RulesConfig.SatCriterionRestingTime.GetUnweightedSatisfaction(driverInfo.IdealRestingTimeScore, driver) },
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
            // Criterion score is 100% when there are two consecutive free days, or otherwise 25% per single free day
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

using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public class RulesConfig {
        /* Shift constraints */
        /// <summary>The maximum number of shifts a driver can have per week.</summary>
        public static int DriverMaxShiftCount { get; private set; }
        /// <summary>Maximum length of a day shift, including travel (in minutes).</summary>
        public static int MaxFullDayShiftLength { get; private set; }
        /// <summary>Maximum length of a day shift, excluding travel (in minutes).</summary>
        public static int MaxMainDayShiftLength { get; private set; }
        /// <summary>Maximum length of a night shift, including travel (in minutes).</summary>
        public static int MaxFullNightShiftLength { get; private set; }
        /// <summary>Maximum length of a night shift, excluding travel (in minutes).</summary>
        public static int MaxMainNightShiftLength { get; private set; }
        /// <summary>Minimum required resting time after a day shift (in minutes).</summary>
        public static int MinRestTimeAfterDayShift { get; private set; }
        /// <summary>Minimum required resting time after a night shift (in minutes).</summary>
        public static int MinRestTimeAfterNightShift { get; private set; }
        /// <summary>Maximum allowed resting time during a hotel stay (in minutes).</summary>
        public static int HotelMaxRestTime { get; private set; }
        /// <summary>Additional travel time between two shifts when there is an hotel stay. This travel time is equally split between the shift before and after (in minutes).</summary>
        public static int HotelExtraTravelTime { get; private set; }
        /// <summary>Additional travel distance between two shifts when there is an hotel stay. This travel time is equally split between the shift before and after (in minutes).</summary>
        public static int HotelExtraTravelDistance { get; private set; }
        /// <summary>Minimum required resting time between two shifts to count as a single free day (in minutes).</summary>
        public static int SingleFreeDayMinRestTime { get; private set; }
        /// <summary>Minimum required resting time between two shifts to count as two consecutive free days (in minutes).</summary>
        public static int DoubleFreeDayMinRestTime { get; private set; }

        /* Shift type rules */
        /// <summary>Function determining whether a shift is a night shift, according to labour laws.</summary>
        public static Func<int, int, bool> IsNightShiftByLawFunc { get; private set; }
        /// <summary>Function determining whether a shift is a night shift, according to company rules.</summary>
        public static Func<int, int, bool> IsNightShiftByCompanyRulesFunc { get; private set; }
        /// <summary>Function determining whether a shift is a weekend shift, according to company rules.</summary>
        public static Func<int, int, bool> IsWeekendShiftByCompanyRulesFunc { get; private set; }

        /* Misc costs */
        /// <summary>Estimated costs of an hotel stay (hotel expenses + driver compensation).</summary>
        public static float HotelCosts { get; private set; }
        /// <summary>Additional costs for travel by pool car. Applied to intra-shift car travel, travel to pick up personal car, and travel to/from hotel.</summary>
        public static float SharedCarCostsPerKilometer { get; private set; }

        /* Satisfaction */
        /// <summary>Shift lengths above this threshold give a satisfaction penalty (in hours).</summary>
        public static int IdealShiftLength { get; private set; }
        /// <summary>Resting times below this threshold give a satisfaction penalty (in hours).</summary>
        public static int IdealRestTime { get; private set; }

        /* Robustness */
        /// <summary>Activities with these values of ActivityDescriptionEN are considered train driving activities, which have a higher probability to be delayed.</summary>
        public static string[] DrivingActivityTypes { get; private set; }
        /// <summary>Chance that a train driving activity has a delay.</summary>
        public static float DrivingActivityDelayProbability { get; private set; }
        /// <summary>Chance that a non-train-driving activity has a delay.</summary>
        public static float NonDrivingActivityDelayProbability { get; private set; }
        /// <summary>Added cost for each expected conflict due to delays, if the conflict is between activities of the same duty.</summary>
        public static float RobustnessCostFactorSameDuty { get; private set; }
        /// <summary>Added cost for each expected conflict due to delays, if the conflict is between activities of different duties but of the same project.</summary>
        public static float RobustnessCostFactorSameProject { get; private set; }
        /// <summary>Added cost for each expected conflict due to delays, if the conflict is between activities of different duties and projects.</summary>
        public static float RobustnessCostFactorDifferentProject { get; private set; }
        /// <summary>Activity mean delay by planned duration.</summary>
        public static Func<int, double> ActivityMeanDelayFunc { get; private set; }
        /// <summary>Alpha parameter of activity delay gamma distribution, by mean delay.</summary>
        public static Func<double, double> ActivityDelayGammaDistributionAlphaFunc { get; private set; }
        /// <summary>Beta parameter of activity delay gamma distribution, by mean delay.</summary>
        public static Func<double, double> ActivityDelayGammaDistributionBetaFunc { get; private set; }
        /// <summary>Expected travel delay by planned travel time.</summary>
        public static Func<int, int> TravelDelayExpectedFunc { get; private set; }

        /* Week & day parts */
        /// <summary>Weekend/non-weekend parts of the week.</summary>
        public static TimePart[] WeekPartsForWeekend { get; private set; }
        /// <summary>Day/night parts of the day.</summary>
        public static TimePart[] DayPartsForNight { get; private set; }

        /* Satisfaction criteria */
        /// <summary>Array of all satisfaction criteria with their possible types.</summary>
        public static AbstractSatisfactionCriterionInfo[] SatisfactionCriterionInfos { get; private set; }

        public static void Init(XSSFWorkbook settingsBook) {
            ProcessRulesSettings(settingsBook);
            ProcessWeekPartsSettings(settingsBook);
            ProcessDayPartsSettings(settingsBook);
            ProcessSatisfactionSettings(settingsBook);
        }

        static void ProcessRulesSettings(XSSFWorkbook settingsBook) {
            ExcelSheet rulesSettingsSheet = new ExcelSheet("Rules", settingsBook);
            Dictionary<string, ICell> rulesSettingsCellDict = ConfigHandler.GetSettingsValueCellsAsDict(rulesSettingsSheet);

            // Shift constraints
            DriverMaxShiftCount = ExcelSheet.GetIntValue(rulesSettingsCellDict["Max shift count"]).Value;
            MaxMainDayShiftLength = ConfigHandler.HourToMinuteValue(ExcelSheet.GetFloatValue(rulesSettingsCellDict["Max day shift length with travel"]).Value);
            MaxFullDayShiftLength = ConfigHandler.HourToMinuteValue(ExcelSheet.GetFloatValue(rulesSettingsCellDict["Max day shift length without travel"]).Value);
            MaxMainNightShiftLength = ConfigHandler.HourToMinuteValue(ExcelSheet.GetFloatValue(rulesSettingsCellDict["Max night shift length with travel"]).Value);
            MaxFullNightShiftLength = ConfigHandler.HourToMinuteValue(ExcelSheet.GetFloatValue(rulesSettingsCellDict["Max night shift length without travel"]).Value);
            MinRestTimeAfterDayShift = ConfigHandler.HourToMinuteValue(ExcelSheet.GetFloatValue(rulesSettingsCellDict["Min resting time after day shift"]).Value);
            MinRestTimeAfterNightShift = ConfigHandler.HourToMinuteValue(ExcelSheet.GetFloatValue(rulesSettingsCellDict["Min resting time after night shift"]).Value);
            HotelMaxRestTime = ConfigHandler.HourToMinuteValue(ExcelSheet.GetFloatValue(rulesSettingsCellDict["Max hotel stay length"]).Value);
            HotelExtraTravelTime = ConfigHandler.HourToMinuteValue(ExcelSheet.GetFloatValue(rulesSettingsCellDict["Hotel extra travel time"]).Value);
            HotelExtraTravelDistance = ConfigHandler.HourToMinuteValue(ExcelSheet.GetFloatValue(rulesSettingsCellDict["Hotel extra travel distance"]).Value);
            SingleFreeDayMinRestTime = ConfigHandler.HourToMinuteValue(ExcelSheet.GetFloatValue(rulesSettingsCellDict["Min resting time for free day"]).Value);
            DoubleFreeDayMinRestTime = ConfigHandler.HourToMinuteValue(ExcelSheet.GetFloatValue(rulesSettingsCellDict["Min resting time for double free day"]).Value);

            // Shift type rules
            IsNightShiftByLawFunc = GetShiftTypeRuleFunc(rulesSettingsCellDict, "Night shift by law rule type", "Night shift by law rule minimum");
            IsNightShiftByCompanyRulesFunc = GetShiftTypeRuleFunc(rulesSettingsCellDict, "Night shift by company rule type", "Night shift by company rule minimum");
            IsWeekendShiftByCompanyRulesFunc = GetShiftTypeRuleFunc(rulesSettingsCellDict, "Weekend shift by company rule type", "Weekend shift by company rule minimum");

            // Misc costs
            HotelCosts = ExcelSheet.GetFloatValue(rulesSettingsCellDict["Hotel costs"]).Value;
            SharedCarCostsPerKilometer = ExcelSheet.GetFloatValue(rulesSettingsCellDict["Shared car costs per kilometer"]).Value;

            // Satisfaction
            IdealShiftLength = ConfigHandler.HourToMinuteValue(ExcelSheet.GetFloatValue(rulesSettingsCellDict["Ideal shift length for satisfaction"]).Value);
            IdealRestTime = ConfigHandler.HourToMinuteValue(ExcelSheet.GetFloatValue(rulesSettingsCellDict["Ideal resting time for satisfaction"]).Value);

            /* Robustness */
            DrivingActivityTypes = ParseHelper.SplitAndCleanDataStringList(ExcelSheet.GetStringValue(rulesSettingsCellDict["Driving activity descriptions"]));
            DrivingActivityDelayProbability = ExcelSheet.GetFloatValue(rulesSettingsCellDict["Driving activity delay probability"]).Value;
            NonDrivingActivityDelayProbability = ExcelSheet.GetFloatValue(rulesSettingsCellDict["Non-driving activity delay probability"]).Value;
            RobustnessCostFactorSameDuty = ExcelSheet.GetFloatValue(rulesSettingsCellDict["Same duty expected conflict cost"]).Value;
            RobustnessCostFactorSameProject = ExcelSheet.GetFloatValue(rulesSettingsCellDict["Same project expected conflict cost"]).Value;
            RobustnessCostFactorDifferentProject = ExcelSheet.GetFloatValue(rulesSettingsCellDict["Different project expected conflict cost"]).Value;

            float meanDelayQuadraticCoefficient = ExcelSheet.GetFloatValue(rulesSettingsCellDict["Mean delay quadratic coefficient"]).Value;
            float meanDelayLinearCoefficient = ExcelSheet.GetFloatValue(rulesSettingsCellDict["Mean delay linear coefficient"]).Value;
            float meanDelayConstantCoefficient = ExcelSheet.GetFloatValue(rulesSettingsCellDict["Mean delay constant coefficient"]).Value;
            ActivityMeanDelayFunc = (int plannedDuration) => meanDelayQuadraticCoefficient * plannedDuration * plannedDuration + meanDelayLinearCoefficient * plannedDuration + meanDelayConstantCoefficient;

            float delayGammaDistributionCoefficient = ExcelSheet.GetFloatValue(rulesSettingsCellDict["Delay gamma distribution coefficient"]).Value;
            ActivityDelayGammaDistributionAlphaFunc = (double meanDelay) => delayGammaDistributionCoefficient * meanDelay * meanDelay;
            ActivityDelayGammaDistributionBetaFunc = (double meanDelay) => delayGammaDistributionCoefficient * meanDelay;

            float relativeTravelDelayFactor = ExcelSheet.GetFloatValue(rulesSettingsCellDict["Relative travel delay factor"]).Value;
            int constantTravelDelay = ConfigHandler.HourToMinuteValue(ExcelSheet.GetFloatValue(rulesSettingsCellDict["Constant travel delay"]).Value);
            TravelDelayExpectedFunc = (int plannedTravelTime) => (int)(relativeTravelDelayFactor * plannedTravelTime) + constantTravelDelay;
        }

        static void ProcessWeekPartsSettings(XSSFWorkbook settingsBook) {
            ExcelSheet weekPartsSettingsSheet = new ExcelSheet("Week parts", settingsBook);
            List<TimePart> weekPartsForWeekendList = new List<TimePart>();
            weekPartsSettingsSheet.ForEachRow(weekPartsSettingsRow => {
                int dayNum = weekPartsSettingsSheet.GetIntValue(weekPartsSettingsRow, "Start of part | Day").Value - 1;
                int hourNum = weekPartsSettingsSheet.GetIntValue(weekPartsSettingsRow, "Start of part | Hour").Value;
                int startTime = dayNum * DevConfig.DayLength + hourNum * DevConfig.HourLength;
                bool isWeekend = weekPartsSettingsSheet.GetBoolValue(weekPartsSettingsRow, "Is weekend").Value;
                weekPartsForWeekendList.Add(new TimePart(startTime, isWeekend));
            });
            WeekPartsForWeekend = weekPartsForWeekendList.ToArray();
    }

        static void ProcessDayPartsSettings(XSSFWorkbook settingsBook) {
            ExcelSheet dayPartsSettingsSheet = new ExcelSheet("Day parts", settingsBook);
            List<TimePart> dayPartsForWeekendList = new List<TimePart>();
            dayPartsSettingsSheet.ForEachRow(weekPartsSettingsRow => {
                int hourNum = dayPartsSettingsSheet.GetIntValue(weekPartsSettingsRow, "Start hour of part").Value;
                int startTime = hourNum * DevConfig.HourLength;
                bool isNight = dayPartsSettingsSheet.GetBoolValue(weekPartsSettingsRow, "Is night").Value;
                dayPartsForWeekendList.Add(new TimePart(startTime, isNight));
            });
            DayPartsForNight = dayPartsForWeekendList.ToArray();
        }

        static void ProcessSatisfactionSettings(XSSFWorkbook settingsBook) {
            ExcelSheet satisfactionSettingsSheet = new ExcelSheet("Satisfaction", settingsBook);

            Dictionary<string, Func<SaDriverInfo, float>> rangeCriterionNameToRelevantValueFunc = new Dictionary<string, Func<SaDriverInfo, float>>() {
                { "Route variation", driverInfo => SatisfactionCalculator.GetDuplicateRouteCount(driverInfo) },
                { "Travel time", driverInfo => driverInfo.TravelTime },
                { "Contract time accuracy", driverInfo => driverInfo.WorkedTime },
                { "Shift lengths", driverInfo => driverInfo.IdealShiftLengthScore },
                { "Expected delays", driverInfo => (float)driverInfo.Stats.Robustness },
                { "Night shifts", driverInfo => driverInfo.NightShiftCountByCompanyRules },
                { "Weekend shifts", driverInfo => driverInfo.WeekendShiftCountByCompanyRules },
                { "Hotel stays", driverInfo => driverInfo.HotelCount },
                { "Consecutive free days", driverInfo => SatisfactionCalculator.GetConsecutiveFreeDaysScore(driverInfo) },
                { "Resting time", driverInfo => driverInfo.IdealRestingTimeScore },
            };

            List<AbstractSatisfactionCriterionInfo> satisfactionCriterionInfosList = new List<AbstractSatisfactionCriterionInfo>();
            satisfactionSettingsSheet.ForEachRow(satisfactionSettingsRow => {
                string criterionName = satisfactionSettingsSheet.GetStringValue(satisfactionSettingsRow, "Criterion");
                string criterionMode = satisfactionSettingsSheet.GetStringValue(satisfactionSettingsRow, "Mode");
                float? worstThreshold = satisfactionSettingsSheet.GetFloatValue(satisfactionSettingsRow, "0% threshold");
                float? bestThreshold = satisfactionSettingsSheet.GetFloatValue(satisfactionSettingsRow, "100% threshold");
                if (criterionName == null || criterionMode == null || !worstThreshold.HasValue || !bestThreshold.HasValue) return;

                if (!rangeCriterionNameToRelevantValueFunc.ContainsKey(criterionName)) {
                    throw new Exception(string.Format("Unknown criterion name `{0}` in satisfaction settings", criterionName));
                }
                Func<SaDriverInfo, float> relevantValueFunc = rangeCriterionNameToRelevantValueFunc[criterionName];

                switch (criterionName) {
                    case "Contract time accuracy":
                        // Add special criterion types for contract time accuracy
                        satisfactionCriterionInfosList.Add(new MatchContractTimeSatisfactionCriterionInfo(criterionName, string.Format("{0} required", criterionMode), worstThreshold.Value, relevantValueFunc));
                        satisfactionCriterionInfosList.Add(new MaxContractTimeSatisfactionCriterionInfo(criterionName, string.Format("{0} optional", criterionMode), worstThreshold.Value, relevantValueFunc));
                        break;
                    case "Travel time":
                    case "Shift lengths":
                        // Add range criterion with hour to minute conversions
                        int worstThresholdMinutes = ConfigHandler.HourToMinuteValue(worstThreshold.Value);
                        int bestThresholdMinutes = ConfigHandler.HourToMinuteValue(bestThreshold.Value);
                        satisfactionCriterionInfosList.Add(new RangeSatisfactionCriterionInfo(criterionName, criterionMode, worstThresholdMinutes, bestThresholdMinutes, relevantValueFunc));
                        break;
                    default:
                        // Add normal range criterion type for all others
                        satisfactionCriterionInfosList.Add(new RangeSatisfactionCriterionInfo(criterionName, criterionMode, worstThreshold.Value, bestThreshold.Value, relevantValueFunc));
                        break;
                }
            });
            SatisfactionCriterionInfos = satisfactionCriterionInfosList.ToArray();
        }

        static Func<int, int, bool> GetShiftTypeRuleFunc(Dictionary<string, ICell> settingsCellDict, string ruleTypeSettingName, string ruleMinimumSettingName) {
            string type = ExcelSheet.GetStringValue(settingsCellDict[ruleTypeSettingName]);
            switch(type) {
                case "Absolute":
                    int minimumTime = ConfigHandler.HourToMinuteValue(ExcelSheet.GetFloatValue(settingsCellDict[ruleMinimumSettingName]).Value);
                    return (int mainShiftTimeAtNight, int mainShiftLength) => mainShiftTimeAtNight >= minimumTime;
                case "Relative":
                    float minimumFraction = ExcelSheet.GetFloatValue(settingsCellDict[ruleMinimumSettingName]).Value;
                    return (int mainShiftTimeAtNight, int mainShiftLength) => (float)mainShiftTimeAtNight / mainShiftLength >= minimumFraction;
                default:
                    throw new Exception(string.Format("Expected value `Absolute` or `Relative` for setting `{0}`, but found, `{1}`", ruleTypeSettingName, type));
            }
        }
    }
}

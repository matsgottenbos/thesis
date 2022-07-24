using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class RulesConfig {
        /* Shift constraints */
        /// <summary>The maximum number of shifts a driver can have per week.</summary>
        public static int DriverMaxShiftCount;
        /// <summary>Maximum length of a day shift, including travel (in minutes).</summary>
        public static int MaxFullDayShiftLength;
        /// <summary>Maximum length of a day shift, excluding travel (in minutes).</summary>
        public static int MaxMainDayShiftLength;
        /// <summary>Maximum length of a night shift, including travel (in minutes).</summary>
        public static int MaxFullNightShiftLength;
        /// <summary>Maximum length of a night shift, excluding travel (in minutes).</summary>
        public static int MaxMainNightShiftLength;
        /// <summary>Minimum required resting time after a day shift (in minutes).</summary>
        public static int MinRestTimeAfterDayShift;
        /// <summary>Minimum required resting time after a night shift (in minutes).</summary>
        public static int MinRestTimeAfterNightShift;
        /// <summary>Maximum allowed resting time during a hotel stay (in minutes).</summary>
        public static int HotelMaxRestTime;
        /// <summary>Additional travel time between two shifts when there is an hotel stay. This travel time is equally split between the shift before and after (in minutes).</summary>
        public static int HotelExtraTravelTime;
        /// <summary>Additional travel distance between two shifts when there is an hotel stay. This travel time is equally split between the shift before and after (in minutes).</summary>
        public static int HotelExtraTravelDistance;
        /// <summary>Minimum required resting time between two shifts to count as a single free day (in minutes).</summary>
        public static int SingleFreeDayMinRestTime;
        /// <summary>Minimum required resting time between two shifts to count as two consecutive free days (in minutes).</summary>
        public static int DoubleFreeDayMinRestTime;

        /* Shift type rules */
        /// <summary>Function determining whether a shift is a night shift, according to labour laws.</summary>
        public static Func<int, int, bool> IsNightShiftByLawFunc;
        /// <summary>Function determining whether a shift is a night shift, according to company rules.</summary>
        public static Func<int, int, bool> IsNightShiftByCompanyRulesFunc;
        /// <summary>Function determining whether a shift is a weekend shift, according to company rules.</summary>
        public static Func<int, int, bool> IsWeekendShiftByCompanyRulesFunc;

        /* Misc costs */
        /// <summary>Estimated costs of an hotel stay (hotel expenses + driver compensation).</summary>
        public static float HotelCosts;
        /// <summary>Additional costs for travel by pool car. Applied to intra-shift car travel, travel to pick up personal car, and travel to/from hotel.</summary>
        public static float SharedCarCostsPerKilometer;

        /* Satisfaction */
        /// <summary>Shift lengths above this threshold give a satisfaction penalty (in hours).</summary>
        public static int IdealShiftLength;
        /// <summary>Resting times below this threshold give a satisfaction penalty (in hours).</summary>
        public static int IdealRestTime;

        /* Robustness */
        /// <summary>Added cost for each expected conflict due to delays, if the conflict is between activities of the same duty.</summary>
        public static float RobustnessCostFactorSameDuty;
        /// <summary>Added cost for each expected conflict due to delays, if the conflict is between activities of different duties but of the same project.</summary>
        public static float RobustnessCostFactorSameProject;
        /// <summary>Added cost for each expected conflict due to delays, if the conflict is between activities of different duties and projects.</summary>
        public static float RobustnessCostFactorDifferentProject;
        /// <summary>Chance that a activity has a delay.</summary>
        public static float ActivityDelayProbability;
        /// <summary>Activity mean delay by planned duration.</summary>
        public static Func<int, double> ActivityMeanDelayFunc;
        /// <summary>Alpha parameter of activity delay gamma distribution, by mean delay.</summary>
        public static Func<double, double> ActivityDelayGammaDistributionAlphaFunc;
        /// <summary>Beta parameter of activity delay gamma distribution, by mean delay.</summary>
        public static Func<double, double> ActivityDelayGammaDistributionBetaFunc;
        /// <summary>Expected travel delay by planned travel time.</summary>
        public static Func<int, int> TravelDelayExpectedFunc;

        /* Week & day parts */
        /// <summary>Weekend/non-weekend parts of the week.</summary>
        public static TimePart[] WeekPartsForWeekend;
        /// <summary>Day/night parts of the day.</summary>
        public static TimePart[] DayPartsForNight;






        // Satisfaction
        public static readonly RangeSatisfactionCriterion SatCriterionRouteVariation = new RangeSatisfactionCriterion(10, 0, 0.2f, 0.2f);
        public static readonly RangeSatisfactionCriterion SatCriterionTravelTime = new RangeSatisfactionCriterion(40 * 60, 0, 0.1f, 0.2f);
        public static readonly MatchContractTimeSatisfactionCriterion SatCriterionContractTimeAccuracyRequiredDriver = new MatchContractTimeSatisfactionCriterion(0.4f, 0.2f, 0.2f);
        public static readonly MaxContractTimeSatisfactionCriterion SatCriterionContractTimeAccuracyOptionalDriver = new MaxContractTimeSatisfactionCriterion(0.4f, 0.2f, 0.2f);
        public static readonly RangeSatisfactionCriterion SatCriterionShiftLengths = new RangeSatisfactionCriterion(10 * 60, 0, 0.05f, 0.2f);
        public static readonly RangeSatisfactionCriterion SatCriterionRobustness = new RangeSatisfactionCriterion(800, 0, 0.05f, 0.2f);
        public static readonly RangeSatisfactionCriterion SatCriterionNightShifts = new RangeSatisfactionCriterion(5, 0, 0.05f, 0.2f);
        public static readonly RangeSatisfactionCriterion SatCriterionWeekendShifts = new RangeSatisfactionCriterion(3, 0, 0.05f, 0.2f);
        public static readonly RangeSatisfactionCriterion SatCriterionHotelStays = new RangeSatisfactionCriterion(4, 0, 0.15f, 0.2f);
        // TBA: time off requests
        // TBA: consecutive shifts
        public static readonly RangeSatisfactionCriterion SatCriterionConsecutiveFreeDays = new RangeSatisfactionCriterion(0, 1, 0.05f, 0.2f);
        public static readonly RangeSatisfactionCriterion SatCriterionRestingTime = new RangeSatisfactionCriterion(36, 0, 0.1f, 0.2f);

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
            RobustnessCostFactorSameDuty = ExcelSheet.GetFloatValue(rulesSettingsCellDict["Same duty expected conflict cost"]).Value;
            RobustnessCostFactorSameProject = ExcelSheet.GetFloatValue(rulesSettingsCellDict["Same project expected conflict cost"]).Value;
            RobustnessCostFactorDifferentProject = ExcelSheet.GetFloatValue(rulesSettingsCellDict["Different project expected conflict cost"]).Value;
            ActivityDelayProbability = ExcelSheet.GetFloatValue(rulesSettingsCellDict["Delay probability"]).Value;

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
            ExcelSheet satisfactionRulesSettingsSheet = new ExcelSheet("Satisfaction", settingsBook);

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

using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class SaConfig {
        // SA parameters
        /// <summary></summary>
        public static int LogFrequency;
        /// <summary></summary>
        public static int ThreadCallbackFrequency;
        /// <summary></summary>
        public static int ParameterUpdateFrequency;
        /// <summary></summary>
        public static float InitialTemperature;
        /// <summary></summary>
        public static float CycleMinInitialTemperature;
        /// <summary></summary>
        public static float CycleMaxInitialTemperature;
        /// <summary></summary>
        public static float TemperatureReductionFactor;
        /// <summary></summary>
        public static float EndCycleTemperature;
        /// <summary></summary>
        public static float CycleMinSatisfactionFactor;
        /// <summary></summary>
        public static float CycleMaxSatisfactionFactor;
        /// <summary>Chance of a full reset at the end of a cycle.</summary>
        public static float FullResetProb;
        /// <summary>Waiting times shorter than this count as the same shift; waiting time longer start a new shift.</summary>
        public static int ShiftWaitingTimeThreshold;
        /// <summary>Minimum cost difference to consider two solutions to be separate points on the pareto front.</summary>
        public static float ParetoFrontMinCostDiff;

        // Operation probabilities
        /// <summary></summary>
        public static float AssignInternalProbCumulative;
        /// <summary></summary>
        public static float AssignExternalProbCumulative;
        /// <summary></summary>
        public static float SwapProbCumulative;
        /// <summary></summary>
        public static float ToggleHotelProbCumulative;

        // Penalties
        /// <summary></summary>
        public static float OverlapViolationPenalty;
        /// <summary></summary>
        public static float ShiftLengthViolationPenalty;
        /// <summary></summary>
        public static float ShiftLengthViolationPenaltyPerMin;
        /// <summary></summary>
        public static float RestTimeViolationPenalty;
        /// <summary></summary>
        public static float RestTimeViolationPenaltyPerMin;
        /// <summary></summary>
        public static float InternalShiftCountViolationPenaltyPerShift;
        /// <summary></summary>
        public static float ExternalShiftCountPenaltyPerShift;
        /// <summary></summary>
        public static float InvalidHotelPenalty;
        /// <summary></summary>
        public static float QualificationViolationPenalty;

        public static void Init(XSSFWorkbook settingsBook) {
            ExcelSheet appSettingsSheet = new ExcelSheet("Algorithm", settingsBook);
            Dictionary<string, ICell> algorithmSettingsCellDict = ConfigHandler.GetSettingsValueCellsAsDict(appSettingsSheet);

            // Parameters
            LogFrequency = (int)ParseHelper.ParseLargeNumString(ExcelSheet.GetStringValue(algorithmSettingsCellDict["Log frequency"]));
            ThreadCallbackFrequency = (int)ParseHelper.ParseLargeNumString(ExcelSheet.GetStringValue(algorithmSettingsCellDict["Thread callback frequency"]));
            ParameterUpdateFrequency = (int)ParseHelper.ParseLargeNumString(ExcelSheet.GetStringValue(algorithmSettingsCellDict["Parameter update frequency"]));
            InitialTemperature = ExcelSheet.GetIntValue(algorithmSettingsCellDict["Initial temperature"]).Value;
            CycleMinInitialTemperature = ExcelSheet.GetIntValue(algorithmSettingsCellDict["Cycle min initial temperature"]).Value;
            CycleMaxInitialTemperature = ExcelSheet.GetIntValue(algorithmSettingsCellDict["Cycle max initial temperature"]).Value;
            TemperatureReductionFactor = ExcelSheet.GetFloatValue(algorithmSettingsCellDict["Temperature reduction factor"]).Value;
            EndCycleTemperature = ExcelSheet.GetFloatValue(algorithmSettingsCellDict["End cycle temperature"]).Value;
            CycleMinSatisfactionFactor = ExcelSheet.GetFloatValue(algorithmSettingsCellDict["Cycle min satisfaction factor"]).Value;
            CycleMaxSatisfactionFactor = ExcelSheet.GetFloatValue(algorithmSettingsCellDict["Cycle max satisfaction factor"]).Value;
            FullResetProb = ExcelSheet.GetFloatValue(algorithmSettingsCellDict["Full reset probability"]).Value;
            ShiftWaitingTimeThreshold = ConfigHandler.HourToMinuteValue(ExcelSheet.GetFloatValue(algorithmSettingsCellDict["Shift waiting time threshold"]).Value);
            ParetoFrontMinCostDiff = ExcelSheet.GetIntValue(algorithmSettingsCellDict["Pareto front min cost diff"]).Value;

            // Operation probabilities
            AssignInternalProbCumulative = ExcelSheet.GetFloatValue(algorithmSettingsCellDict["Assign internal"]).Value;
            AssignExternalProbCumulative = AssignInternalProbCumulative + ExcelSheet.GetFloatValue(algorithmSettingsCellDict["Assign external"]).Value;
            SwapProbCumulative = AssignExternalProbCumulative + ExcelSheet.GetFloatValue(algorithmSettingsCellDict["Swap"]).Value;
            ToggleHotelProbCumulative = SwapProbCumulative + ExcelSheet.GetFloatValue(algorithmSettingsCellDict["Toggle hotel"]).Value;

            // Penalties
            OverlapViolationPenalty = ExcelSheet.GetIntValue(algorithmSettingsCellDict["Overlap per violation"]).Value;
            ShiftLengthViolationPenalty = ExcelSheet.GetIntValue(algorithmSettingsCellDict["Shift length per violation"]).Value;
            ShiftLengthViolationPenaltyPerMin = ExcelSheet.GetIntValue(algorithmSettingsCellDict["Shift length per excess hour"]).Value / (float)DevConfig.HourLength;
            RestTimeViolationPenalty = ExcelSheet.GetIntValue(algorithmSettingsCellDict["Resting time per violation"]).Value;
            RestTimeViolationPenaltyPerMin = ExcelSheet.GetIntValue(algorithmSettingsCellDict["Resting time per deficient hour"]).Value / (float)DevConfig.HourLength;
            InternalShiftCountViolationPenaltyPerShift = ExcelSheet.GetIntValue(algorithmSettingsCellDict["Internal shift count per excess shift"]).Value;
            ExternalShiftCountPenaltyPerShift = ExcelSheet.GetIntValue(algorithmSettingsCellDict["External shift count per excess shift"]).Value;
            InvalidHotelPenalty = ExcelSheet.GetIntValue(algorithmSettingsCellDict["Invalid hotel per violation"]).Value;
            QualificationViolationPenalty = ExcelSheet.GetIntValue(algorithmSettingsCellDict["Qualification per violation"]).Value;
        }
    }
}

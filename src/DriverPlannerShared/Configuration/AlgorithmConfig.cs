using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Collections.Generic;

namespace DriverPlannerShared {
    public class AlgorithmConfig {
        /* Algorithm parameters */
        /// <summary>Number of iterations across all threads after which the algorithm logs its progress to the console.</summary>
        public static int LogFrequency { get; private set; }
        /// <summary>Number of iterations after which each algorithm threads sends an update to the multithread handler.</summary>
        public static int ThreadCallbackFrequency { get; private set; }
        /// <summary>Number of iterations after which the simulated annealing temperature is reduced.</summary>
        public static int TemperatureReductionFrequency { get; private set; }
        /// <summary>Factor with which the simulated annealing temperature is reduced.</summary>
        public static float TemperatureReductionFactor { get; private set; }
        /// <summary>Simulated annealing temperature when the algorithm starts.</summary>
        public static float InitialTemperature { get; private set; }
        /// <summary>Lower bound of the randomly selected simulated annealing temperature after a partial reset.</summary>
        public static float CycleMinInitialTemperature { get; private set; }
        /// <summary>Upper bound of the randomly selected simulated annealing temperature after a partial reset.</summary>
        public static float CycleMaxInitialTemperature { get; private set; }
        /// <summary>Simulated annealing temperature at which a partial or full reset happens.</summary>
        public static float EndCycleTemperature { get; private set; }
        /// <summary>Simulated annealing temperature at which a reset happens early if no valid solution was found this cycle.</summary>
        public static float EarlyEndCycleTemperature { get; private set; }
        /// <summary>Lower bound of the randomly selected satisfaction factor of a cycle.</summary>
        public static float CycleMinSatisfactionFactor { get; private set; }
        /// <summary>Upper bound of the randomly selected satisfaction factor of a cycle.</summary>
        public static float CycleMaxSatisfactionFactor { get; private set; }
        /// <summary>Chance of a full reset at the end of a cycle.</summary>
        public static float FullResetProb { get; private set; }
        /// <summary>Waiting times shorter than this count as the same shift; waiting time longer start a new shift.</summary>
        public static int ShiftWaitingTimeThreshold { get; private set; }
        /// <summary>Minimum cost difference to consider two solutions to be separate points on the pareto front.</summary>
        public static float ParetoFrontMinCostDiff { get; private set; }

        /* Operation probabilities */
        /// <summary>Cumulative probability of selecting the assign internal driver operation.</summary>
        public static float AssignInternalProbCumulative { get; private set; }
        /// <summary>Cumulative probability of selecting the assign external driver operation.</summary>
        public static float AssignExternalProbCumulative { get; private set; }
        /// <summary>Cumulative probability of selecting the swap drivers operation.</summary>
        public static float SwapProbCumulative { get; private set; }
        /// <summary>Cumulative probability of selecting the toggle hotel operation.</summary>
        public static float ToggleHotelProbCumulative { get; private set; }

        /* Penalties */
        /// <summary>Penalty cost added for each overlap violation.</summary>
        public static float OverlapViolationPenalty { get; private set; }
        /// <summary>Penalty cost added for each shift violation.</summary>
        public static float ShiftLengthViolationPenalty { get; private set; }
        /// <summary>Penalty cost added for each minute of shift violation.</summary>
        public static float ShiftLengthViolationPenaltyPerMin { get; private set; }
        /// <summary>Penalty cost added for each resting time violation.</summary>
        public static float RestTimeViolationPenalty { get; private set; }
        /// <summary>Penalty cost added for each minute of resting time violation.</summary>
        public static float RestTimeViolationPenaltyPerMin { get; private set; }
        /// <summary>Penalty cost added for each shift of internal driver shift count violation.</summary>
        public static float InternalShiftCountViolationPenaltyPerShift { get; private set; }
        /// <summary>Penalty cost added for each shift of external driver type shift count violation.</summary>
        public static float ExternalShiftCountPenaltyPerShift { get; private set; }
        /// <summary>Penalty cost added for each invalid hotel.</summary>
        public static float InvalidHotelPenalty { get; private set; }
        /// <summary>Penalty cost added for each availability violation.</summary>
        public static float AvailabilityViolationPenalty { get; private set; }
        /// <summary>Penalty cost added for each qualification violation.</summary>
        public static float QualificationViolationPenalty { get; private set; }

        public static void Init(XSSFWorkbook settingsBook) {
            ExcelSheet algorithmSettingsSheet = new ExcelSheet("Algorithm", settingsBook);
            Dictionary<string, ICell> algorithmSettingsCellDict = ConfigHandler.GetSettingsValueCellsAsDict(algorithmSettingsSheet);

            // Simulated annealing parameters
            LogFrequency = (int)ParseHelper.ParseLargeNumString(algorithmSettingsSheet.GetStringValue(algorithmSettingsCellDict["Log frequency"]));
            ThreadCallbackFrequency = (int)ParseHelper.ParseLargeNumString(algorithmSettingsSheet.GetStringValue(algorithmSettingsCellDict["Thread callback frequency"]));
            TemperatureReductionFrequency = (int)ParseHelper.ParseLargeNumString(algorithmSettingsSheet.GetStringValue(algorithmSettingsCellDict["Temperature reduction frequency"]));
            TemperatureReductionFactor = algorithmSettingsSheet.GetFloatValue(algorithmSettingsCellDict["Temperature reduction factor"]).Value;
            InitialTemperature = algorithmSettingsSheet.GetIntValue(algorithmSettingsCellDict["Initial temperature"]).Value;
            CycleMinInitialTemperature = algorithmSettingsSheet.GetIntValue(algorithmSettingsCellDict["Cycle min initial temperature"]).Value;
            CycleMaxInitialTemperature = algorithmSettingsSheet.GetIntValue(algorithmSettingsCellDict["Cycle max initial temperature"]).Value;
            EndCycleTemperature = algorithmSettingsSheet.GetFloatValue(algorithmSettingsCellDict["End cycle temperature"]).Value;
            EarlyEndCycleTemperature = algorithmSettingsSheet.GetFloatValue(algorithmSettingsCellDict["Early end cycle temperature"]).Value;
            CycleMinSatisfactionFactor = algorithmSettingsSheet.GetFloatValue(algorithmSettingsCellDict["Cycle min satisfaction factor"]).Value;
            CycleMaxSatisfactionFactor = algorithmSettingsSheet.GetFloatValue(algorithmSettingsCellDict["Cycle max satisfaction factor"]).Value;
            FullResetProb = algorithmSettingsSheet.GetFloatValue(algorithmSettingsCellDict["Full reset probability"]).Value;
            ShiftWaitingTimeThreshold = ConfigHandler.HourToMinuteValue(algorithmSettingsSheet.GetFloatValue(algorithmSettingsCellDict["Shift waiting time threshold"]).Value);
            ParetoFrontMinCostDiff = algorithmSettingsSheet.GetIntValue(algorithmSettingsCellDict["Pareto front min cost diff"]).Value;

            // Operation probabilities
            AssignInternalProbCumulative = algorithmSettingsSheet.GetFloatValue(algorithmSettingsCellDict["Assign internal"]).Value;
            AssignExternalProbCumulative = AssignInternalProbCumulative + algorithmSettingsSheet.GetFloatValue(algorithmSettingsCellDict["Assign external"]).Value;
            SwapProbCumulative = AssignExternalProbCumulative + algorithmSettingsSheet.GetFloatValue(algorithmSettingsCellDict["Swap"]).Value;
            ToggleHotelProbCumulative = SwapProbCumulative + algorithmSettingsSheet.GetFloatValue(algorithmSettingsCellDict["Toggle hotel"]).Value;

            // Penalties
            OverlapViolationPenalty = algorithmSettingsSheet.GetIntValue(algorithmSettingsCellDict["Overlap per violation"]).Value;
            ShiftLengthViolationPenalty = algorithmSettingsSheet.GetIntValue(algorithmSettingsCellDict["Shift length per violation"]).Value;
            ShiftLengthViolationPenaltyPerMin = algorithmSettingsSheet.GetIntValue(algorithmSettingsCellDict["Shift length per excess hour"]).Value / (float)DevConfig.HourLength;
            RestTimeViolationPenalty = algorithmSettingsSheet.GetIntValue(algorithmSettingsCellDict["Resting time per violation"]).Value;
            RestTimeViolationPenaltyPerMin = algorithmSettingsSheet.GetIntValue(algorithmSettingsCellDict["Resting time per deficient hour"]).Value / (float)DevConfig.HourLength;
            InternalShiftCountViolationPenaltyPerShift = algorithmSettingsSheet.GetIntValue(algorithmSettingsCellDict["Internal shift count per excess shift"]).Value;
            ExternalShiftCountPenaltyPerShift = algorithmSettingsSheet.GetIntValue(algorithmSettingsCellDict["External shift count per excess shift"]).Value;
            InvalidHotelPenalty = algorithmSettingsSheet.GetIntValue(algorithmSettingsCellDict["Invalid hotel per violation"]).Value;
            AvailabilityViolationPenalty = algorithmSettingsSheet.GetIntValue(algorithmSettingsCellDict["Availability per violation"]).Value;
            QualificationViolationPenalty = algorithmSettingsSheet.GetIntValue(algorithmSettingsCellDict["Qualification per violation"]).Value;
        }
    }
}

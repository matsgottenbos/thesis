using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    enum DataSource {
        Generator,
        Excel,
        Odata,
    }

    static class Config {
        // App
        public const bool RunOptimalAlgorithm = false;
        public const bool RunSimulatedAnnealing = true;
        public const DataSource SelectedDataSource = DataSource.Excel;
        //public const DataSource SelectedDataSource = DataSource.Generator;

        // Shifts
        public const int MaxShiftLengthWithTravel = 12 * 60; // Maximum length of a shift, including travel
        public const int MaxShiftLengthWithoutTravel = 10 * 60; // Maximum length of a shift, excluding travel
        public const int MinRestTime = 11 * 60; // Minimum required resting time between two shifts
        public const int ShiftWaitingTimeThreshold = 6 * 60; // Waiting times shorter than this count as the same trip; waiting time longer start a new shift
        public const int ShiftMaxStartTimeDiff = 24 * 60; // The maximum difference in start times considered when searching for trips in the same shift
        public const int BetweenShiftsMaxStartTimeDiff = 36 * 60; // The maximum difference in start times considered when checking rest time between different shifts

        // Time periods
        public const int DayLength = 24 * 60;

        // Salaries
        public static readonly SalaryRateInfo[] InternalDriverDailySalaryRates = new SalaryRateInfo[] {
            new SalaryRateInfo(0 * 60,  60 / 60f), // Night 0-6: hourly rate of 60
            new SalaryRateInfo(6 * 60,  55 / 60f), // Morning 6-7, hourly rate of 55
            new SalaryRateInfo(7 * 60,  50 / 60f), // Day 7-18, hourly rate of 50
            new SalaryRateInfo(18 * 60, 55 / 60f), // Evening 18-23, hourly rate of 55
            new SalaryRateInfo(23 * 60, 60 / 60f), // Night 23-6, hourly rate of 60
            // TODO: add weekends and holidays
        };
        public const float InternalDriverTravelSalaryRate = 50 / 60f;
        public const int InternalDriverUnpaidTravelTimePerShift = 60;
        public static readonly SalaryRateInfo[] ExternalDriverDailySalaryRates = new SalaryRateInfo[] {
            new SalaryRateInfo(0 * 60,  80 / 60f), // Night 0-6: hourly rate of 80
            new SalaryRateInfo(6 * 60,  75 / 60f), // Morning 6-7, hourly rate of 75
            new SalaryRateInfo(7 * 60,  70 / 60f), // Day 7-18, hourly rate of 70
            new SalaryRateInfo(18 * 60, 75 / 60f), // Evening 18-23, hourly rate of 75
            new SalaryRateInfo(23 * 60, 80 / 60f), // Night 23-6, hourly rate of 80
            // TODO: add weekends and holidays
        };
        public const float ExternalDriverTravelSalaryRate = 70 / 60f;

        // Hotels
        public const float HotelCosts = 130f;
        public const int HotelExtraTravelTime = 30;
        public const int HotelMaxRestTime = 24 * 60;

        // Contract time deviations
        public const float MinContractTimeFraction = 0.8f;
        public const float MaxContractTimeFraction = 1.2f;

        // Operations
        public const float AssignExternalDriverProbability = 0.2f;

        /* Generator */
        // Counts
        public const int GenTimeframeLength = 2 * 24 * 60;
        public const int GenStationCount = 10;
        public const int GenTripCount = 15;
        public const int GenInternalDriverCount = 4;
        public const int GenExternaDriverTypeCount = 2;
        public const int GenExternalDriverMinCountPerType = 2;
        public const int GenExternalDriverMaxCountPerType = 5;

        // Distances
        public const int GenMinStationTravelTime = 30;
        public const int GenMaxStationTravelTime = 4 * 60;
        public const int GenMaxHomeTravelTime = 2 * 60;

        // Contract times
        public const int GenInternalDriverContractTime = GenTimeframeLength / 4;

        // Generator probabilities
        public const float GenTrackProficiencyProb = 0.9f;


        /* Simulated annealing */
        // SA parameters
        public const int SaIterationCount = 500000000;
        public const int SaCheckCostFrequency = 100000;
        public const int SaLogFrequency = 1000000;
        public const int SaParameterUpdateFrequency = 100000;
        public const float SaInitialTemperature = 1000f;
        public const float SaCycleInitialTemperatureMin = 200f;
        public const float SaCycleInitialTemperatureMax = 1000f;
        public const float SaTemperatureReductionFactor = 0.99f;
        public const float SaEndCycleTemperature = 100f;

        // Operation probabilities
        public const float AssignInternalProbCumulative = 0.59f;
        public const float AssignExternalProbCumulative = 0.6f;
        public const float SwapProbCumulative = 0.9999f;
        public const float ToggleHotelProbCumulative = 1f;

        // Penalties
        public const float PrecendenceViolationPenalty = 5000;
        public const float ShiftLengthViolationPenalty = 1000;
        public const float ShiftLengthViolationPenaltyPerMin = 200 / 60f;
        public const float RestTimeViolationPenalty = 1000;
        public const float RestTimeViolationPenaltyPerMin = 200 / 60f;
        public const float ContractTimeViolationPenalty = 1000;
        public const float ContractTimeViolationPenaltyPerMin = 200 / 60f;
        public const float InvalidHotelPenalty = 5000;


        /* File structure */
        public static readonly string ProjectFolder = (Environment.Is64BitProcess ? Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName : Directory.GetParent(Environment.CurrentDirectory).Parent.FullName) + @"\"; // Path to the project root folder
        public static readonly string SolutionFolder = ProjectFolder + @"\..\"; // Path to the solution root folder
        public static readonly string DataFolder = Path.Combine(SolutionFolder, @"data\");


        /* Misc */
        // Floating point imprecision
        public const float FloatingPointMargin = 0.0001f;

        // Debug
        public const bool DebugCheckAndLogOperations = false;
        public const bool DebugSaLogCurrentSolution = false;
        public const bool DebugRunInspector = false;
        public const bool DebugRunOdataTest = false;
        public const bool DebugUseSeededSa = false;
    }

    class SalaryRateInfo {
        public readonly int StartTime;
        public readonly float SalaryRate;

        public SalaryRateInfo(int startTime, float salaryRate) {
            StartTime = startTime;
            SalaryRate = salaryRate;
        }
    }
}

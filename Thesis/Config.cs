using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class Config {
        // App
        public const bool RunOptimalAlgorithm = false;
        public const bool RunSimulatedAnnealing = true;

        // Shift
        public const int MaxWorkDayLength = 10 * 60; // Maximum length of a shift (including travel)
        public const int MinRestTime = 11 * 60; // Minimum required resting time between two shifts
        public const int ShiftWaitingTimeThreshold = 6 * 60; // Waiting times shorter than this count as the same trip; waiting time longer start a new shift
        public const int ShiftMaxStartTimeDiff = 24 * 60; // The maximum difference in start times considered when searching for trips in the same shift
        public const int BetweenShiftsMaxStartTimeDiff = 36 * 60; // The maximum difference in start times considered when checking rest time between different shifts

        // Salaries
        public const float SalaryRate = 50 / 60f;
        public const int UnpaidTravelTimePerDay = 60;

        // Contract time deviations
        //public const float MinContractTimeFraction = 0.8f;
        //public const float MaxContractTimeFraction = 1.2f;
        public const float MinContractTimeFraction = 0.6f;
        public const float MaxContractTimeFraction = 1.4f;

        /* Generator */
        // Counts
        public const int GenTimeframeLength = 2 * 24 * 60;
        public const int GenStationCount = 10;
        public const int GenTripCount = 15;
        public const int GenDriverCount = 10;
        public const int GenMaxStationCountPerTrip = 4;

        // Distances
        public const int GenMinStationTravelTime = 60;
        public const int GenMaxStationTravelTime = 3 * 60;

        // Contract times
        public const int GenMinContractTime = GenTimeframeLength / 6;
        public const int GenMaxContractTime = GenTimeframeLength / 3;

        // Generator probabilities
        public const float GenWithinDaySuccessorProb = 0.5f;
        public const float GenTrackProficiencyProb = 0.9f;


        /* Simulated annealing */
        // SA parameters
        public const int SaIterationCount = 50000000;
        public const int SaCheckCostFrequency = 100000;
        public const int SaLogFrequency = 1000000;
        public const int SaParameterUpdateFrequency = SaIterationCount / 1000;
        public const float SaInitialTemperature = 1500f;
        public const float SaTemperatureReductionFactor = 0.997f;
        public const float SaInitialPenaltyFactor = 0.001f;
        public const float SaPenaltyIncrement = 0.001f;

        // Penalties
        public const float PrecendenceViolationPenalty = 5000;
        public const float WorkDayLengthViolationPenalty = 1000;
        public const float WorkDayLengthViolationPenaltyPerMin = 200 / 60f;
        public const float RestTimeViolationPenalty = 1000;
        public const float RestTimeViolationPenaltyPerMin = 200 / 60f;
        public const float ContractTimeViolationPenalty = 1000;
        public const float ContractTimeViolationPenaltyPerMin = 200 / 60f;


        /* File structure */
        public static readonly string ProjectFolder = (Environment.Is64BitProcess ? Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName : Directory.GetParent(Environment.CurrentDirectory).Parent.FullName) + @"\"; // Path to the project root folder
        public static readonly string SolutionFolder = ProjectFolder + @"\..\"; // Path to the solution root folder
        public static readonly string DataFolder = Path.Combine(SolutionFolder, @"data\");


        /* Debug */
        public const bool DebugCheckOperations = false;
        public const bool DebugCheckAndLogOperations = true;
        public const bool DebugRunInspector = false;
        public const bool DebugUseSeededSa = true;
    }
}

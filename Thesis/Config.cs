using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class Config {
        // App
        public const bool RunOptimalAlgorithm = true;
        public const bool RunSimulatedAnnealing = true;

        // Working day
        public const int DayLength = 20 * 60;
        public const int MaxWorkDayLength = 10 * 60;

        // Salaries
        public const float SalaryRate = 50 / 60f;
        public const int UnpaidTravelTimePerDay = 60;

        // Contract time deviations
        public const float MinContractTimeFraction = 0.8f;
        public const float MaxContractTimeFraction = 1.2f;


        /* Generator */
        // Counts
        public const int GenDayCount = 2;
        public const int GenStationCount = 10;
        public const int GenTripCountPerDay = 10;
        public const int GenDriverCount = 10;
        public const int GenMaxStationCountPerTrip = 4;

        // Distances
        public const int GenMinStationTravelTime = 60;
        public const int GenMaxStationTravelTime = 3 * 60;

        // Contract times
        public const int GenMinContractTime = 5 * 60 * GenDayCount;
        public const int GenMaxContractTime = 10 * 60 * GenDayCount;

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
        public const float ContractTimeViolationPenalty = 1000;
        public const float ContractTimeViolationPenaltyPerMin = 200 / 60f;


        /* File structure */
        public static readonly string ProjectFolder = (Environment.Is64BitProcess ? Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName : Directory.GetParent(Environment.CurrentDirectory).Parent.FullName) + @"\"; // Path to the project root folder
        public static readonly string SolutionFolder = ProjectFolder + @"\..\"; // Path to the solution root folder
        public static readonly string DataFolder = Path.Combine(SolutionFolder, @"data\");


        /* Debug */
        public const bool DebugCheckOperations = false;
        public const bool DebugCheckAndLogOperations = false;
        public const bool DebugRunInspector = false;
    }
}

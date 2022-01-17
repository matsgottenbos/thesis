using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class Config {
        // App
        public const bool RunOptimalAlgorithm = false;
        public const bool RunSimulatedAnnealing = true;

        // Working day
        public const float DayLength = 20f;
        public const float MaxWorkDayLength = 10f;

        // Salaries
        public const float HourlyRate = 50f;

        // Contract hour deviations
        public const float MinContractHoursFraction = 0.8f;
        public const float MaxContractHoursFraction = 1.2f;


        /* Generator */
        // Counts
        public const int GenDayCount = 2;
        public const int GenStationCount = 10;
        public const int GenTripCountPerDay = 10;
        public const int GenDriverCount = 10;
        public const int GenMaxStationCountPerTrip = 4;

        // Distances
        public const float GenMaxDist = 3f;

        // Contract hours
        public const int GenMinContractHours = 5 * GenDayCount;
        public const int GenMaxContractHours = 10 * GenDayCount;

        // Generator probabilities
        public const float GenWithinDaySuccessorProb = 0.5f;
        public const float GenTrackProficiencyProb = 0.9f;


        /* Simulated annealing */
        // SA parameters
        public const int SaIterationCount = 200000000;
        public const int SaCheckCostFrequency = 100000;
        public const int SaLogFrequency = 1000000;
        public const int SaParameterUpdateFrequency = SaIterationCount / 1000;
        public const float SaInitialTemperature = 1500f;
        public const float SaTemperatureReductionFactor = 0.997f;
        public const float SaInitialPenaltyFactor = 0.001f;
        public const float SaPenaltyIncrement = 0.001f;

        // Penalties
        public const float PrecendenceViolationPenalty = 10000;
        public const float WorkDayLengthViolationPenalty = 1000;
        public const float WorkDayLengthViolationPenaltyPerHour = 1000;
        public const float ContractHoursViolationPenalty = 1000;
        public const float ContractHoursViolationPenaltyPerHour = 100;


        /* Debug */
        public const bool DebugCheckOperations = false;
        public const bool DebugCheckAndLogOperations = false;
    }
}

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

        // Counts
        public const int DayCount = 2;
        public const int StationCount = 10;
        //public const int TripCountPerDay = 5;
        public const int TripCountPerDay = 10;
        public const int DriverCount = 10;
        public const int MaxStationCountPerTrip = 4;

        // Distances
        public const float MaxDist = 3f;

        // Hours
        public const float DayLength = 20f;
        public const float MaxWorkDayLength = 10f;

        // Probabilities
        public const float WithinDaySuccessorProb = 0.5f;
        public const float TrackProficiencyProb = 0.9f;

        // Salaries
        public const float MinHourlyRate = 55f;
        public const float MaxHourlyRate = 65f;

        // Penalties
        public const float PrecendenceViolationPenalty = 10000;
        public const float WorkDayLengthExceedancePenalty = 1000;
        public const float WorkDayLengthExceedancePenaltyPerHour = 1000;

        // Simulated annealing
        public const int SaIterationCount = 10000000;
        public const int SaCheckCostFrequency = 100000;
        public const int SaLogFrequency = 100000;
        public const int SaParameterUpdateFrequency = SaIterationCount / 1000;
        public const float SaInitialTemperature = 1500f;
        public const float SaTemperatureReductionFactor = 0.995f;
        public const float SaInitialPenaltyFactor = 0.001f;
        public const float SaPenaltyIncrement = 0.001f;
    }
}

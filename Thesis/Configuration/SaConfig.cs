using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class SaConfig {
        /* Simulated annealing */
        // SA parameters
        public const int SaIterationCount = 200000000;
        public const int SaLogFrequency = 2000000;
        public const int SaThreadCallbackFrequency = 500000;
        public const int SaParameterUpdateFrequency = 100000;
        public const float SaInitialTemperature = 2000f;
        public const float SaCycleMinInitialTemperature = 500f;
        public const float SaCycleMaxInitialTemperature = 3000f;
        public const float SaTemperatureReductionFactor = 0.97f;
        public const float SaEndCycleTemperature = 0.1f;
        public const float SaCycleMinSatisfactionFactor = 0f;
        public const float SaCycleMaxSatisfactionFactor = 10f;
        public const float FullResetProb = 0.2f; // Chance of a full reset at the end of a cycle
        public const int ShiftWaitingTimeThreshold = 6 * 60; // Waiting times shorter than this count as the same shift; waiting time longer start a new shift
        public const float ParetoFrontMinCostDiff = 500f; // Minmum cost difference to consider two solutions to be separate points on the pareto front

        // Operation probabilities
        public const float AssignInternalProbCumulative = 0.7f;
        public const float AssignExternalProbCumulative = 0.8f;
        public const float SwapProbCumulative = 0.999f;
        public const float ToggleHotelProbCumulative = 1f;

        // Penalties
        public const double OverlapViolationPenalty = 10000;
        public const double ShiftLengthViolationPenalty = 1000;
        public const double ShiftLengthViolationPenaltyPerMin = 1000 / 60f;
        public const double RestTimeViolationPenalty = 1000;
        public const double RestTimeViolationPenaltyPerMin = 1000 / 60f;
        public const double InternalShiftCountViolationPenaltyPerShift = 2000;
        public const double ExternalShiftCountPenaltyPerShift = 4000;
        public const double InvalidHotelPenalty = 4000;
        public const double QualificationViolationPenalty = 4000;
    }
}

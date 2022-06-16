using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class SaConfig {
        /* Simulated annealing */
        // SA parameters
        public const int SaIterationCount = 500000000;
        public const int SaCheckCostFrequency = 100000;
        public const int SaLogFrequency = 1000000;
        public const int SaParameterUpdateFrequency = 100000;
        public const float SaInitialTemperature = 5000f;
        public const float SaCycleMinInitialTemperature = 500f;
        public const float SaCycleMaxInitialTemperature = 7000f;
        public const float SaTemperatureReductionFactor = 0.99f;
        public const float SaEndCycleTemperature = 300f;
        public const float SaCycleMinSatisfactionFactor = 0f;
        public const float SaCycleMaxSatisfactionFactor = 0.5f;
        public const int ShiftWaitingTimeThreshold = 6 * 60; // Waiting times shorter than this count as the same trip; waiting time longer start a new shift
        public const float ParetoFrontMinCostDiff = 500f; // Minmum cost different to consider two solutions to be separate points on the pareto front

        // Operation probabilities
        public const float AssignInternalProbCumulative = 0.3f;
        public const float AssignExternalProbCumulative = 0.6f;
        public const float SwapProbCumulative = 0.9999f;
        public const float ToggleHotelProbCumulative = 1f;

        // Penalties
        public const double PrecendenceViolationPenalty = 20000;
        public const double ShiftLengthViolationPenalty = 5000;
        public const double ShiftLengthViolationPenaltyPerMin = 5000 / 60f;
        public const double RestTimeViolationPenalty = 5000;
        public const double RestTimeViolationPenaltyPerMin = 5000 / 60f;
        public const double InternalShiftCountViolationPenaltyPerShift = 20000;
        public const double InvalidHotelPenalty = 20000;
        public const double ExternalShiftCountPenaltyPerShift = 20000;
    }
}

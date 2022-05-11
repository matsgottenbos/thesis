using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    abstract class SatisfactionCriterium<T> {
        public readonly T WorstThreshold, BestThreshold;
        public readonly float Weight;

        public SatisfactionCriterium(T worstThreshold, T bestThreshold, float weight) {
            WorstThreshold = worstThreshold;
            BestThreshold = bestThreshold;
            Weight = weight;
        }

        public abstract double GetSatisfaction(T value);
    }

    class IntSatisfactionCriterium : SatisfactionCriterium<int> {
        public IntSatisfactionCriterium(int worstThreshold, int bestThreshold, float weight) : base(worstThreshold, bestThreshold, weight) {
        }

        public override double GetSatisfaction(int value) {
            return Weight * (value - WorstThreshold) / (BestThreshold - WorstThreshold);
        }
    }

    class FloatSatisfactionCriterium : SatisfactionCriterium<float> {
        public FloatSatisfactionCriterium(float worstThreshold, float bestThreshold, float weight) : base(worstThreshold, bestThreshold, weight) {
        }

        public override double GetSatisfaction(float value) {
            return Weight * (value - WorstThreshold) / (BestThreshold - WorstThreshold);
        }
    }
}

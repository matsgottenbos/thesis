using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    abstract class AbstractSatisfactionCriterium<T> {
        protected readonly float weight;

        public AbstractSatisfactionCriterium(float weight) {
            this.weight = weight;
        }

        public double GetSatisfaction(T value, InternalDriver driver) {
            return weight * GetUnweightedSatisfaction(value, driver);
        }

        protected abstract double GetUnweightedSatisfaction(T value, InternalDriver driver);
    }

    class RangeSatisfactionCriterium : AbstractSatisfactionCriterium<float> {
        readonly float worstThreshold, bestThreshold;

        public RangeSatisfactionCriterium(float worstThreshold, float bestThreshold, float weight) : base (weight) {
            this.worstThreshold = worstThreshold;
            this.bestThreshold = bestThreshold;
        }

        protected override double GetUnweightedSatisfaction(float value, InternalDriver driver) {
            return Math.Max(0, Math.Min((value - worstThreshold) / (bestThreshold - worstThreshold), 1));
        }
    }

    class TargetSatisfactionCriterium : AbstractSatisfactionCriterium<float> {
        readonly Func<InternalDriver, float> targetFunc, worstDeviationFunc;

        public TargetSatisfactionCriterium(Func<InternalDriver, float> targetFunc, Func<InternalDriver, float> worstDeviationFunc, float weight) : base(weight) {
            this.targetFunc = targetFunc;
            this.worstDeviationFunc = worstDeviationFunc;
        }

        protected override double GetUnweightedSatisfaction(float value, InternalDriver driver) {
            float deviation = Math.Abs(value - targetFunc(driver));
            return Math.Min(deviation / worstDeviationFunc(driver), 1);
        }
    }

    class ConsecutiveFreeDaysCriterium : AbstractSatisfactionCriterium<(int, int)> {

        public ConsecutiveFreeDaysCriterium(float weight) : base(weight) { }

        protected override double GetUnweightedSatisfaction((int, int) value, InternalDriver driver) {
            // Satisfaction is 100% when there are two consecutive free days, or otherwise 25% per single free day
            if (value.Item2 >= 1) return 1;
            return value.Item1 * 0.25;
        }
    }
}

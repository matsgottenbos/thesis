using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    abstract class AbstractSatisfactionCriterion<T> {
        protected readonly float weight, maxWeight;

        public AbstractSatisfactionCriterion(float weight, float maxWeight) {
            this.weight = weight;
            this.maxWeight = maxWeight;
        }

        public double GetSatisfaction(T value, InternalDriver driver) {
            return weight * GetUnweightedSatisfaction(value, driver);
        }

        /** Get the satisfaction value, with the difference to 50% scaled relative to the max weight */
        public double GetSatisfactionForMinimum(T value, InternalDriver driver) {
            double satisfactionDiffToMidpoint = GetUnweightedSatisfaction(value, driver) - 0.5;
            return 0.5 + weight / maxWeight * satisfactionDiffToMidpoint;
        }

        public abstract double GetUnweightedSatisfaction(T value, InternalDriver driver);
    }

    class RangeSatisfactionCriterion : AbstractSatisfactionCriterion<float> {
        readonly float worstThreshold, bestThreshold;

        public RangeSatisfactionCriterion(float worstThreshold, float bestThreshold, float weight, float maxWeight) : base (weight, maxWeight) {
            this.worstThreshold = worstThreshold;
            this.bestThreshold = bestThreshold;
        }

        public override double GetUnweightedSatisfaction(float value, InternalDriver driver) {
            return Math.Max(0, Math.Min((value - worstThreshold) / (bestThreshold - worstThreshold), 1));
        }
    }

    class TargetSatisfactionCriterion : AbstractSatisfactionCriterion<float> {
        readonly Func<InternalDriver, float> targetFunc, worstDeviationFunc;

        public TargetSatisfactionCriterion(Func<InternalDriver, float> targetFunc, Func<InternalDriver, float> worstDeviationFunc, float weight, float maxWeight) : base(weight, maxWeight) {
            this.targetFunc = targetFunc;
            this.worstDeviationFunc = worstDeviationFunc;
        }

        public override double GetUnweightedSatisfaction(float value, InternalDriver driver) {
            float deviation = Math.Abs(value - targetFunc(driver));
            return Math.Max(0, 1 - deviation / worstDeviationFunc(driver));
        }
    }

    class ConsecutiveFreeDaysCriterion : AbstractSatisfactionCriterion<(int, int)> {

        public ConsecutiveFreeDaysCriterion(float weight, float maxWeight) : base(weight, maxWeight) { }

        public override double GetUnweightedSatisfaction((int, int) value, InternalDriver driver) {
            // Satisfaction is 100% when there are two consecutive free days, or otherwise 25% per single free day
            if (value.Item2 >= 1) return 1;
            return value.Item1 * 0.25;
        }
    }
}

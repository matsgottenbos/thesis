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

    class MatchContractTimeSatisfactionCriterion : AbstractSatisfactionCriterion<float> {
        readonly float worstDeviationFaction;

        public MatchContractTimeSatisfactionCriterion(float worstDeviationFaction, float weight, float maxWeight) : base(weight, maxWeight) {
            this.worstDeviationFaction = worstDeviationFaction;
        }

        public override double GetUnweightedSatisfaction(float value, InternalDriver driver) {
            float deviationFraction = Math.Abs(value / driver.ContractTime - 1);
            return Math.Max(0, 1 - deviationFraction / worstDeviationFaction);
        }
    }

    class MaxContractTimeSatisfactionCriterion : AbstractSatisfactionCriterion<float> {
        readonly float worstDeviationFaction;

        public MaxContractTimeSatisfactionCriterion(float worstDeviationFaction, float weight, float maxWeight) : base(weight, maxWeight) {
            this.worstDeviationFaction = worstDeviationFaction;
        }

        public override double GetUnweightedSatisfaction(float value, InternalDriver driver) {
            float excessFraction = Math.Max(0, value / driver.ContractTime - 1);
            return Math.Max(0, 1 - excessFraction / worstDeviationFaction);
        }
    }
}

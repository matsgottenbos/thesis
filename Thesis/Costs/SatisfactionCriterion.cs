using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    abstract class AbstractSatisfactionCriterionInfo {
        public readonly string Name, Mode;
        protected readonly Func<SaDriverInfo, float> relevantValueFunc;

        public AbstractSatisfactionCriterionInfo(string name, string mode, Func<SaDriverInfo, float> relevantValueFunc) {
            Name = name;
            Mode = mode;
            this.relevantValueFunc = relevantValueFunc;
        }

        public double GetUnweightedSatisfaction(InternalDriver driver, SaDriverInfo driverInfo) {
            float value = relevantValueFunc(driverInfo);
            return GetUnweightedSatisfactionByValue(value, driver);
        }

        protected abstract double GetUnweightedSatisfactionByValue(float value, InternalDriver driver);
    }

    class RangeSatisfactionCriterionInfo : AbstractSatisfactionCriterionInfo {
        readonly float worstThreshold, bestThreshold;

        public RangeSatisfactionCriterionInfo(string name, string mode, float worstThreshold, float bestThreshold, Func<SaDriverInfo, float> relevantValueFunc) : base(name, mode, relevantValueFunc) {
            this.worstThreshold = worstThreshold;
            this.bestThreshold = bestThreshold;
        }

        protected override double GetUnweightedSatisfactionByValue(float value, InternalDriver driver) {
            return Math.Max(0, Math.Min((value - worstThreshold) / (bestThreshold - worstThreshold), 1));
        }
    }

    class MatchContractTimeSatisfactionCriterionInfo : AbstractSatisfactionCriterionInfo {
        readonly float worstDeviationFaction;

        public MatchContractTimeSatisfactionCriterionInfo(string name, string mode, float worstDeviationFaction, Func<SaDriverInfo, float> relevantValueFunc) : base(name, mode, relevantValueFunc) {
            this.worstDeviationFaction = worstDeviationFaction;
        }

        protected override double GetUnweightedSatisfactionByValue(float value, InternalDriver driver) {
            float deviationFraction = Math.Abs(value / driver.ContractTime - 1);
            return Math.Max(0, 1 - deviationFraction / worstDeviationFaction);
        }
    }

    class MaxContractTimeSatisfactionCriterionInfo : AbstractSatisfactionCriterionInfo {
        readonly float worstDeviationFaction;

        public MaxContractTimeSatisfactionCriterionInfo(string name, string mode, float worstDeviationFaction, Func<SaDriverInfo, float> relevantValueFunc) : base(name, mode, relevantValueFunc) {
            this.worstDeviationFaction = worstDeviationFaction;
        }

        protected override double GetUnweightedSatisfactionByValue(float value, InternalDriver driver) {
            float excessFraction = Math.Max(0, value / driver.ContractTime - 1);
            return Math.Max(0, 1 - excessFraction / worstDeviationFaction);
        }
    }


    class SatisfactionCriterion {
        public readonly AbstractSatisfactionCriterionInfo CriterionInfo;
        readonly float weight;
        float maxWeight;

        public SatisfactionCriterion(AbstractSatisfactionCriterionInfo criterionInfo, float weight) {
            CriterionInfo = criterionInfo;
            this.weight = weight;
        }

        public void SetMaxWeight(float maxWeight) {
            this.maxWeight = maxWeight;
        }

        public double GetSatisfaction(InternalDriver driver, SaDriverInfo driverInfo) {
            return weight * CriterionInfo.GetUnweightedSatisfaction(driver, driverInfo);
        }

        /** Get the satisfaction value, with the difference to 50% scaled relative to the max weight */
        public double GetSatisfactionForMinimum(InternalDriver driver, SaDriverInfo driverInfo) {
            double satisfactionDiffToMidpoint = CriterionInfo.GetUnweightedSatisfaction(driver, driverInfo) - 0.5;
            return 0.5 + weight / maxWeight * satisfactionDiffToMidpoint;
        }
    }
}

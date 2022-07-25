using System;
using System.Collections.Generic;

namespace Thesis {
    class InternalDriver : Driver {
        public readonly int InternalIndex, ContractTime;
        public readonly SatisfactionCriterion[] SatisfactionCriteria;
        readonly string InternalDriverName;
        public readonly bool IsOptional;
        readonly InternalSalarySettings internalSalarySettings;

        public InternalDriver(int allDriversIndex, int internalIndex, string internalDriverName, bool isInternational, bool isOptional, int[] homeTravelTimes, int[] homeTravelDistances, bool[] activityAvailability, bool[] activityQualifications, int contractTime, InternalSalarySettings internalSalarySettings, SatisfactionCriterion[] satisfactionCriteria) : base(allDriversIndex, isInternational, true, homeTravelTimes, homeTravelDistances, activityAvailability, activityQualifications, internalSalarySettings) {
            InternalIndex = internalIndex;
            InternalDriverName = internalDriverName;
            IsOptional = isOptional;
            ContractTime = contractTime;
            this.internalSalarySettings = internalSalarySettings;
            SatisfactionCriteria = satisfactionCriteria;
        }

        public override string GetId() {
            return InternalIndex.ToString();
        }

        public string GetInternalDriverName(bool useRealName) {
            if (useRealName) return InternalDriverName;
            return string.Format("Driver {0}", InternalIndex + 1);
        }

        int GetPaidTravelTime(int travelTime) {
            return Math.Max(0, travelTime - internalSalarySettings.UnpaidTravelTimePerShift);
        }

        public override float GetPaidTravelCost(int travelTime, int travelDistance) {
            return GetPaidTravelTime(travelTime) * internalSalarySettings.TravelTimeSalaryRate;
        }

        public override double GetSatisfaction(SaDriverInfo driverInfo) {
            if (IsOptional) return 0;

            double averageCriterionSatisfaction = 0;
            double minimumCriterionSatisfaction = 1;

            for (int criterionIndex = 0; criterionIndex < SatisfactionCriteria.Length; criterionIndex++) {
                SatisfactionCriterion criterion = SatisfactionCriteria[criterionIndex];
                averageCriterionSatisfaction += criterion.GetSatisfaction(this, driverInfo);
                minimumCriterionSatisfaction = Math.Min(minimumCriterionSatisfaction, criterion.GetSatisfactionForMinimum(this, driverInfo));
            }

            return (averageCriterionSatisfaction + minimumCriterionSatisfaction) / 2;
        }

        public Dictionary<string, double> GetSatisfactionPerCriterion(SaDriverInfo driverInfo) {
            Dictionary<string, double> satisfactionPerCriterion = new Dictionary<string, double>();
            for (int criterionIndex = 0; criterionIndex < SatisfactionCriteria.Length; criterionIndex++) {
                SatisfactionCriterion criterion = SatisfactionCriteria[criterionIndex];
                string name = criterion.CriterionInfo.Name;
                double unweightedSatisfaction = criterion.CriterionInfo.GetUnweightedSatisfaction(this, driverInfo);
                satisfactionPerCriterion.Add(name, unweightedSatisfaction);
            }
            return satisfactionPerCriterion;
        }
    }
}

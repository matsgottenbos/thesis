using System;

namespace Thesis {
    class InternalDriver : Driver {
        public readonly int InternalIndex, ContractTime;
        public readonly AbstractSatisfactionCriterion<float> SatCriterionContractTimeAccuracy;
        readonly string InternalDriverName;
        public readonly bool IsOptional;
        readonly InternalSalarySettings internalSalarySettings;

        public InternalDriver(int allDriversIndex, int internalIndex, string internalDriverName, bool isInternational, bool isOptional, int[] homeTravelTimes, int[] homeTravelDistances, bool[] activityQualifications, int contractTime, InternalSalarySettings internalSalarySettings) : base(allDriversIndex, isInternational, true, homeTravelTimes, homeTravelDistances, activityQualifications, internalSalarySettings) {
            InternalIndex = internalIndex;
            InternalDriverName = internalDriverName;
            IsOptional = isOptional;
            ContractTime = contractTime;
            this.internalSalarySettings = internalSalarySettings;
            SatCriterionContractTimeAccuracy = isOptional ? RulesConfig.SatCriterionContractTimeAccuracyOptionalDriver : RulesConfig.SatCriterionContractTimeAccuracyRequiredDriver;
        }

        public override string GetId() {
            return InternalIndex.ToString();
        }

        public string GetInternalDriverName(bool useRealName) {
            if (useRealName) return InternalDriverName;
            return string.Format("Driver {0}", InternalIndex + 1);
        }

        public override double GetSatisfaction(SaDriverInfo driverInfo) {
            if (IsOptional) return 0;
            return SatisfactionCalculator.GetDriverSatisfaction(this, driverInfo);
        }

        int GetPaidTravelTime(int travelTime) {
            return Math.Max(0, travelTime - internalSalarySettings.UnpaidTravelTimePerShift);
        }

        public override float GetPaidTravelCost(int travelTime, int travelDistance) {
            return GetPaidTravelTime(travelTime) * internalSalarySettings.TravelTimeSalaryRate;
        }
    }
}

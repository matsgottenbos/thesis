using System;

namespace Thesis {
    class InternalDriver : Driver {
        public readonly int InternalIndex, ContractTime;
        readonly string InternalDriverName;
        readonly InternalSalarySettings internalSalarySettings;

        public InternalDriver(int allDriversIndex, int internalIndex, string internalDriverName, bool isInternational, int[] homeTravelTimes, int[] homeTravelDistances, bool[,] trackProficiencies, int contractTime, InternalSalarySettings internalSalarySettings) : base(allDriversIndex, isInternational, true, homeTravelTimes, homeTravelDistances, trackProficiencies, internalSalarySettings) {
            InternalIndex = internalIndex;
            InternalDriverName = internalDriverName;
            ContractTime = contractTime;
            this.internalSalarySettings = internalSalarySettings;
        }

        public override string GetId() {
            return InternalIndex.ToString();
        }

        public string GetInternalDriverName(bool useRealName) {
            if (useRealName) return InternalDriverName;
            return string.Format("Driver {0}", InternalIndex + 1);
        }

        public override double GetSatisfaction(SaDriverInfo driverInfo, SaInfo info) {
            return SatisfactionCalculator.GetDriverSatisfaction(this, driverInfo, info);
        }

        int GetPaidTravelTime(int travelTime) {
            return Math.Max(0, travelTime - internalSalarySettings.UnpaidTravelTimePerShift);
        }

        public override float GetPaidTravelCost(int travelTime, int travelDistance) {
            return GetPaidTravelTime(travelTime) * internalSalarySettings.TravelTimeSalaryRate;
        }
    }
}

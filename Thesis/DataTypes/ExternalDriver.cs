using System;

namespace Thesis {
    class ExternalDriver : Driver {
        public readonly int ExternalDriverTypeIndex, IndexInType;
        public readonly string CompanyName, ExternalDriverTypeName;
        readonly ExternalSalarySettings externalSalarySettings;

        public ExternalDriver(int allDriversIndex, int externalDriverTypeIndex, int indexInType, string companyName, string externalDriverTypeName, bool isInternational, bool isHotelAllowed, int[] homeTravelTimes, bool[] activityQualifications, int[] homeTravelDistances, ExternalSalarySettings externalSalarySettings) : base(allDriversIndex, isInternational, isHotelAllowed, homeTravelTimes, homeTravelDistances, activityQualifications, externalSalarySettings) {
            ExternalDriverTypeIndex = externalDriverTypeIndex;
            IndexInType = indexInType;
            CompanyName = companyName;
            ExternalDriverTypeName = externalDriverTypeName;
            this.externalSalarySettings = externalSalarySettings;
        }

        public override string GetId() {
            return string.Format("e{0}.{1}", ExternalDriverTypeIndex, IndexInType);
        }

        public string GetExternalDriverName(int actualIndexInType) {
            return string.Format("{0} {1}", ExternalDriverTypeName, actualIndexInType + 1);
        }

        public override bool IsAvailableDuringRange(int rangeStartTime, int rangeEndTime) {
            return true;
        }

        public override double GetSatisfaction(SaDriverInfo driverInfo) {
            return 0;
        }

        int GetPaidTravelDistance(int travelDistance) {
            return Math.Max(0, travelDistance - externalSalarySettings.UnpaidTravelDistancePerShift);
        }

        public override float GetPaidTravelCost(int travelTime, int travelDistance) {
            return GetPaidTravelDistance(travelDistance) * externalSalarySettings.TravelDistanceSalaryRate;
        }
    }
}

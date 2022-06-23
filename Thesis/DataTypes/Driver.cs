using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    abstract class Driver {
        public readonly int AllDriversIndex;
        protected readonly bool isInternational;
        readonly int[] homeTravelTimes, homeTravelDistances;
        protected Instance instance;
        readonly SalarySettings salarySettings;

        public Driver(int allDriversIndex, bool isInternational, int[] homeTravelTimes, int[] homeTravelDistances, SalarySettings salarySettings) {
            AllDriversIndex = allDriversIndex;
            this.isInternational = isInternational;
            this.homeTravelTimes = homeTravelTimes;
            this.homeTravelDistances = homeTravelDistances;
            this.salarySettings = salarySettings;
        }

        public void SetInstance(Instance instance) {
            this.instance = instance;
        }

        public abstract string GetId();
        public abstract string GetName(bool useRealName);

        public float DrivingCost(Trip firstTripInternal, Trip lastTripInternal) {
            return instance.ShiftInfo(firstTripInternal, lastTripInternal).GetDrivingCost(salarySettings.DriverTypeIndex);
        }

        public int HomeTravelTimeToStart(Trip trip) {
            return homeTravelTimes[trip.StartStationAddressIndex];
        }

        public int HomeTravelDistanceToStart(Trip trip) {
            return homeTravelDistances[trip.StartStationAddressIndex];
        }

        public abstract float GetPaidTravelCost(int travelTime, int travelDistance);


        public abstract double GetSatisfaction(SaDriverInfo driverInfo, SaInfo info);
    }

    class InternalDriver : Driver {
        public readonly int InternalIndex, ContractTime;
        readonly string InternalDriverName;
        public readonly bool[,] TrackProficiencies;
        readonly InternalSalarySettings internalSalarySettings;

        public InternalDriver(int allDriversIndex, int internalIndex, string internalDriverName, bool isInternational, int[] homeTravelTimes, int[] homeTravelDistances, bool[,] trackProficiencies, int contractTime, InternalSalarySettings internalSalarySettings) : base(allDriversIndex, isInternational, homeTravelTimes, homeTravelDistances, internalSalarySettings) {
            InternalIndex = internalIndex;
            InternalDriverName = internalDriverName;
            TrackProficiencies = trackProficiencies;
            ContractTime = contractTime;
            this.internalSalarySettings = internalSalarySettings;
        }

        public override string GetId() {
            return InternalIndex.ToString();
        }

        public override string GetName(bool useRealName) {
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

    class ExternalDriver : Driver {
        public readonly int ExternalDriverTypeIndex, IndexInType;
        public readonly string CompanyName, ShortCompanyName;
        readonly ExternalSalarySettings externalSalarySettings;

        public ExternalDriver(int allDriversIndex, int externalDriverTypeIndex, int indexInType, string companyName, string shortCompanyName, bool isInternational, int[] homeTravelTimes, int[] homeTravelDistances, ExternalSalarySettings externalSalarySettings) : base(allDriversIndex, isInternational, homeTravelTimes, homeTravelDistances, externalSalarySettings) {
            ExternalDriverTypeIndex = externalDriverTypeIndex;
            IndexInType = indexInType;
            CompanyName = companyName;
            ShortCompanyName = shortCompanyName;
            this.externalSalarySettings = externalSalarySettings;
        }

        public override string GetId() {
            return string.Format("e{0}.{1}", ExternalDriverTypeIndex, IndexInType);
        }

        public override string GetName(bool useRealName) {
            string nationalInternationalStr = isInternational ? "international" : "national";
            return string.Format("{0} {1} {2}", ShortCompanyName, nationalInternationalStr, IndexInType + 1);
        }

        public override double GetSatisfaction(SaDriverInfo driverInfo, SaInfo info) {
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

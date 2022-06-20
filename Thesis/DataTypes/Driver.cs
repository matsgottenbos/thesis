using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    abstract class Driver {
        public readonly int AllDriversIndex;
        readonly int[] homeTravelTimes;
        readonly SalarySettings salaryInfo;
        protected Instance instance;

        public Driver(int allDriversIndex, int[] homeTravelTimes, SalarySettings salaryInfo) {
            AllDriversIndex = allDriversIndex;
            this.homeTravelTimes = homeTravelTimes;
            this.salaryInfo = salaryInfo;
        }

        public void SetInstance(Instance instance) {
            this.instance = instance;
        }

        public abstract string GetId();
        public abstract string GetName(bool useRealName);


        /*** Helper methods ***/

        /* Shift lengths and costs */

        public float DrivingCost(Trip firstTripInternal, Trip lastTripInternal) {
            return instance.ShiftInfo(firstTripInternal, lastTripInternal).GetDrivingCost(salaryInfo.DriverTypeIndex);
        }

        public (int, int) ShiftLengthWithCustomPickup(Trip firstTripInternal, Trip lastTripInternal, Trip parkingTrip) {
            int shiftLengthWithoutTravel = instance.ShiftInfo(firstTripInternal, lastTripInternal).DrivingTime;
            int shiftLengthWithTravel = shiftLengthWithoutTravel + HomeTravelTimeToStart(firstTripInternal) + instance.CarTravelTime(lastTripInternal, parkingTrip) + HomeTravelTimeToStart(parkingTrip);
            return (shiftLengthWithoutTravel, shiftLengthWithTravel);
        }

        public float ShiftCostWithCustomPickup(Trip firstTripInternal, Trip lastTripInternal, Trip parkingTrip) {
            float drivingCost = DrivingCost(firstTripInternal, lastTripInternal);
            int travelTime = HomeTravelTimeToStart(firstTripInternal) + instance.CarTravelTime(lastTripInternal, parkingTrip) + HomeTravelTimeToStart(parkingTrip);
            float travelCost = GetPaidTravelCost(travelTime);
            return drivingCost + travelCost;
        }


        /* Travelling */

        public int HomeTravelTimeToStart(Trip trip) {
            return homeTravelTimes[trip.StartStationAddressIndex];
        }

        int GetPaidTravelTime(int travelTime) {
            return Math.Max(0, travelTime - salaryInfo.UnpaidTravelTimePerShift);
        }

        public float GetPaidTravelCost(int travelTime) {
            return GetPaidTravelTime(travelTime) * salaryInfo.TravelSalaryRate;
        }


        /* Satisfaction */

        public abstract double GetSatisfaction(SaDriverInfo driverInfo);
    }

    class InternalDriver : Driver {
        public readonly int InternalIndex, ContractTime;
        readonly string InternalDriverName;
        public readonly bool[,] TrackProficiencies;

        public InternalDriver(int allDriversIndex, int internalIndex, string internalDriverName, int[] oneWayTravelTimes, bool[,] trackProficiencies, int contractTime, SalarySettings salaryInfo) : base(allDriversIndex, oneWayTravelTimes, salaryInfo) {
            InternalIndex = internalIndex;
            InternalDriverName = internalDriverName;
            TrackProficiencies = trackProficiencies;
            ContractTime = contractTime;
        }

        public override string GetId() {
            return InternalIndex.ToString();
        }

        public override string GetName(bool useRealName) {
            if (useRealName) return InternalDriverName;
            return string.Format("Driver {0}", InternalIndex + 1);
        }

        public override double GetSatisfaction(SaDriverInfo driverInfo) {
            return SatisfactionCalculator.GetDriverSatisfaction(driverInfo, this);
        }
    }

    class ExternalDriver : Driver {
        public readonly int ExternalDriverTypeIndex, IndexInType;

        public ExternalDriver(int allDriversIndex, int externalDriverTypeIndex, int indexInType, int[] oneWayTravelTimes, SalarySettings salaryInfo) : base(allDriversIndex, oneWayTravelTimes, salaryInfo) {
            ExternalDriverTypeIndex = externalDriverTypeIndex;
            IndexInType = indexInType;
        }

        public override string GetId() {
            return string.Format("e{0}.{1}", ExternalDriverTypeIndex, IndexInType);
        }

        public override string GetName(bool useRealName) {
            return string.Format("External {0}.{1}", ExternalDriverTypeIndex + 1, IndexInType + 1);
        }

        public override double GetSatisfaction(SaDriverInfo driverInfo) {
            return 0;
        }
    }
}

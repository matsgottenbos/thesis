﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    abstract class Driver {
        public readonly int AllDriversIndex;
        readonly int[] homeTravelTimes;
        readonly float travelSalaryRate;
        protected Instance instance;

        public Driver(int allDriversIndex, int[] homeTravelTimes, float travelSalaryRate) {
            AllDriversIndex = allDriversIndex;
            this.homeTravelTimes = homeTravelTimes;
            this.travelSalaryRate = travelSalaryRate;
        }

        public void SetInstance(Instance instance) {
            this.instance = instance;
        }

        public abstract string GetId();
        public abstract string GetName(bool useRealName);


        /*** Helper methods ***/

        /* Shift lengths and costs */

        public abstract float DrivingCost(Trip firstTripInternal, Trip lastTripInternal);

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
            return homeTravelTimes[trip.StartStationIndex];
        }

        protected abstract int GetPaidTravelTime(int travelTime);

        public float GetPaidTravelCost(int travelTime) {
            return GetPaidTravelTime(travelTime) * travelSalaryRate;
        }


        /* Contract time */

        public abstract int GetMinContractTimeViolation(int workedTime);
        public abstract int GetMaxContractTimeViolation(int workedTime);

        public int GetTotalContractTimeViolation(int workedTime) {
            return GetMinContractTimeViolation(workedTime) + GetMaxContractTimeViolation(workedTime);
        }


        /* Penalties */

        public double GetContractTimePenalty(int workedTime, bool debugIsNew) {
            int contractTimeViolation = GetTotalContractTimeViolation(workedTime);

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                if (debugIsNew) SaDebugger.GetCurrentNormalDiff().ContractTime.AddNew(workedTime);
                else SaDebugger.GetCurrentNormalDiff().ContractTime.AddOld(workedTime);
            }
            #endif

            if (contractTimeViolation > 0) {
                return Config.ContractTimeViolationPenalty + contractTimeViolation * Config.ContractTimeViolationPenaltyPerMin;
            }
            return 0;
        }


        /* Satisfaction */

        public abstract double GetSatisfaction(DriverInfo driverInfo);
    }

    class InternalDriver : Driver {
        public readonly int InternalIndex, MinContractTime, MaxContractTime;
        readonly string InternalDriverName;
        public readonly bool[,] TrackProficiencies;

        public InternalDriver(int allDriversIndex, int internalIndex, string internalDriverName, int[] oneWayTravelTimes, int minWorkedTime, int maxWorkedTime, bool[,] trackProficiencies) : base(allDriversIndex, oneWayTravelTimes, Config.InternalDriverTravelSalaryRate) {
            InternalIndex = internalIndex;
            InternalDriverName = internalDriverName;
            MinContractTime = minWorkedTime;
            MaxContractTime = maxWorkedTime;
            TrackProficiencies = trackProficiencies;
        }

        public override string GetId() {
            return InternalIndex.ToString();
        }

        public override string GetName(bool useRealName) {
            if (useRealName) return InternalDriverName;
            return string.Format("Driver {0}", InternalIndex + 1);
        }

        public override float DrivingCost(Trip firstTripInternal, Trip lastTripInternal) {
            return instance.ShiftInfo(firstTripInternal, lastTripInternal).InternalDrivingCost;
        }

        protected override int GetPaidTravelTime(int travelTime) {
            return Math.Max(0, travelTime - Config.InternalDriverUnpaidTravelTimePerShift);
        }

        public override int GetMinContractTimeViolation(int workedTime) {
            return Math.Max(0, MinContractTime - workedTime);
        }

        public override int GetMaxContractTimeViolation(int workedTime) {
            return Math.Max(0, workedTime - MaxContractTime);
        }

        public override double GetSatisfaction(DriverInfo driverInfo) {
            return SatisfactionCalculator.GetDriverSatisfaction(driverInfo);
        }
    }

    class ExternalDriver : Driver {
        public readonly int ExternalDriverTypeIndex, IndexInType;

        public ExternalDriver(int allDriversIndex, int externalDriverTypeIndex, int indexInType, int[] oneWayTravelTimes) : base(allDriversIndex, oneWayTravelTimes, Config.ExternalDriverTravelSalaryRate) {
            ExternalDriverTypeIndex = externalDriverTypeIndex;
            IndexInType = indexInType;
        }

        public override string GetId() {
            return string.Format("e{0}.{1}", ExternalDriverTypeIndex, IndexInType);
        }

        public override string GetName(bool useRealName) {
            return string.Format("External {0}.{1}", ExternalDriverTypeIndex + 1, IndexInType + 1);
        }

        public override float DrivingCost(Trip firstTripInternal, Trip lastTripInternal) {
            return instance.ShiftInfo(firstTripInternal, lastTripInternal).ExternalDrivingCost;
        }

        protected override int GetPaidTravelTime(int travelTime) {
            return travelTime;
        }

        public override int GetMinContractTimeViolation(int workedTime) {
            return 0;
        }

        public override int GetMaxContractTimeViolation(int workedTime) {
            return 0;
        }

        public override double GetSatisfaction(DriverInfo driverInfo) {
            return 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    abstract class Driver {
        public readonly int AllDriversIndex;
        readonly int[] homeTravelTimes;
        readonly float travelSalaryRate;
        readonly int[,] drivingTimes, shiftLengthsWithPickup;
        readonly float[,] drivingCosts, shiftCostsWithPickup;
        Instance instance;

        public Driver(int allDriversIndex, int[] homeTravelTimes, float travelSalaryRate, int[,] drivingTimes, float[,] drivingCosts, int[,] shiftLengthsWithPickup, float[,] shiftCostsWithPickup) {
            AllDriversIndex = allDriversIndex;
            this.homeTravelTimes = homeTravelTimes;
            this.travelSalaryRate = travelSalaryRate;
            this.drivingTimes = drivingTimes;
            this.drivingCosts = drivingCosts;
            this.shiftLengthsWithPickup = shiftLengthsWithPickup;
            this.shiftCostsWithPickup = shiftCostsWithPickup;
        }

        public void SetInstance(Instance instance) {
            this.instance = instance;
        }

        public abstract string GetId();


        /*** Helper methods ***/

        /* Shift lengths and costs */

        public int DrivingTime(Trip firstTripInternal, Trip lastTripInternal) {
            return drivingTimes[firstTripInternal.Index, lastTripInternal.Index];
        }

        public float DrivingCost(Trip firstTripInternal, Trip lastTripInternal) {
            return drivingCosts[firstTripInternal.Index, lastTripInternal.Index];
        }

        // Obsolete; only used in old optimal solver code
        public int ShiftLengthWithPickup(Trip firstTripInternal, Trip lastTripInternal) {
            return shiftLengthsWithPickup[firstTripInternal.Index, lastTripInternal.Index];
        }

        // Obsolete; only used in old optimal solver code
        public float ShiftCostWithPickup(Trip firstTripInternal, Trip lastTripInternal) {
            return shiftCostsWithPickup[firstTripInternal.Index, lastTripInternal.Index];
        }

        public (int, int) ShiftLengthWithCustomPickup(Trip firstTripInternal, Trip lastTripInternal, Trip parkingTrip) {
            int shiftLengthWithoutTravel = DrivingTime(firstTripInternal, lastTripInternal);
            int shiftLengthWithTravel = shiftLengthWithoutTravel + HomeTravelTimeToStart(firstTripInternal) + instance.CarTravelTime(lastTripInternal, parkingTrip) + HomeTravelTimeToStart(parkingTrip);
            return (shiftLengthWithoutTravel, shiftLengthWithTravel);
        }

        public float ShiftCostWithCustomPickup(Trip firstTripInternal, Trip lastTripInternal, Trip parkingTrip) {
            float drivingCost = DrivingCost(firstTripInternal, lastTripInternal);
            int travelTime = HomeTravelTimeToStart(firstTripInternal) + instance.CarTravelTime(lastTripInternal, parkingTrip) + HomeTravelTimeToStart(parkingTrip);
            float travelCost = GetPaidTravelCost(travelTime);
            return drivingCost + travelCost;
        }


        /* Rest time */

        // Obsolete; only used in old optimal solver code
        public int RestTimeWithPickup(Trip shift1FirstTrip, Trip shift1LastTrip, Trip shift2FirstTrip) {
            return RestTimeWithCustomPickup(shift1LastTrip, shift2FirstTrip, shift1FirstTrip);
        }

        // Obsolete; only used in obsolete RestTimeWithPickup
        public int RestTimeWithCustomPickup(Trip tripBeforeTravel, Trip tripAfterTravel, Trip parkingTrip) {
            return tripAfterTravel.StartTime - tripBeforeTravel.EndTime - instance.CarTravelTime(tripBeforeTravel, parkingTrip) - HomeTravelTimeToStart(parkingTrip) - HomeTravelTimeToStart(tripAfterTravel);
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

        public float GetContractTimePenalty(int workedTime, bool debugIsNew) {
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
    }

    class InternalDriver : Driver {
        public readonly int InternalIndex, MinContractTime, MaxContractTime;
        public readonly string DriverName;
        public readonly bool[,] TrackProficiencies;

        public InternalDriver(int allDriversIndex, int internalIndex, string driverName, int[] oneWayTravelTimes, int[,] drivingTimes, float[,] drivingCosts, int[,] shiftLengthsWithPickup, float[,] shiftCostsWithPickup, int minWorkedTime, int maxWorkedTime, bool[,] trackProficiencies) : base(allDriversIndex, oneWayTravelTimes, Config.InternalDriverTravelSalaryRate, drivingTimes, drivingCosts, shiftLengthsWithPickup, shiftCostsWithPickup) {
            InternalIndex = internalIndex;
            DriverName = driverName;
            MinContractTime = minWorkedTime;
            MaxContractTime = maxWorkedTime;
            TrackProficiencies = trackProficiencies;
        }

        public override string GetId() {
            return InternalIndex.ToString();
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
    }

    class ExternalDriver : Driver {
        public readonly int ExternalDriverTypeIndex, IndexInType;

        public ExternalDriver(int allDriversIndex, int externalDriverTypeIndex, int indexInType, int[] oneWayTravelTimes, int[,] drivingTimes, float[,] drivingCosts, int[,] shiftLengthsWithPickup, float[,] shiftCostsWithPickup) : base(allDriversIndex, oneWayTravelTimes, Config.ExternalDriverTravelSalaryRate, drivingTimes, drivingCosts, shiftLengthsWithPickup, shiftCostsWithPickup) {
            ExternalDriverTypeIndex = externalDriverTypeIndex;
            IndexInType = indexInType;
        }

        public override string GetId() {
            return string.Format("e{0}.{1}", ExternalDriverTypeIndex, IndexInType);
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
    }
}

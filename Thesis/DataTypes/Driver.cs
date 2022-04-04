using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    abstract class Driver {
        public readonly int AllDriversIndex;
        readonly int[] OneWayTravelTimes, TwoWayPayedTravelTimes;
        readonly float TravelSalaryRate;
        readonly int[,] DrivingTimes, ShiftLengthsWithoutPickup, ShiftLengthsWithPickup;
        readonly float[,] DrivingCosts, ShiftCostsWithPickup;
        Instance instance;

        public Driver(int allDriversIndex, int[] oneWayTravelTimes, int[] twoWayPayedTravelTimes, float travelSalaryRate, int[,] drivingTimes, float[,] drivingCosts, int[,] shiftLengthsWithoutPickup, int[,] shiftLengthsWithPickup, float[,] shiftCostsWithPickup) {
            AllDriversIndex = allDriversIndex;
            OneWayTravelTimes = oneWayTravelTimes;
            TwoWayPayedTravelTimes = twoWayPayedTravelTimes;
            TravelSalaryRate = travelSalaryRate;
            DrivingTimes = drivingTimes;
            DrivingCosts = drivingCosts;
            ShiftLengthsWithoutPickup = shiftLengthsWithoutPickup;
            ShiftLengthsWithPickup = shiftLengthsWithPickup;
            ShiftCostsWithPickup = shiftCostsWithPickup;
        }

        public void SetInstance(Instance instance) {
            this.instance = instance;
        }

        public abstract string GetId();


        /*** Helper methods ***/

        /* Shift lengths and costs */

        public int DrivingTime(Trip firstTripInternal, Trip lastTripInternal) {
            return DrivingTimes[firstTripInternal.Index, lastTripInternal.Index];
        }

        public float DrivingCost(Trip firstTripInternal, Trip lastTripInternal) {
            return DrivingCosts[firstTripInternal.Index, lastTripInternal.Index];
        }

        public int ShiftLengthWithoutPickup(Trip firstTripInternal, Trip lastTripInternal) {
            return ShiftLengthsWithoutPickup[firstTripInternal.Index, lastTripInternal.Index];
        }

        public int ShiftLengthWithPickup(Trip firstTripInternal, Trip lastTripInternal) {
            return ShiftLengthsWithPickup[firstTripInternal.Index, lastTripInternal.Index];
        }

        public float ShiftCostWithPickup(Trip firstTripInternal, Trip lastTripInternal) {
            return ShiftCostsWithPickup[firstTripInternal.Index, lastTripInternal.Index];
        }

        public int ShiftLengthWithCustomPickup(Trip firstTripInternal, Trip lastTripInternal, Trip parkingTrip) {
            return DrivingTime(firstTripInternal, lastTripInternal) + HomeTravelTimeToStart(firstTripInternal) + instance.CarTravelTime(lastTripInternal, parkingTrip) + HomeTravelTimeToStart(parkingTrip);
        }

        public float ShiftCostWithCustomPickup(Trip firstTripInternal, Trip lastTripInternal, Trip parkingTrip) {
            float drivingCost = DrivingCost(firstTripInternal, lastTripInternal);
            int travelTime = HomeTravelTimeToStart(firstTripInternal) + instance.CarTravelTime(lastTripInternal, parkingTrip) + HomeTravelTimeToStart(parkingTrip);
            float travelCost = GetPayedTravelCost(travelTime);
            return drivingCost + travelCost;
        }

        public (int, float) ShiftLengthAndCostWithPickup(Trip firstTripInternal, Trip lastTripInternal) {
            return (ShiftLengthWithPickup(firstTripInternal, lastTripInternal), ShiftCostWithPickup(firstTripInternal, lastTripInternal));
        }


        /* Rest time */

        public int RestTimeWithPickup(Trip shift1FirstTrip, Trip shift1LastTrip, Trip shift2FirstTrip) {
            return RestTimeWithCustomPickup(shift1LastTrip, shift2FirstTrip, shift1FirstTrip);
        }

        public int RestTimeWithCustomPickup(Trip tripBeforeTravel, Trip tripAfterTravel, Trip parkingTrip) {
            return tripAfterTravel.StartTime - tripBeforeTravel.EndTime - instance.CarTravelTime(tripBeforeTravel, parkingTrip) - HomeTravelTimeToStart(parkingTrip) - HomeTravelTimeToStart(tripAfterTravel);
        }


        /* Travelling */

        public int HomeTravelTimeToStart(Trip trip) {
            return OneWayTravelTimes[trip.FirstStation];
        }

        public int HomeTravelTimeToEnd(Trip trip) {
            return OneWayTravelTimes[trip.LastStation];
        }

        public int TwoWayPayedTravelTimeFromHome(Trip trip) {
            return TwoWayPayedTravelTimes[trip.FirstStation];
        }

        protected abstract int GetPayedTravelTime(int travelTime);

        public float GetPayedTravelCost(int travelTime) {
            return GetPayedTravelTime(travelTime) * TravelSalaryRate;
        }


        /* Contract time */

        public abstract int GetMinContractTimeViolation(int workedTime);
        public abstract int GetMaxContractTimeViolation(int workedTime);

        public int GetTotalContractTimeViolation(int workedTime) {
            return GetMinContractTimeViolation(workedTime) + GetMaxContractTimeViolation(workedTime);
        }


        /* Penalties */

        public float GetContractTimeBasePenalty(int workedTime, bool debugIsNew) {
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
        public readonly bool[,] TrackProficiencies;

        public InternalDriver(int allDriversIndex, int internalIndex, int[] oneWayTravelTimes, int[] twoWayPayedTravelTimes, int[,] drivingTimes, float[,] drivingCosts, int[,] shiftLengthsWithoutPickup, int[,] shiftLengthsWithPickup, float[,] shiftCostsWithPickup, int minWorkedTime, int maxWorkedTime, bool[,] trackProficiencies) : base(allDriversIndex, oneWayTravelTimes, twoWayPayedTravelTimes, Config.InternalDriverTravelSalaryRate, drivingTimes, drivingCosts, shiftLengthsWithoutPickup, shiftLengthsWithPickup, shiftCostsWithPickup) {
            InternalIndex = internalIndex;
            MinContractTime = minWorkedTime;
            MaxContractTime = maxWorkedTime;
            TrackProficiencies = trackProficiencies;
        }

        public override string GetId() {
            return InternalIndex.ToString();
        }

        protected override int GetPayedTravelTime(int travelTime) {
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

        public ExternalDriver(int allDriversIndex, int externalDriverTypeIndex, int indexInType, int[] oneWayTravelTimes, int[] twoWayPayedTravelTimes, int[,] drivingTimes, float[,] drivingCosts, int[,] shiftLengthsWithoutPickup, int[,] shiftLengthsWithPickup, float[,] shiftCostsWithPickup) : base(allDriversIndex, oneWayTravelTimes, twoWayPayedTravelTimes, Config.ExternalDriverTravelSalaryRate, drivingTimes, drivingCosts, shiftLengthsWithoutPickup, shiftLengthsWithPickup, shiftCostsWithPickup) {
            ExternalDriverTypeIndex = externalDriverTypeIndex;
            IndexInType = indexInType;
        }

        public override string GetId() {
            return string.Format("e{0}.{1}", ExternalDriverTypeIndex, IndexInType);
        }

        protected override int GetPayedTravelTime(int travelTime) {
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

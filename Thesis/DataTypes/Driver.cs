using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    abstract class Driver {
        public readonly int AllDriversIndex;
        public readonly int[] OneWayTravelTimes, TwoWayPayedTravelTimes;
        public readonly int[,] ShiftLengths;
        public readonly float[,] ShiftCosts;
        Instance instance;

        public Driver(int allDriversIndex, int[] oneWayTravelTimes, int[] twoWayPayedTravelTimes, int[,] shiftLengths, float[,] shiftCosts) {
            AllDriversIndex = allDriversIndex;
            OneWayTravelTimes = oneWayTravelTimes;
            TwoWayPayedTravelTimes = twoWayPayedTravelTimes;
            ShiftLengths = shiftLengths;
            ShiftCosts = shiftCosts;
        }

        public void SetInstance(Instance instance) {
            this.instance = instance;
        }

        public abstract string GetId();


        /*** Helper methods ***/

        /* Shift lengths and costs */

        public int ShiftLength(Trip firstTripInternal, Trip lastTripInternal) {
            return ShiftLengths[firstTripInternal.Index, lastTripInternal.Index];
        }

        public float ShiftCost(Trip firstTripInternal, Trip lastTripInternal) {
            return ShiftCosts[firstTripInternal.Index, lastTripInternal.Index];
        }

        public (int, float) ShiftLengthAndCost(Trip firstTripInternal, Trip lastTripInternal) {
            return (ShiftLength(firstTripInternal, lastTripInternal), ShiftCost(firstTripInternal, lastTripInternal));
        }


        /* Rest time */

        public int RestTime(Trip shift1FirstTrip, Trip shift1LastTrip, Trip shift2FirstTrip) {
            return shift2FirstTrip.StartTime - shift1LastTrip.EndTime - instance.CarTravelTime(shift1LastTrip, shift1FirstTrip) - OneWayTravelTimeToHome(shift1LastTrip) - OneWayTravelTimeFromHome(shift2FirstTrip);
        }


        /* Travelling */

        public int OneWayTravelTimeFromHome(Trip trip) {
            return OneWayTravelTimes[trip.FirstStation];
        }

        public int OneWayTravelTimeToHome(Trip trip) {
            return OneWayTravelTimes[trip.LastStation];
        }

        public int TwoWayPayedTravelTimeFromHome(Trip trip) {
            return TwoWayPayedTravelTimes[trip.FirstStation];
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

        public InternalDriver(int allDriversIndex, int internalIndex, int[] oneWayTravelTimes, int[] twoWayPayedTravelTimes, int[,] shiftLengths, float[,] shiftCosts, int minWorkedTime, int maxWorkedTime, bool[,] trackProficiencies) : base(allDriversIndex, oneWayTravelTimes, twoWayPayedTravelTimes, shiftLengths, shiftCosts) {
            InternalIndex = internalIndex;
            MinContractTime = minWorkedTime;
            MaxContractTime = maxWorkedTime;
            TrackProficiencies = trackProficiencies;
        }

        public override string GetId() {
            return InternalIndex.ToString();
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

        public ExternalDriver(int allDriversIndex, int externalDriverTypeIndex, int indexInType, int[] oneWayTravelTimes, int[] twoWayPayedTravelTimes, int[,] shiftLengths, float[,] shiftCosts) : base(allDriversIndex, oneWayTravelTimes, twoWayPayedTravelTimes, shiftLengths, shiftCosts) {
            ExternalDriverTypeIndex = externalDriverTypeIndex;
            IndexInType = indexInType;
        }

        public override string GetId() {
            return string.Format("e{0}.{1}", ExternalDriverTypeIndex, IndexInType);
        }

        public override int GetMinContractTimeViolation(int workedTime) {
            return 0;
        }

        public override int GetMaxContractTimeViolation(int workedTime) {
            return 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class Driver {
        public int Index, MinContractTime, MaxContractTime;
        public readonly int[] OneWayTravelTimes, TwoWayPayedTravelTimes;
        public readonly bool[,] TrackProficiencies;
        public readonly int[,] ShiftLengths;
        public readonly float[,] ShiftCosts;

        public Driver(int index, int minWorkedTime, int maxWorkedTime, int[] oneWayTravelTimes, int[] twoWayPayedTravelTimes, bool[,] trackProficiencies, int[,] shiftLengths, float[,] shiftCosts) {
            Index = index;
            MinContractTime = minWorkedTime;
            MaxContractTime = maxWorkedTime;
            TrackProficiencies = trackProficiencies;
            OneWayTravelTimes = oneWayTravelTimes;
            TwoWayPayedTravelTimes = twoWayPayedTravelTimes;
            ShiftLengths = shiftLengths;
            ShiftCosts = shiftCosts;
        }


        /* Helper methods */

        public int ShiftLength(Trip firstTripInternal, Trip lastTripInternal) {
            return ShiftLengths[firstTripInternal.Index, lastTripInternal.Index];
        }
        public float ShiftCost(Trip firstTripInternal, Trip lastTripInternal) {
            return ShiftCosts[firstTripInternal.Index, lastTripInternal.Index];
        }
        public (int, float) ShiftLengthAndCost(Trip firstTripInternal, Trip lastTripInternal) {
            return (ShiftLength(firstTripInternal, lastTripInternal), ShiftCost(firstTripInternal, lastTripInternal));
        }
    }
}

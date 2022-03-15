using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class Driver {
        public int Index;
        public readonly int MinContractTime, MaxContractTime;
        public readonly int[] OneWayTravelTimes, TwoWayPayedTravelTimes;
        public readonly bool[,] TrackProficiencies;
        public readonly int[,] ShiftLengths;
        public readonly float[,] ShiftCosts;
        Instance instance;

        public Driver(int minWorkedTime, int maxWorkedTime, int[] oneWayTravelTimes, int[] twoWayPayedTravelTimes, bool[,] trackProficiencies, int[,] shiftLengths, float[,] shiftCosts) {
            MinContractTime = minWorkedTime;
            MaxContractTime = maxWorkedTime;
            TrackProficiencies = trackProficiencies;
            OneWayTravelTimes = oneWayTravelTimes;
            TwoWayPayedTravelTimes = twoWayPayedTravelTimes;
            ShiftLengths = shiftLengths;
            ShiftCosts = shiftCosts;
        }

        public void SetIndex(int index) {
            Index = index;
        }

        public void SetInstance(Instance instance) {
            this.instance = instance;
        }


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
    }

    //class InternalDriver : Driver {

    //}

    //class ExternalDriver : Driver {

    //}
}

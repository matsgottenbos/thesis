using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class Trip {
        public int Index;
        public readonly List<int> Stations;
        public readonly int FirstStation, LastStation, StartTime, EndTime, Duration;
        public readonly float DrivingCost;
        public readonly List<Trip> Successors;

        public Trip(int index, List<int> stations, int startTime, int endTime, int duration, float drivingCost) {
            Index = index;
            Stations = stations;
            FirstStation = stations[0];
            LastStation = stations[^1];
            Successors = new List<Trip>();
            StartTime = startTime;
            EndTime = endTime;
            Duration = duration;
            DrivingCost = drivingCost;
        }

        public void AddSuccessor(Trip trip) {
            Successors.Add(trip);
        }
    }

    class Driver {
        public int Index, MinContractTime, MaxContractTime;
        public readonly int[] OneWayTravelTimes, TwoWayPayedTravelTimes;
        public readonly bool[,] TrackProficiencies;

        public Driver(int index, int minWorkedTime, int maxWorkedTime, int[] oneWayTravelTimes, int[] twoWayPayedTravelTimes, bool[,] trackProficiencies) {
            Index = index;
            MinContractTime = minWorkedTime;
            MaxContractTime = maxWorkedTime;
            TrackProficiencies = trackProficiencies;
            OneWayTravelTimes = oneWayTravelTimes;
            TwoWayPayedTravelTimes = twoWayPayedTravelTimes;
        }
    }

    class Instance {
        public readonly int[,] TrainTravelTimes, CarTravelTimes;
        public readonly Trip[] Trips;
        public readonly bool[,] TripSuccession;
        public readonly Driver[] Drivers;

        public Instance(int[,] trainTravelTimes, int[,] carTravelTimes, Trip[] trips, bool[,] tripSuccession, Driver[] drivers) {
            TrainTravelTimes = trainTravelTimes;
            CarTravelTimes = carTravelTimes;
            Trips = trips;
            TripSuccession = tripSuccession;
            Drivers = drivers;
        }
    }
}

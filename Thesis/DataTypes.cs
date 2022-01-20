using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class Trip {
        public int Index;
        public readonly List<int> Stations;
        public readonly int FirstStation, LastStation, DayIndex, StartTime, EndTime, Duration;
        public readonly float DrivingCost;
        public readonly List<Trip> Successors;
        public readonly List<Trip> SameDaySuccessors;
        public readonly List<int> SameDaySuccessorsIndices;

        public Trip(int index, List<int> stations, int dayIndex, int startTime, int endTime, int duration, float drivingCost) {
            Index = index;
            Stations = stations;
            FirstStation = stations[0];
            LastStation = stations[^1];
            Successors = new List<Trip>();
            SameDaySuccessors = new List<Trip>();
            SameDaySuccessorsIndices = new List<int>();
            DayIndex = dayIndex;
            StartTime = startTime;
            EndTime = endTime;
            Duration = duration;
            DrivingCost = drivingCost;
        }

        public void AddSuccessor(Trip trip, int? dayIndex) {
            Successors.Add(trip);
            if (trip.DayIndex == DayIndex) {
                SameDaySuccessors.Add(trip);
                SameDaySuccessorsIndices.Add(dayIndex.Value);
            }
        }
    }

    class Driver {
        public int Index, MinContractTime, MaxContractTime;
        public readonly int[] TwoWayPayedTravelTimes;
        public readonly bool[,] TrackProficiencies;

        public Driver(int index, int minWorkedTime, int maxWorkedTime, int[] twoWayPayedTravelTimes, bool[,] trackProficiencies) {
            Index = index;
            MinContractTime = minWorkedTime;
            MaxContractTime = maxWorkedTime;
            TrackProficiencies = trackProficiencies;
            TwoWayPayedTravelTimes = twoWayPayedTravelTimes;
        }
    }

    class Instance {
        public readonly int[,] TrainTravelTimes, CarTravelTimes;
        public readonly Trip[] Trips;
        public readonly bool[,] TripSuccession;
        public readonly Trip[][] TripsPerDay;
        public readonly Driver[] Drivers;

        public Instance(int[,] trainTravelTimes, int[,] carTravelTimes, Trip[] trips, bool[,] tripSuccession, Trip[][] tripsPerDay, Driver[] drivers) {
            TrainTravelTimes = trainTravelTimes;
            CarTravelTimes = carTravelTimes;
            Trips = trips;
            TripSuccession = tripSuccession;
            TripsPerDay = tripsPerDay;
            Drivers = drivers;
        }
    }
}

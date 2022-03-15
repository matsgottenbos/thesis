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
        public readonly List<Trip> Successors;

        public Trip(int index, List<int> stations, int startTime, int endTime, int duration) {
            Index = index;
            Stations = stations;
            FirstStation = stations[0];
            LastStation = stations[^1];
            Successors = new List<Trip>();
            StartTime = startTime;
            EndTime = endTime;
            Duration = duration;
        }

        public void AddSuccessor(Trip trip) {
            Successors.Add(trip);
        }
    }
}

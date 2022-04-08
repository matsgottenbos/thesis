using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class Trip {
        public int Index;
        public readonly int StartStationIndex, EndStationIndex, StartTime, EndTime, Duration;
        public readonly List<Trip> Successors;

        public Trip(int index, int startStationIndex, int endStationIndex, int startTime, int endTime, int duration) {
            Index = index;
            StartStationIndex = startStationIndex;
            EndStationIndex = endStationIndex;
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

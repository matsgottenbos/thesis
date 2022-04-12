using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class Trip {
        public int Index;
        public readonly int StartStationIndex, EndStationIndex, StartTime, EndTime, Duration;
        public readonly string DutyName, ActivityName;
        public readonly List<Trip> Successors;

        public Trip(int index, string dutyName, string activityName, int startStationIndex, int endStationIndex, int startTime, int endTime, int duration) {
            Index = index;
            DutyName = dutyName;
            ActivityName = activityName;
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

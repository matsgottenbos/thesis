using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class Trip {
        public int Index;
        public readonly int StartStationIndex, EndStationIndex, StartTime, EndTime, Duration;
        public int? SharedRouteIndex;
        public readonly string DutyName, ActivityName, ProjectName;
        public readonly List<Trip> Successors;

        public Trip(string dutyName, string activityName, string projectName, int startStationIndex, int endStationIndex, int startTime, int endTime, int duration) {
            Index = -1;
            DutyName = dutyName;
            ProjectName = projectName;
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

        public void SetIndex(int index) {
            Index = index;
        }

        public void SetSharedRouteIndex(int sharedRouteIndex) {
            SharedRouteIndex = sharedRouteIndex;
        }
    }
}

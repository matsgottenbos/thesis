using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class Trip {
        public int Index;
        public readonly int StartStationAddressIndex, EndStationAddressIndex, StartTime, EndTime, Duration;
        public int? SharedRouteIndex;
        public readonly string DutyName, ActivityName, DutyId, ProjectName, StartStationName, EndStationName;
        public readonly List<Trip> Successors;

        public Trip(string dutyName, string activityName, string dutyId, string projectName, string startStationName, string endStationName, int startStationAddressIndex, int endStationAddressIndex, int startTime, int endTime, int duration) {
            Index = -1;
            DutyName = dutyName;
            ProjectName = projectName;
            StartStationName = startStationName;
            EndStationName = endStationName;
            ActivityName = activityName;
            DutyId = dutyId;
            StartStationAddressIndex = startStationAddressIndex;
            EndStationAddressIndex = endStationAddressIndex;
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

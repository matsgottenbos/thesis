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
        public readonly string DutyName, ActivityName, DutyId, ProjectName, StartStationName, EndStationName, DataAssignedCompanyName, DataAssignedEmployeeName;
        public readonly List<Trip> Successors;

        public Trip(RawTrip rawTrip, int index, int startStationAddressIndex, int endStationAddressIndex) {
            Index = index;
            DutyName = rawTrip.DutyName;
            ProjectName = rawTrip.ProjectName;
            StartStationName = rawTrip.StartStationName;
            EndStationName = rawTrip.EndStationName;
            ActivityName = rawTrip.ActivityName;
            DutyId = rawTrip.DutyId;
            StartStationAddressIndex = startStationAddressIndex;
            EndStationAddressIndex = endStationAddressIndex;
            Successors = new List<Trip>();
            StartTime = rawTrip.StartTime;
            EndTime = rawTrip.EndTime;
            Duration = rawTrip.Duration;
            DataAssignedCompanyName = rawTrip.DataAssignedCompanyName;
            DataAssignedEmployeeName = rawTrip.DataAssignedEmployeeName;
        }

        public void AddSuccessor(Trip trip) {
            Successors.Add(trip);
        }

        public void SetSharedRouteIndex(int sharedRouteIndex) {
            SharedRouteIndex = sharedRouteIndex;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class Activity {
        public int Index;
        public readonly int StartStationAddressIndex, EndStationAddressIndex, StartTime, EndTime, Duration;
        public int? SharedRouteIndex;
        public readonly string DutyName, ActivityName, DutyId, ProjectName, StartStationName, EndStationName, DataAssignedCompanyName, DataAssignedEmployeeName;
        public readonly List<Activity> Successors;

        public Activity(RawActivity rawActivity, int index, int startStationAddressIndex, int endStationAddressIndex) {
            Index = index;
            DutyName = rawActivity.DutyName;
            ProjectName = rawActivity.ProjectName;
            StartStationName = rawActivity.StartStationName;
            EndStationName = rawActivity.EndStationName;
            ActivityName = rawActivity.ActivityName;
            DutyId = rawActivity.DutyId;
            StartStationAddressIndex = startStationAddressIndex;
            EndStationAddressIndex = endStationAddressIndex;
            Successors = new List<Activity>();
            StartTime = rawActivity.StartTime;
            EndTime = rawActivity.EndTime;
            Duration = rawActivity.Duration;
            DataAssignedCompanyName = rawActivity.DataAssignedCompanyName;
            DataAssignedEmployeeName = rawActivity.DataAssignedEmployeeName;
        }

        public void AddSuccessor(Activity activity) {
            Successors.Add(activity);
        }

        public void SetSharedRouteIndex(int sharedRouteIndex) {
            SharedRouteIndex = sharedRouteIndex;
        }
    }
}

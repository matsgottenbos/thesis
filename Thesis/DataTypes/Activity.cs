using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class Activity : RawActivity {
        public int Index;
        public readonly int StartStationAddressIndex, EndStationAddressIndex;
        public int? SharedRouteIndex;

        public Activity(RawActivity rawActivity, int index, int startStationAddressIndex, int endStationAddressIndex) : base(rawActivity.DutyName, rawActivity.ActivityName, rawActivity.DutyId, rawActivity.ProjectName, rawActivity.TrainNumber, rawActivity.StartStationName, rawActivity.EndStationName, rawActivity.RequiredCountryQualifications, rawActivity.StartTime, rawActivity.EndTime, rawActivity.DataAssignedCompanyName, rawActivity.DataAssignedEmployeeName) {
            Index = index;
            StartStationAddressIndex = startStationAddressIndex;
            EndStationAddressIndex = endStationAddressIndex;
        }

        public void SetSharedRouteIndex(int sharedRouteIndex) {
            SharedRouteIndex = sharedRouteIndex;
        }
    }
}

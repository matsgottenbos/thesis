using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class RawTrip {
        public readonly int StartTime, EndTime, Duration;
        public readonly string DutyName, ActivityName, DutyId, ProjectName, StartStationName, EndStationName;

        public RawTrip(string dutyName, string activityName, string dutyId, string projectName, string startStationName, string endStationName, int startTime, int endTime, int duration) {
            DutyName = dutyName;
            ProjectName = projectName;
            StartStationName = startStationName;
            EndStationName = endStationName;
            ActivityName = activityName;
            DutyId = dutyId;
            StartTime = startTime;
            EndTime = endTime;
            Duration = duration;
        }
    }
}

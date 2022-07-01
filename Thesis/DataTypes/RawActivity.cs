using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class RawActivity {
        public readonly int StartTime, EndTime, Duration;
        public readonly string DutyName, ActivityName, DutyId, ProjectName, TrainNumber, StartStationName, EndStationName, StartStationCountry, EndStationCountry, DataAssignedCompanyName, DataAssignedEmployeeName;

        public RawActivity(string dutyName, string activityName, string dutyId, string projectName, string trainNumber, string startStationName, string endStationName, string stationStationCountry, string endStationCountry, int startTime, int endTime, int duration, string dataAssignedCompanyName, string dataAssignedEmployeeName) {
            DutyName = dutyName;
            ActivityName = activityName;
            DutyId = dutyId;
            ProjectName = projectName;
            TrainNumber = trainNumber;
            StartStationName = startStationName;
            EndStationName = endStationName;
            StartStationCountry = stationStationCountry;
            EndStationCountry = endStationCountry;
            StartTime = startTime;
            EndTime = endTime;
            Duration = duration;
            DataAssignedCompanyName = dataAssignedCompanyName;
            DataAssignedEmployeeName = dataAssignedEmployeeName;
        }
    }
}

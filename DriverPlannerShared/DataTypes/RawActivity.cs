using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public class RawActivity {
        public readonly int StartTime, EndTime;
        public readonly string DutyName, ActivityType, DutyId, ProjectName, TrainNumber, StartStationName, EndStationName, DataAssignedCompanyName, DataAssignedEmployeeName;
        public readonly string[] RequiredCountryQualifications;
        public readonly RawActivity[] OriginalRawActivities;

        public RawActivity(string dutyName, string activityName, string dutyId, string projectName, string trainNumber, string startStationName, string endStationName, string[] requiredCountryQualifications, int startTime, int endTime, string dataAssignedCompanyName, string dataAssignedEmployeeName, RawActivity[] originalRawActivities = null) {
            DutyName = dutyName;
            ActivityType = activityName;
            DutyId = dutyId;
            ProjectName = projectName;
            TrainNumber = trainNumber;
            StartStationName = startStationName;
            EndStationName = endStationName;
            RequiredCountryQualifications = requiredCountryQualifications;
            StartTime = startTime;
            EndTime = endTime;
            DataAssignedCompanyName = dataAssignedCompanyName;
            DataAssignedEmployeeName = dataAssignedEmployeeName;
            OriginalRawActivities = originalRawActivities;
        }

        /** Test for equality on everything except assigned employee/company in data */
        public static bool AreDetailsEqual(RawActivity a, RawActivity b) {
            return (
                a.StartTime == b.StartTime &&
                a.EndTime == b.EndTime &&
                a.DutyName == b.DutyName &&
                a.ActivityType == b.ActivityType &&
                a.DutyId == b.DutyId &&
                a.ProjectName == b.ProjectName &&
                a.TrainNumber == b.TrainNumber &&
                a.StartStationName == b.StartStationName &&
                a.EndStationName == b.EndStationName &&
                ArrayHelper.AreArraysEqual(a.RequiredCountryQualifications, b.RequiredCountryQualifications)
            );
        }
    }
}

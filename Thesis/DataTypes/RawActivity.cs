﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class RawActivity {
        public readonly int StartTime, EndTime;
        public readonly string DutyName, ActivityName, DutyId, ProjectName, TrainNumber, StartStationName, EndStationName, StartStationCountry, EndStationCountry, DataAssignedCompanyName, DataAssignedEmployeeName;
        public readonly RawActivity[] OriginalRawActivities;

        public RawActivity(string dutyName, string activityName, string dutyId, string projectName, string trainNumber, string startStationName, string endStationName, string startStationCountry, string endStationCountry, int startTime, int endTime, string dataAssignedCompanyName, string dataAssignedEmployeeName, RawActivity[] originalRawActivities = null) {
            DutyName = dutyName;
            ActivityName = activityName;
            DutyId = dutyId;
            ProjectName = projectName;
            TrainNumber = trainNumber;
            StartStationName = startStationName;
            EndStationName = endStationName;
            StartStationCountry = startStationCountry;
            EndStationCountry = endStationCountry;
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
                a.ActivityName == b.ActivityName &&
                a.DutyId == b.DutyId &&
                a.ProjectName == b.ProjectName &&
                a.TrainNumber == b.TrainNumber &&
                a.StartStationName == b.StartStationName &&
                a.EndStationName == b.EndStationName &&
                a.StartStationCountry == b.StartStationCountry &&
                a.EndStationCountry == b.EndStationCountry
            );
        }
    }
}

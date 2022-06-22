using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Thesis {
    static class ExcelDataImporter {
        public static Instance Import(XorShiftRandom rand) {
            XSSFWorkbook testDataBook = ExcelHelper.ReadExcelFile(Path.Combine(AppConfig.InputFolder, "testData.xlsx"));
            ExcelSheet activitiesSheet = new ExcelSheet("DutyActivities", testDataBook);

            RawTrip[] rawTrips = ParseRawTrips(activitiesSheet, DataConfig.ExcelPlanningStartDate, DataConfig.ExcelPlanningNextDate);
            return new Instance(rand, rawTrips);
        }

        static RawTrip[] ParseRawTrips(ExcelSheet activitiesSheet, DateTime planningStartDate, DateTime planningNextDate) {
            List<RawTrip> rawTrips = new List<RawTrip>();
            activitiesSheet.ForEachRow(activityRow => {
                // Skip if non-included order owner
                string orderOwner = activitiesSheet.GetStringValue(activityRow, "RailwayUndertaking");
                if (orderOwner == null || !DataConfig.ExcelIncludedRailwayUndertakings.Contains(orderOwner)) return;

                // Get duty, activity and project name name
                string dutyName = activitiesSheet.GetStringValue(activityRow, "DutyNo");
                string activityName = activitiesSheet.GetStringValue(activityRow, "ActivityDescriptionEN");
                string dutyId = activitiesSheet.GetStringValue(activityRow, "DutyID");
                string projectName = activitiesSheet.GetStringValue(activityRow, "Project");

                // Filter to configured activity descriptions
                if (!ParseHelper.DataStringInList(activityName, DataConfig.ExcelIncludedActivityDescriptions)) return;

                // Get start and end stations
                string startStationDataName = activitiesSheet.GetStringValue(activityRow, "OriginLocationName");
                if (startStationDataName == null) return; // Skip row if start location is empty

                string endStationDataName = activitiesSheet.GetStringValue(activityRow, "DestinationLocationName");
                if (endStationDataName == null) return; // Skip row if end location is empty

                // Get start and end time
                DateTime? startTimeRaw = activitiesSheet.GetDateValue(activityRow, "PlannedStart");
                if (startTimeRaw == null || startTimeRaw < planningStartDate || startTimeRaw > planningNextDate) return; // Skip trips outside planning timeframe
                int startTime = (int)Math.Round((startTimeRaw - planningStartDate).Value.TotalMinutes);
                DateTime? endTimeRaw = activitiesSheet.GetDateValue(activityRow, "PlannedEnd");
                if (endTimeRaw == null) return; // Skip row if required values are empty
                int endTime = (int)Math.Round((endTimeRaw - planningStartDate).Value.TotalMinutes);
                int duration = endTime - startTime;

                // Temp: skip trips longer than max shift length
                if (duration > RulesConfig.NormalShiftMaxLengthWithoutTravel) return;

                // Get company and employee assigned in data
                string assignedCompanyName = activitiesSheet.GetStringValue(activityRow, "EmployeeWorksFor");
                string assignedEmployeeName = activitiesSheet.GetStringValue(activityRow, "EmployeeName");

                rawTrips.Add(new RawTrip(dutyName, activityName, dutyId, projectName, startStationDataName, endStationDataName, startTime, endTime, duration, assignedCompanyName, assignedEmployeeName));
            });

            if (rawTrips.Count == 0) {
                throw new Exception("No trips found in timeframe");
            }

            return rawTrips.ToArray();
        }
    }
}

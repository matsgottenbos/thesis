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
        public static Instance Import(DateTime planningStartDate, DateTime planningEndDate) {
            XSSFWorkbook testDataBook = ExcelHelper.ReadExcelFile(Path.Combine(DevConfig.InputFolder, "testData.xlsx"));
            ExcelSheet activitiesSheet = new ExcelSheet("DutyActivities", testDataBook);

            RawActivity[] rawActivities = ParseRawActivities(activitiesSheet, planningStartDate, planningEndDate);
            return new Instance(rawActivities);
        }

        static RawActivity[] ParseRawActivities(ExcelSheet activitiesSheet, DateTime planningStartDate, DateTime planningNextDate) {
            List<RawActivity> rawActivities = new List<RawActivity>();
            activitiesSheet.ForEachRow(activityRow => {
                // Skip if non-included order owner
                string orderOwner = activitiesSheet.GetStringValue(activityRow, "RailwayUndertaking");
                if (orderOwner == null || !AppConfig.IncludedRailwayUndertakings.Contains(orderOwner)) return;

                // Get duty, activity and project name name
                string dutyName = activitiesSheet.GetStringValue(activityRow, "DutyNo");
                string activityType = activitiesSheet.GetStringValue(activityRow, "ActivityDescriptionEN");
                string dutyId = activitiesSheet.GetStringValue(activityRow, "DutyID");
                string projectName = activitiesSheet.GetStringValue(activityRow, "Project") ?? "";
                string trainNumber = activitiesSheet.GetStringValue(activityRow, "TrainNo") ?? "";

                // Filter to configured activity descriptions
                if (!ParseHelper.DataStringInList(activityType, AppConfig.IncludedActivityTypes)) return;

                // Get start and end stations
                string startStationDataName = activitiesSheet.GetStringValue(activityRow, "OriginLocationName");
                string endStationDataName = activitiesSheet.GetStringValue(activityRow, "DestinationLocationName");
                string startStationCountry = activitiesSheet.GetStringValue(activityRow, "OriginCountry");
                string endStationCountry = activitiesSheet.GetStringValue(activityRow, "DestinationCountry");
                if (startStationDataName == null || endStationDataName == null) return;

                // Get required country qualifications
                string[] requiredCountryQualifications;
                if (startStationCountry == endStationCountry) requiredCountryQualifications = new string[] { startStationCountry };
                else requiredCountryQualifications = new string[] { startStationCountry, endStationCountry };

                // Get start and end time
                DateTime? startTimeRaw = activitiesSheet.GetDateValue(activityRow, "PlannedStart");
                if (startTimeRaw == null || startTimeRaw < planningStartDate || startTimeRaw > planningNextDate) return; // Skip activities outside planning timeframe
                int startTime = (int)Math.Round((startTimeRaw - planningStartDate).Value.TotalMinutes);
                DateTime? endTimeRaw = activitiesSheet.GetDateValue(activityRow, "PlannedEnd");
                if (endTimeRaw == null) return; // Skip row if required values are empty
                int endTime = (int)Math.Round((endTimeRaw - planningStartDate).Value.TotalMinutes);

                // Skip activities longer than max shift length
                if (endTime - startTime > RulesConfig.MaxMainNightShiftLength) return;

                // Get company and employee assigned in data
                string assignedCompanyName = activitiesSheet.GetStringValue(activityRow, "EmployeeWorksFor");
                string assignedEmployeeName = activitiesSheet.GetStringValue(activityRow, "EmployeeName");


                // Temp; TODO: make cleaner
                string[] stationsToSkip = new string[] { "Stuttgart Hbf", "Stuttgart Hafen", "Germersheim", "Bietigheim-Bissingen" };
                if (stationsToSkip.Contains(startStationDataName) || stationsToSkip.Contains(endStationDataName)) return;
                if (endTime - startTime > 6 * 60) return;



                rawActivities.Add(new RawActivity(dutyName, activityType, dutyId, projectName, trainNumber, startStationDataName, endStationDataName, requiredCountryQualifications, startTime, endTime, assignedCompanyName, assignedEmployeeName));
            });

            if (rawActivities.Count == 0) {
                throw new Exception("No activities found in timeframe");
            }

            return rawActivities.ToArray();
        }
    }
}

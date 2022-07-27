/*
 * Import data from an Excel file
*/

using NPOI.XSSF.UserModel;

namespace DriverPlannerShared {
    public static class ExcelDataImporter {
        public static Instance Import() {
            XSSFWorkbook testDataBook = ExcelHelper.ReadExcelFile(Path.Combine(DevConfig.InputFolder, "testData.xlsx"));
            ExcelSheet activitiesSheet = new ExcelSheet("DutyActivities", testDataBook);

            RawActivity[] rawActivities = ParseRawActivities(activitiesSheet);
            return new Instance(rawActivities);
        }

        static RawActivity[] ParseRawActivities(ExcelSheet activitiesSheet) {
            List<RawActivity> rawActivities = new List<RawActivity>();
            activitiesSheet.ForEachRow(activityRow => {
                // Skip if non-included railway undertaking
                string railwayUndertaking = activitiesSheet.GetStringValue(activityRow, "RailwayUndertaking");
                if (railwayUndertaking == null || !AppConfig.IncludedRailwayUndertakings.Contains(railwayUndertaking)) return;

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
                if (startStationDataName == null || endStationDataName == null || startStationCountry == null || endStationCountry == null) return;

                // Get required country qualifications
                string[] requiredCountryQualifications;
                if (startStationCountry == endStationCountry) requiredCountryQualifications = new string[] { startStationCountry };
                else requiredCountryQualifications = new string[] { startStationCountry, endStationCountry };

                // Get start and end time
                DateTime? startTimeRaw = activitiesSheet.GetDateValue(activityRow, "PlannedStart");
                if (startTimeRaw == null || startTimeRaw < AppConfig.PlanningStartDate || startTimeRaw > AppConfig.PlanningEndDate) return; // Skip activities outside planning timeframe
                int startTime = (int)Math.Round((startTimeRaw - AppConfig.PlanningStartDate).Value.TotalMinutes);
                DateTime? endTimeRaw = activitiesSheet.GetDateValue(activityRow, "PlannedEnd");
                if (endTimeRaw == null) return; // Skip row if required values are empty
                int endTime = (int)Math.Round((endTimeRaw - AppConfig.PlanningStartDate).Value.TotalMinutes);

                // Skip activities longer than max shift length
                if (endTime - startTime > RulesConfig.MaxMainNightShiftLength) return;

                // Get company and employee assigned in data
                string assignedCompanyName = activitiesSheet.GetStringValue(activityRow, "EmployeeWorksFor");
                string assignedEmployeeName = activitiesSheet.GetStringValue(activityRow, "EmployeeName");

                rawActivities.Add(new RawActivity(dutyName, activityType, dutyId, projectName, trainNumber, startStationDataName, endStationDataName, requiredCountryQualifications, startTime, endTime, assignedCompanyName, assignedEmployeeName));
            });

            return rawActivities.ToArray();
        }
    }
}

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
            ExcelSheet dutiesSheet = new ExcelSheet("DutyActivities", testDataBook);

            RawTrip[] rawTrips = ParseRawTrips(dutiesSheet, DataConfig.ExcelPlanningStartDate, DataConfig.ExcelPlanningNextDate);
            return new Instance(rand, rawTrips);
        }

        static RawTrip[] ParseRawTrips(ExcelSheet dutiesSheet, DateTime planningStartDate, DateTime planningNextDate) {
            List<RawTrip> rawTrips = new List<RawTrip>();
            dutiesSheet.ForEachRow(dutyRow => {
                // Skip if non-included order owner
                string orderOwner = dutiesSheet.GetStringValue(dutyRow, "RailwayUndertaking");
                if (orderOwner == null || !DataConfig.ExcelIncludedRailwayUndertakings.Contains(orderOwner)) return;

                // Get duty, activity and project name name
                string dutyName = dutiesSheet.GetStringValue(dutyRow, "DutyNo") ?? "";
                string activityName = dutiesSheet.GetStringValue(dutyRow, "ActivityDescriptionEN") ?? "";
                string dutyId = dutiesSheet.GetStringValue(dutyRow, "DutyID") ?? "";
                string projectName = dutiesSheet.GetStringValue(dutyRow, "Project") ?? "";

                // Filter to configured activity descriptions
                if (!ParseHelper.DataStringInList(activityName, DataConfig.ExcelIncludedActivityDescriptions)) return;

                // Get start and end stations
                string startStationDataName = dutiesSheet.GetStringValue(dutyRow, "OriginLocationName") ?? "";
                if (startStationDataName == "") return; // Skip row if start location is empty

                string endStationDataName = dutiesSheet.GetStringValue(dutyRow, "DestinationLocationName") ?? "";
                if (endStationDataName == "") return; // Skip row if end location is empty

                // Get start and end time
                DateTime? startTimeRaw = dutiesSheet.GetDateValue(dutyRow, "PlannedStart");
                if (startTimeRaw == null || startTimeRaw < planningStartDate || startTimeRaw > planningNextDate) return; // Skip trips outside planning timeframe
                int startTime = (int)Math.Round((startTimeRaw - planningStartDate).Value.TotalMinutes);
                DateTime? endTimeRaw = dutiesSheet.GetDateValue(dutyRow, "PlannedEnd");
                if (endTimeRaw == null) return; // Skip row if required values are empty
                int endTime = (int)Math.Round((endTimeRaw - planningStartDate).Value.TotalMinutes);
                int duration = endTime - startTime;

                // Temp: skip trips longer than max shift length
                if (duration > RulesConfig.NormalShiftMaxLengthWithoutTravel) return;

                rawTrips.Add(new RawTrip(dutyName, activityName, dutyId, projectName, startStationDataName, endStationDataName, startTime, endTime, duration));
            });

            if (rawTrips.Count == 0) {
                throw new Exception("No trips found in timeframe");
            }

            return rawTrips.ToArray();
        }
    }
}

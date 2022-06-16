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
            // Import Excel sheet
            FileStream fileStream = new FileStream(Path.Combine(AppConfig.DataFolder, "data.xlsx"), FileMode.Open, FileAccess.Read);
            XSSFWorkbook excelBook = new XSSFWorkbook(fileStream);
            fileStream.Close();

            DataTable dutiesTable = new DataTable("DutyActivities", excelBook);
            DataTable employeesTable = new DataTable("Employees", excelBook);
            DataTable routeKnowledgeTable = new DataTable("RouteKnowledge", excelBook);

            // Parse trips and station codes
            (Trip[] rawTrips, string[] stationCodes, int timeframeLength) = ParseRawTripsAndStationCodes(dutiesTable, DataConfig.ExcelPlanningStartDate, DataConfig.ExcelPlanningNextDate);
            int stationCount = stationCodes.Length;

            // Estimate or generate train travel times, and generate car travel times
            int[,] trainTravelTimes = EstimateOrGenerateTrainTravelTimes(rawTrips, stationCodes, rand);
            int[,] carTravelTimes = DataGenerator.GenerateCarTravelTimes(stationCount, trainTravelTimes, rand);

            // Parse internal driver names and track proficiencies
            (string[] internalDriverNames, bool[] internalDriverIsInternational) = ParseInternalDriverNamesAndInternationalStatus(employeesTable);
            //bool[][,] internalDriverTrackProficiencies = ParseInternalDriverTrackProficiencies(routeKnowledgeTable, internalDriverNames, stationCodes);
            int internalDriverCount = internalDriverNames.Length;

            // Generate remaining driver data; TODO: use real data
            int[][] internalDriversHomeTravelTimes = DataGenerator.GenerateInternalDriverHomeTravelTimes(internalDriverCount, stationCount, rand);
            bool[][,] internalDriverTrackProficiencies = DataGenerator.GenerateInternalDriverTrackProficiencies(internalDriverCount, stationCount, rand);
            int[][] externalDriversHomeTravelTimes = DataGenerator.GenerateExternalDriverHomeTravelTimes(DataConfig.ExternalDriverTypes.Length, stationCount, rand);

            // TODO: get internal driver contract times from settings
            int[] internalDriversContractTimes = new int[internalDriverCount];
            for (int i = 0; i < internalDriverCount; i++) internalDriversContractTimes[i] = DataConfig.ExcelInternalDriverContractTime;

            return new Instance(rand, rawTrips, stationCodes, carTravelTimes, internalDriverNames, internalDriversHomeTravelTimes, internalDriverTrackProficiencies, internalDriversContractTimes, internalDriverIsInternational, externalDriversHomeTravelTimes);
        }

        static (Trip[], string[], int) ParseRawTripsAndStationCodes(DataTable dutiesTable, DateTime planningStartDate, DateTime planningNextDate) {
            List<Trip> rawTripList = new List<Trip>();
            List<string> stationNameList = new List<string>();
            int timeframeLength = 0;
            dutiesTable.ForEachRow(dutyRow => {
                // Skip if non-included order owner
                string orderOwner = dutyRow.GetCell(dutiesTable.GetColumnIndex("RailwayUndertaking"))?.StringCellValue;
                if (orderOwner == null || !DataConfig.ExcelIncludedRailwayUndertakings.Contains(orderOwner)) return;

                // Get duty, activity and project name name
                string dutyName = dutyRow.GetCell(dutiesTable.GetColumnIndex("DutyNo"))?.StringCellValue ?? "";
                string activityName = dutyRow.GetCell(dutiesTable.GetColumnIndex("ActivityDescriptionEN"))?.StringCellValue ?? "";
                string dutyId = dutyRow.GetCell(dutiesTable.GetColumnIndex("DutyID"))?.StringCellValue ?? "";
                string projectName = dutyRow.GetCell(dutiesTable.GetColumnIndex("Project"))?.StringCellValue ?? "";

                // Filter to configured activity descriptions
                if (!ParseHelper.DataStringInList(activityName, DataConfig.ExcelIncludedActivityDescriptions)) return;

                // Get start and end stations
                string startStationName = dutyRow.GetCell(dutiesTable.GetColumnIndex("OriginLocationName"))?.StringCellValue ?? "";
                if (startStationName == "") return; // Skip row if start location is empty

                int startStationIndex = GetOrAddCodeIndex(startStationName, stationNameList);
                string endStationName = dutyRow.GetCell(dutiesTable.GetColumnIndex("DestinationLocationName"))?.StringCellValue ?? "";
                if (endStationName == "") return; // Skip row if end location is empty

                int endStationIndex = GetOrAddCodeIndex(endStationName, stationNameList);

                // Get start and end time
                DateTime? startTimeRaw = dutyRow.GetCell(dutiesTable.GetColumnIndex("PlannedStart"))?.DateCellValue;
                if (startTimeRaw == null || startTimeRaw < planningStartDate || startTimeRaw > planningNextDate) return; // Skip trips outside planning timeframe
                int startTime = (int)Math.Round((startTimeRaw - planningStartDate).Value.TotalMinutes);
                DateTime? endTimeRaw = dutyRow.GetCell(dutiesTable.GetColumnIndex("PlannedEnd"))?.DateCellValue;
                if (endTimeRaw == null) return; // Skip row if required values are empty
                int endTime = (int)Math.Round((endTimeRaw - planningStartDate).Value.TotalMinutes);
                int duration = endTime - startTime;

                // Set the timeframe length to the last end time of all trips
                timeframeLength = Math.Max(timeframeLength, endTime);

                // Temp: skip trips longer than max shift length
                if (duration > RulesConfig.NormalShiftMaxLengthWithoutTravel) return;

                rawTripList.Add(new Trip(dutyName, activityName, dutyId, projectName, startStationIndex, endStationIndex, startTime, endTime, duration));
            });
            Trip[] rawTrips = rawTripList.ToArray();
            string[] stationCodes = stationNameList.ToArray();

            if (rawTrips.Length == 0) {
                throw new Exception("No trips found in timeframe");
            }

            return (rawTrips, stationCodes, timeframeLength);
        }

        static int[,] EstimateOrGenerateTrainTravelTimes(Trip[] rawTrips, string[] stationCodes, XorShiftRandom rand) {
            // Extract train travel times
            List<int>[,] trainTravelTimesAll = new List<int>[stationCodes.Length, stationCodes.Length];
            for (int rawTripIndex = 0; rawTripIndex < rawTrips.Length; rawTripIndex++) {
                Trip trip = rawTrips[rawTripIndex];
                if (trainTravelTimesAll[trip.StartStationIndex, trip.EndStationIndex] == null) trainTravelTimesAll[trip.StartStationIndex, trip.EndStationIndex] = new List<int>();
                trainTravelTimesAll[trip.StartStationIndex, trip.EndStationIndex].Add(trip.Duration);
            }

            // Estimate or generate train travel times
            int[,] trainTravelTimes = new int[stationCodes.Length, stationCodes.Length];
            for (int i = 0; i < stationCodes.Length; i++) {
                for (int j = 0; j < stationCodes.Length; j++) {
                    if (trainTravelTimesAll[i, j] == null) {
                        // No real travel times available, so generate them for now; TODO: use API to determine travel times
                        trainTravelTimes[i, j] = (int)(rand.NextDouble() * (DataConfig.GenMaxStationTravelTime - DataConfig.GenMinStationTravelTime) + DataConfig.GenMinStationTravelTime);
                    } else {
                        // Use average of real travel times as estimate
                        trainTravelTimes[i, j] = (int)trainTravelTimesAll[i, j].Average();
                    }
                }
            }
            return trainTravelTimes;
        }

        static (string[], bool[]) ParseInternalDriverNamesAndInternationalStatus(DataTable employeesTable) {
            List<string> internalDriverNames = new List<string>();
            List<bool> internalDriverIsInternational = new List<bool>();
            employeesTable.ForEachRow(employeeRow => {
                // Only include configures companies
                string driverCompany = employeeRow.GetCell(employeesTable.GetColumnIndex("PrimaryCompany")).StringCellValue;
                if (!ParseHelper.DataStringInList(driverCompany, DataConfig.ExcelIncludedRailwayUndertakings)) return;

                // Only include configures job titles
                string driverJobTitle = employeeRow.GetCell(employeesTable.GetColumnIndex("PrimaryJobTitle")).StringCellValue;
                bool isInternationalDriver;
                if (ParseHelper.DataStringInList(driverJobTitle, DataConfig.ExcelIncludedJobTitlesNational)) {
                    // National driver
                    isInternationalDriver = true;
                } else if (ParseHelper.DataStringInList(driverJobTitle, DataConfig.ExcelIncludedJobTitlesInternational)) {
                    // International driver
                    isInternationalDriver = false;
                } else {
                    // Non-configured job title, ignore driver
                    return;
                }

                string driverName = employeeRow.GetCell(employeesTable.GetColumnIndex("FullName")).StringCellValue;
                internalDriverNames.Add(driverName);
                internalDriverIsInternational.Add(isInternationalDriver);
            });
            return (internalDriverNames.ToArray(), internalDriverIsInternational.ToArray());
        }

        static bool[][,] ParseInternalDriverTrackProficiencies(DataTable routeKnowledgeTable, string[] internalDriverNames, string[] stationNames) {
            bool[][,] internalDriverProficiencies = new bool[internalDriverNames.Length][,];
            for (int driverIndex = 0; driverIndex < internalDriverNames.Length; driverIndex++) {
                internalDriverProficiencies[driverIndex] = new bool[stationNames.Length, stationNames.Length];

                // Everyone is proficient when staying in the same location
                for (int stationIndex = 0; stationIndex < stationNames.Length; stationIndex++) {
                    internalDriverProficiencies[driverIndex][stationIndex, stationIndex] = true;
                }
            }

            routeKnowledgeTable.ForEachRow(routeKnowledgeRow => {
                // Get station indices
                string station1Name = routeKnowledgeRow.GetCell(routeKnowledgeTable.GetColumnIndex("OriginLocationName")).StringCellValue;
                int station1Index = Array.IndexOf(stationNames, station1Name);
                string station2Name = routeKnowledgeRow.GetCell(routeKnowledgeTable.GetColumnIndex("DestinationLocationName")).StringCellValue;
                int station2Index = Array.IndexOf(stationNames, station2Name);
                if (station1Index == -1 || station2Index == -1) return;

                // Get driver index
                string driverName = routeKnowledgeRow.GetCell(routeKnowledgeTable.GetColumnIndex("EmployeeName")).StringCellValue;
                int driverIndex = Array.IndexOf(internalDriverNames, driverName);
                if (driverIndex == -1) return;

                internalDriverProficiencies[driverIndex][station1Index, station2Index] = true;
                internalDriverProficiencies[driverIndex][station2Index, station1Index] = true;
            });

            return internalDriverProficiencies;
        }

        static int GetOrAddCodeIndex(string code, List<string> codes) {
            if (codes.Contains(code)) {
                // Existing code
                return codes.FindIndex(searchCode => searchCode == code);
            }

            // New code
            codes.Add(code);
            return codes.Count - 1;
        }
    }

    class DataTable {
        readonly ISheet sheet;
        readonly Dictionary<string, int> columnNamesToIndices;

        public DataTable(string sheetName, XSSFWorkbook excelBook) {
            sheet = excelBook.GetSheet(sheetName);
            if (sheet == null) throw new Exception($"Unknown sheet `{sheetName}`");

            // Parse column headers
            IRow headerRow = sheet.GetRow(0);
            columnNamesToIndices = new Dictionary<string, int>();
            for (int colIndex = 0; colIndex < headerRow.LastCellNum; colIndex++) {
                string headerName = headerRow.GetCell(colIndex).StringCellValue;
                columnNamesToIndices.Add(headerName, colIndex);
            }
        }

        public int GetColumnIndex(string columnName) {
            return columnNamesToIndices[columnName];
        }

        public void ForEachRow(Action<IRow> rowFunc) {
            for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++) {
                IRow row = sheet.GetRow(rowIndex);
                if (row == null) continue; // Skip empty rows

                rowFunc(row);
            }
        }
    }
}

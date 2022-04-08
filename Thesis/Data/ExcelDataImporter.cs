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
        public static Instance Import(Random rand) {
            // Import Excel sheet
            FileStream fileStream = new FileStream(Path.Combine(Config.DataFolder, "data.xlsx"), FileMode.Open, FileAccess.Read);
            XSSFWorkbook excelBook = new XSSFWorkbook(fileStream);
            fileStream.Close();

            DataTable dutiesTable = new DataTable("DutyActivities", excelBook);
            DataTable employeesTable = new DataTable("Employees", excelBook);
            //DataTable certificatesTable = new DataTable("Certificates", excelBook);
            // TODO: use RouteKnowledge

            // Parse trips and station codes
            (Trip[] rawTrips, string[] stationCodes) = ParseRawTripsAndStationCodes(dutiesTable, Config.ExcelPlanningStartDate, Config.ExcelPlanningNextDate);
            int stationCount = stationCodes.Length;

            // Estimate or generate train travel times, and generate car travel times
            int[,] trainTravelTimes = EstimateOrGenerateTrainTravelTimes(rawTrips, stationCodes, rand);
            int[,] carTravelTimes = DataGenerator.GenerateCarTravelTimes(stationCount, trainTravelTimes, rand);

            // Parse internal driver names and track proficiencies
            string[] internalDriverNames = ParseInternalDriverNames(employeesTable);
            //bool[][,] internalDriverTrackProficiencies = ParseInternalDriverTrackProficiencies(certificatesTable, internalDriverNames, stationCodes);
            int internalDriverCount = internalDriverNames.Length;

            // Generate remaining driver data; TODO: use real data
            int[][] internalDriversHomeTravelTimes = DataGenerator.GenerateInternalDriverHomeTravelTimes(internalDriverCount, stationCount, rand);
            bool[][,] internalDriverTrackProficiencies = DataGenerator.GenerateInternalDriverTrackProficiencies(internalDriverCount, stationCount, rand);
            int[] externalDriverCounts = DataGenerator.GenerateExternalDriverCounts(Config.ExcelExternalDriverTypeCount, Config.ExcelExternalDriverMinCountPerType, Config.ExcelExternalDriverMaxCountPerType, rand);
            int[][] externalDriversHomeTravelTimes = DataGenerator.GenerateExternalDriverHomeTravelTimes(Config.ExcelExternalDriverTypeCount, stationCount, rand);

            return new Instance(rawTrips, carTravelTimes, internalDriverNames, internalDriversHomeTravelTimes, internalDriverTrackProficiencies, Config.ExcelInternalDriverContractTime, externalDriverCounts, externalDriversHomeTravelTimes);
        }

        static (Trip[], string[] stationCodes) ParseRawTripsAndStationCodes(DataTable dutiesTable, DateTime planningStartDate, DateTime planningNextDate) {
            List<Trip> rawTripList = new List<Trip>();
            List<string> stationCodesList = new List<string>();
            dutiesTable.ForEachRow(dutyRow => {
                //string activityType = dutyRow.GetCell(dutiesTable.GetColumnIndex("ActivityDescriptionEN")).StringCellValue;
                //if (activityType != "Drive train") return; // Skip non-driving activities

                // Get start and end stations
                string startStationCode = dutyRow.GetCell(dutiesTable.GetColumnIndex("OriginLocationCode")).StringCellValue;
                int startStationIndex = GetOrAddCodeIndex(startStationCode, stationCodesList);
                string endStationCode = dutyRow.GetCell(dutiesTable.GetColumnIndex("DestinationLocationCode")).StringCellValue;
                int endStationIndex = GetOrAddCodeIndex(endStationCode, stationCodesList);
                //if (startStationIndex == endStationIndex) return; // Skip non-driving activities

                // Get start and end time
                DateTime startTimeRaw = dutyRow.GetCell(dutiesTable.GetColumnIndex("PlannedStart")).DateCellValue;
                if (startTimeRaw < planningStartDate || startTimeRaw > planningNextDate) return; // Skip trips outside planning timeframe
                int startTime = (int)Math.Round((startTimeRaw - planningStartDate).TotalMinutes);
                DateTime endTimeRaw = dutyRow.GetCell(dutiesTable.GetColumnIndex("PlannedEnd")).DateCellValue;
                int endTime = (int)Math.Round((endTimeRaw - planningStartDate).TotalMinutes);
                int duration = endTime - startTime;

                // Temp: skip trips longer than max shift length
                if (duration > Config.MaxShiftLengthWithoutTravel) return;

                rawTripList.Add(new Trip(-1, startStationIndex, endStationIndex, startTime, endTime, duration));
            });
            Trip[] rawTrips = rawTripList.ToArray();
            string[] stationCodes = stationCodesList.ToArray();
            return (rawTrips, stationCodes);
        }

        static int[,] EstimateOrGenerateTrainTravelTimes(Trip[] rawTrips, string[] stationCodes, Random rand) {
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
                        trainTravelTimes[i, j] = (int)(rand.NextDouble() * (Config.GenMaxStationTravelTime - Config.GenMinStationTravelTime) + Config.GenMinStationTravelTime);
                    } else {
                        // Use average of real travel times as estimate
                        trainTravelTimes[i, j] = (int)trainTravelTimesAll[i, j].Average();
                    }
                }
            }
            return trainTravelTimes;
        }

        static string[] ParseInternalDriverNames(DataTable employeesTable) {
            List<string> internalDriverNames = new List<string>();
            employeesTable.ForEachRow(employeeRow => {
                string driverJobTitle = employeeRow.GetCell(employeesTable.GetColumnIndex("PrimaryJobTitle")).StringCellValue;
                if (driverJobTitle != "Machinist VB nationaal" && driverJobTitle != "Machinist VB Internationaal NL-D") return;

                string driverName = employeeRow.GetCell(employeesTable.GetColumnIndex("FullName")).StringCellValue;
                internalDriverNames.Add(driverName);
            });
            return internalDriverNames.ToArray();
        }

        static bool[][,] ParseInternalDriverTrackProficiencies(DataTable certificatesTable, string[] internalDriverNames, string[] stationCodes) {
            bool[][,] internalDriverProficiencies = new bool[internalDriverNames.Length][,];
            for (int i = 0; i < internalDriverNames.Length; i++) {
                internalDriverProficiencies[i] = new bool[stationCodes.Length, stationCodes.Length];
            }

            certificatesTable.ForEachRow(certificateTable => {
                // Only look at route knowledge certificate
                string certificateType = certificateTable.GetCell(certificatesTable.GetColumnIndex("CertificateTypeNameEN")).StringCellValue;
                if (certificateType != "Route knowledge") return;

                string certificateName = certificateTable.GetCell(certificatesTable.GetColumnIndex("CertificateName")).StringCellValue;
                Match certificateRegexMatch = Regex.Match(certificateName, @"^(?:\d+ )?(\w+) - (\w+)$");
                if (!certificateRegexMatch.Success) return;

                // Get station indices
                string station1Code = certificateRegexMatch.Groups.Values.ToArray()[1].Value;
                int station1Index = Array.IndexOf(stationCodes, station1Code);
                string station2Code = certificateRegexMatch.Groups.Values.ToArray()[2].Value;
                int station2Index = Array.IndexOf(stationCodes, station2Code);
                if (station1Index == -1 || station2Index == -1) return;

                // Get driver index
                string driverName = certificateTable.GetCell(certificatesTable.GetColumnIndex("EmployeeName")).StringCellValue;
                int driverIndex = Array.IndexOf(internalDriverNames, driverName);
                if (driverIndex == -1) return;

                internalDriverProficiencies[driverIndex][station1Index, station2Index] = true;
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

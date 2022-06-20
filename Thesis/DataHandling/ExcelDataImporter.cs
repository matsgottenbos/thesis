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
            // Import Excel sheets
            XSSFWorkbook addressesBook = ExcelHelper.ReadExcelFile(Path.Combine(AppConfig.DataFolder, "addresses.xlsx"));
            XSSFWorkbook dataBook = ExcelHelper.ReadExcelFile(Path.Combine(AppConfig.DataFolder, "data.xlsx"));

            ExcelSheet linkingStationNamesSheet = new ExcelSheet("Linking station names", addressesBook);
            ExcelSheet dutiesSheet = new ExcelSheet("DutyActivities", dataBook);
            ExcelSheet employeesSheet = new ExcelSheet("Employees", dataBook);
            ExcelSheet routeKnowledgeSheet = new ExcelSheet("RouteKnowledge", dataBook);

            // Get car travel times between stations
            (int[,] carTravelTimes, _, string[] stationNames) = TravelInfoHandler.ImportTravelInfo();
            int stationCount = stationNames.Length;

            // Get station name conversion dictionary
            Dictionary<string, int> stationDataNameToAddressIndex = GetStationDataNameToAddressIndexDict(linkingStationNamesSheet, stationNames);

            // Parse trips and station codes
            Trip[] rawTrips = ParseRawTrips(dutiesSheet, DataConfig.ExcelPlanningStartDate, DataConfig.ExcelPlanningNextDate, stationDataNameToAddressIndex);

            // Parse internal driver names and track proficiencies
            (string[] internalDriverNames, bool[] internalDriverIsInternational) = ParseInternalDriverNamesAndInternationalStatus(employeesSheet);
            //bool[][,] internalDriverTrackProficiencies = ParseInternalDriverTrackProficiencies(routeKnowledgeSheet, internalDriverNames, stationNames);
            int internalDriverCount = internalDriverNames.Length;

            // Generate remaining driver data; TODO: use real data
            int[][] internalDriversHomeTravelTimes = DataGenerator.GenerateInternalDriverHomeTravelTimes(internalDriverCount, stationCount, rand);
            bool[][,] internalDriverTrackProficiencies = DataGenerator.GenerateInternalDriverTrackProficiencies(internalDriverCount, stationCount, rand);
            int[][] externalDriversHomeTravelTimes = DataGenerator.GenerateExternalDriverHomeTravelTimes(DataConfig.ExternalDriverTypes.Length, stationCount, rand);

            // TODO: get internal driver contract times from settings
            int[] internalDriversContractTimes = new int[internalDriverCount];
            for (int i = 0; i < internalDriverCount; i++) internalDriversContractTimes[i] = DataConfig.ExcelInternalDriverContractTime;

            return new Instance(rand, rawTrips, stationNames, carTravelTimes, internalDriverNames, internalDriversHomeTravelTimes, internalDriverTrackProficiencies, internalDriversContractTimes, internalDriverIsInternational, externalDriversHomeTravelTimes);
        }

        /** Get a dictionary that converts from station name in data to station index in address list */
        static Dictionary<string, int> GetStationDataNameToAddressIndexDict(ExcelSheet linkingStationNamesSheet, string[] stationNames) {
            Dictionary<string, int> stationDataNameToAddressIndex = new Dictionary<string, int>();
            linkingStationNamesSheet.ForEachRow(linkingStationNamesRow => {
                string dataStationName = linkingStationNamesSheet.GetStringValue(linkingStationNamesRow, "Station name in data");
                string addressStationName = linkingStationNamesSheet.GetStringValue(linkingStationNamesRow, "Station name in address list");

                int addressStationIndex = Array.IndexOf(stationNames, addressStationName);
                if (addressStationIndex == -1) {
                    throw new Exception();
                }
                stationDataNameToAddressIndex.Add(dataStationName, addressStationIndex);
            });
            return stationDataNameToAddressIndex;
        }

        static Trip[] ParseRawTrips(ExcelSheet dutiesSheet, DateTime planningStartDate, DateTime planningNextDate, Dictionary<string, int> stationDataNameToAddressIndex) {
            List<Trip> rawTripList = new List<Trip>();
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
                if (!stationDataNameToAddressIndex.ContainsKey(startStationDataName)) throw new Exception(string.Format("Unknown station `{0}`", startStationDataName));
                int startStationAddressIndex = stationDataNameToAddressIndex[startStationDataName];

                string endStationDataName = dutiesSheet.GetStringValue(dutyRow, "DestinationLocationName") ?? "";
                if (endStationDataName == "") return; // Skip row if end location is empty
                if (!stationDataNameToAddressIndex.ContainsKey(endStationDataName)) throw new Exception(string.Format("Unknown station `{0}`", endStationDataName));
                int endStationAddressIndex = stationDataNameToAddressIndex[endStationDataName];

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

                rawTripList.Add(new Trip(dutyName, activityName, dutyId, projectName, startStationDataName, endStationDataName, startStationAddressIndex, endStationAddressIndex, startTime, endTime, duration));
            });
            Trip[] rawTrips = rawTripList.ToArray();

            if (rawTrips.Length == 0) {
                throw new Exception("No trips found in timeframe");
            }

            return rawTrips;
        }

        static int[,] EstimateOrGenerateTrainTravelTimes(Trip[] rawTrips, string[] stationNames, XorShiftRandom rand) {
            // Extract train travel times
            List<int>[,] trainTravelTimesAll = new List<int>[stationNames.Length, stationNames.Length];
            for (int rawTripIndex = 0; rawTripIndex < rawTrips.Length; rawTripIndex++) {
                Trip trip = rawTrips[rawTripIndex];
                if (trainTravelTimesAll[trip.StartStationAddressIndex, trip.EndStationAddressIndex] == null) trainTravelTimesAll[trip.StartStationAddressIndex, trip.EndStationAddressIndex] = new List<int>();
                trainTravelTimesAll[trip.StartStationAddressIndex, trip.EndStationAddressIndex].Add(trip.Duration);
            }

            // Estimate or generate train travel times
            int[,] trainTravelTimes = new int[stationNames.Length, stationNames.Length];
            for (int i = 0; i < stationNames.Length; i++) {
                for (int j = 0; j < stationNames.Length; j++) {
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

        static (string[], bool[]) ParseInternalDriverNamesAndInternationalStatus(ExcelSheet employeesSheet) {
            List<string> internalDriverNames = new List<string>();
            List<bool> internalDriverIsInternational = new List<bool>();
            employeesSheet.ForEachRow(employeeRow => {
                // Only include configures companies
                string driverCompany = employeesSheet.GetStringValue(employeeRow, "PrimaryCompany");
                if (!ParseHelper.DataStringInList(driverCompany, DataConfig.ExcelIncludedRailwayUndertakings)) return;

                // Only include configures job titles
                string driverJobTitle = employeesSheet.GetStringValue(employeeRow, "PrimaryJobTitle");
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

                string driverName = employeesSheet.GetStringValue(employeeRow, "FullName");
                internalDriverNames.Add(driverName);
                internalDriverIsInternational.Add(isInternationalDriver);
            });
            return (internalDriverNames.ToArray(), internalDriverIsInternational.ToArray());
        }

        static bool[][,] ParseInternalDriverTrackProficiencies(ExcelSheet routeKnowledgeTable, string[] internalDriverNames, string[] stationNames) {
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
                string station1Name = routeKnowledgeTable.GetStringValue(routeKnowledgeRow, "OriginLocationName");
                int station1Index = Array.IndexOf(stationNames, station1Name);
                string station2Name = routeKnowledgeTable.GetStringValue(routeKnowledgeRow, "DestinationLocationName");
                int station2Index = Array.IndexOf(stationNames, station2Name);
                if (station1Index == -1 || station2Index == -1) return;

                // Get driver index
                string driverName = routeKnowledgeTable.GetStringValue(routeKnowledgeRow, "EmployeeName");
                int driverIndex = Array.IndexOf(internalDriverNames, driverName);
                if (driverIndex == -1) return;

                internalDriverProficiencies[driverIndex][station1Index, station2Index] = true;
                internalDriverProficiencies[driverIndex][station2Index, station1Index] = true;
            });

            return internalDriverProficiencies;
        }

        static int GetOrAddStringIndex(string code, List<string> codes) {
            if (codes.Contains(code)) {
                // Existing code
                return codes.FindIndex(searchCode => searchCode == code);
            }

            // New code
            codes.Add(code);
            return codes.Count - 1;
        }
    }
}

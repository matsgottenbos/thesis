using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class DataMiscProcessor {
        public static (string[], int[,], int[,], int[,]) GetStationNamesAndExpectedCarTravelInfo() {
            (int[,] plannedCarTravelTimes, int[,] carTravelDistances, string[] stationNames) = TravelInfoImporter.ImportFullyConnectedTravelInfo(Path.Combine(AppConfig.IntermediateFolder, "stationTravelInfo.csv"));

            int stationCount = plannedCarTravelTimes.GetLength(0);
            int[,] expectedCarTravelTimes = new int[stationCount, stationCount];
            for (int location1Index = 0; location1Index < stationCount; location1Index++) {
                for (int location2Index = location1Index; location2Index < stationCount; location2Index++) {
                    int plannedTravelTimeBetween = plannedCarTravelTimes[location1Index, location2Index];
                    int expectedTravelTimeBetween;
                    if (plannedTravelTimeBetween == 0) {
                        expectedTravelTimeBetween = 0;
                    } else {
                        expectedTravelTimeBetween = plannedTravelTimeBetween + RulesConfig.TravelDelayExpectedFunc(plannedTravelTimeBetween);
                    }
                    expectedCarTravelTimes[location1Index, location2Index] = expectedTravelTimeBetween;
                    expectedCarTravelTimes[location2Index, location1Index] = expectedTravelTimeBetween;
                }
            }

            return (stationNames, plannedCarTravelTimes, expectedCarTravelTimes, carTravelDistances);
        }

        public static string[][] GetStationCountryQualifications(XSSFWorkbook stationAddressesBook, string[] stationNames) {
            ExcelSheet stationAddressesSheet = new ExcelSheet("Station addresses", stationAddressesBook);

            string[][] stationCountryQualifications = new string[stationNames.Length][];
            stationAddressesSheet.ForEachRow(stationAddressesRow => {
                string stationName = stationAddressesSheet.GetStringValue(stationAddressesRow, "Station name");
                int stationIndex = Array.IndexOf(stationNames, stationName);
                if (stationIndex == -1) throw new Exception(string.Format("Station `{0}` not found in travel info", stationName));

                string countryQualificationsStr = stationAddressesSheet.GetStringValue(stationAddressesRow, "Country qualifications");
                string[] countryQualifications = countryQualificationsStr.Split(", ");
                stationCountryQualifications[stationIndex] = countryQualifications;
            });
            return stationCountryQualifications;
        }

        public static string[] GetDataStationNamesWithoutSwitching(XSSFWorkbook stationAddressesBook) {
            ExcelSheet linkingStationNamesSheet = new ExcelSheet("Linking station names", stationAddressesBook);

            List<string> dataStationNamesWithoutSwitching = new List<string>();
            linkingStationNamesSheet.ForEachRow(linkingStationNamesRow => {
                string stationName = linkingStationNamesSheet.GetStringValue(linkingStationNamesRow, "Station name in data");
                bool? canSwitchDrivers = linkingStationNamesSheet.GetBoolValue(linkingStationNamesRow, "Can switch drivers?");
                if (canSwitchDrivers.HasValue && !canSwitchDrivers.Value) {
                    dataStationNamesWithoutSwitching.Add(stationName);
                }
            });
            return dataStationNamesWithoutSwitching.ToArray();
        }

        public static Driver[] GetDataAssignment(XSSFWorkbook settingsBook, Activity[] activities, InternalDriver[] internalDrivers, Dictionary<(string, bool), ExternalDriver[]> externalDriversByDataTypeDict) {
            ExcelSheet externalDriversSettingsSheet = new ExcelSheet("External drivers", settingsBook);
            List<(string, string)> externalInternationalDriverNames = new List<(string, string)>();
            externalDriversSettingsSheet.ForEachRow(externalDriverSettingsRow => {
                bool? isInternationalDriver = externalDriversSettingsSheet.GetBoolValue(externalDriverSettingsRow, "Is international?");
                if (!isInternationalDriver.HasValue || !isInternationalDriver.Value) return;

                string driverName = externalDriversSettingsSheet.GetStringValue(externalDriverSettingsRow, "External driver name");
                string companyName = externalDriversSettingsSheet.GetStringValue(externalDriverSettingsRow, "Company name");
                externalInternationalDriverNames.Add((driverName, companyName));
            });

            Driver[] dataAssignment = new Driver[activities.Length];
            Dictionary<(string, bool), List<string>> externalDriverNamesByTypeDict = new Dictionary<(string, bool), List<string>>();
            for (int activityIndex = 0; activityIndex < dataAssignment.Length; activityIndex++) {
                Activity activity = activities[activityIndex];
                if (activity.DataAssignedCompanyName == null || activity.DataAssignedEmployeeName == null) {
                    // Unassigned activity
                    continue;
                }

                if (DataConfig.ExcelInternalDriverCompanyNames.Contains(activity.DataAssignedCompanyName)) {
                    // Assigned to internal driver
                    dataAssignment[activityIndex] = Array.Find(internalDrivers, internalDriver => internalDriver.GetInternalDriverName(true) == activity.DataAssignedEmployeeName);
                } else {
                    // Assigned to external driver
                    bool isInternational = externalInternationalDriverNames.Contains((activity.DataAssignedEmployeeName, activity.DataAssignedCompanyName));

                    // Get list of already encountered names of this type
                    List<string> externalDriverNamesOfType;
                    if (externalDriverNamesByTypeDict.ContainsKey((activity.DataAssignedCompanyName, isInternational))) {
                        externalDriverNamesOfType = externalDriverNamesByTypeDict[(activity.DataAssignedCompanyName, isInternational)];
                    } else {
                        externalDriverNamesOfType = new List<string>();
                        externalDriverNamesByTypeDict.Add((activity.DataAssignedCompanyName, isInternational), externalDriverNamesOfType);
                    }

                    // Determine index of this driver in the type
                    int externalDriverIndexInType = externalDriverNamesOfType.IndexOf(activity.DataAssignedEmployeeName);
                    if (externalDriverIndexInType == -1) {
                        externalDriverIndexInType = externalDriverNamesOfType.Count;
                        externalDriverNamesOfType.Add(activity.DataAssignedEmployeeName);
                    }

                    if (!externalDriversByDataTypeDict.ContainsKey((activity.DataAssignedCompanyName, isInternational))) {
                        // Assigned to unknown company
                        continue;
                    }

                    ExternalDriver[] externalDriversOfType = externalDriversByDataTypeDict[(activity.DataAssignedCompanyName, isInternational)];
                    dataAssignment[activityIndex] = externalDriversOfType[externalDriverIndexInType];
                }
            }
            return dataAssignment;
        }
    }
}

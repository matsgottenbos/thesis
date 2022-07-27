/*
 * Process miscellaneous parts of imported data
*/

using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public static class DataMiscProcessor {
        public static (string[], int[,], int[,], int[,]) GetStationNamesAndExpectedCarTravelInfo() {
            (int[,] plannedCarTravelTimes, int[,] carTravelDistances, string[] stationNames) = TravelInfoImporter.ImportFullyConnectedTravelInfo(Path.Combine(DevConfig.IntermediateFolder, "stationTravelInfo.csv"));

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

        public static (string[], string[]) GetBorderAndBorderRegionStationNames(XSSFWorkbook stationAddressesBook) {
            ExcelSheet linkingStationNamesSheet = new ExcelSheet("Linking station names", stationAddressesBook);

            List<string> borderStationNamesList = new List<string>();
            List<string> borderRegionStationNamesList = new List<string>();
            linkingStationNamesSheet.ForEachRow(linkingStationNamesRow => {
                string stationName = linkingStationNamesSheet.GetStringValue(linkingStationNamesRow, "Station name in data");
                bool? isBorder = linkingStationNamesSheet.GetBoolValue(linkingStationNamesRow, "Is border?");
                bool? isBorderRegion = linkingStationNamesSheet.GetBoolValue(linkingStationNamesRow, "Is in border region?");
                if (isBorder.HasValue && isBorder.Value) {
                    borderStationNamesList.Add(stationName);
                }
                if (isBorderRegion.HasValue && isBorderRegion.Value) {
                    borderRegionStationNamesList.Add(stationName);
                }
            });
            return (borderStationNamesList.ToArray(), borderRegionStationNamesList.ToArray());
        }

        public static Driver[] GetDataAssignment(XSSFWorkbook driversBook, Activity[] activities, InternalDriver[] internalDrivers, Dictionary<(string, bool), ExternalDriver[]> externalDriversByDataTypeDict) {
            ExcelSheet externalDriversSettingsSheet = new ExcelSheet("External drivers", driversBook);
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

                if (AppConfig.InternalDriverCompanyNames.Contains(activity.DataAssignedCompanyName)) {
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

/*
 * Helper methods to export travel info
*/

using NPOI.XSSF.UserModel;

namespace DriverPlannerShared {
    public static class TravelInfoExporter {
        /* Determine specific travel info */

        public static void DetermineAndExportAllTravelInfos() {
            XSSFWorkbook addressesBook = ExcelHelper.ReadExcelFile(Path.Combine(DevConfig.InputFolder, "Station addresses.xlsx"));
            ExcelSheet stationAddressesSheet = new ExcelSheet("Station addresses", addressesBook);

            XSSFWorkbook driversBook = ExcelHelper.ReadExcelFile(Path.Combine(DevConfig.InputFolder, "Drivers.xlsx"));
            ExcelSheet internalDriversSheet = new ExcelSheet("Internal drivers", driversBook);
            ExcelSheet externalDriverCompaniesSheet = new ExcelSheet("External driver companies", driversBook);

            (List<LocationInfo> stationLocations, bool isSuccess) = DetermineAndExportStationTravelInfo(stationAddressesSheet);
            if (!isSuccess) return;
            isSuccess = DetermineAndExportInternalTravelInfo(internalDriversSheet, stationLocations);
            if (!isSuccess) return;
            DetermineAndExportExternalTravelInfo(externalDriverCompaniesSheet, stationLocations);
        }

        static (List<LocationInfo>, bool) DetermineAndExportStationTravelInfo(ExcelSheet stationAddressesSheet) {
            List<LocationInfo> stationLocations = new List<LocationInfo>();
            stationAddressesSheet.ForEachRow(stationAddressRow => {
                string name = stationAddressesSheet.GetStringValue(stationAddressRow, "Station name");
                string address = stationAddressesSheet.GetStringValue(stationAddressRow, "Address");
                stationLocations.Add(new LocationInfo(name, address, stationLocations.Count));
            });

            string csvFilePath = Path.Combine(DevConfig.IntermediateFolder, "stationTravelInfo.csv");
            bool isSuccess = DetermineAndExportFullyConnectedTravelInfo(stationLocations, "station", csvFilePath);
            return (stationLocations, isSuccess);
        }

        static bool DetermineAndExportInternalTravelInfo(ExcelSheet internalDriversSheet, List<LocationInfo> stationLocations) {
            List<LocationInfo> internalLocations = new List<LocationInfo>();
            internalDriversSheet.ForEachRow(internalDriversRow => {
                string name = internalDriversSheet.GetStringValue(internalDriversRow, "Internal driver name");
                string address = internalDriversSheet.GetStringValue(internalDriversRow, "Home address");
                internalLocations.Add(new LocationInfo(name, address, internalLocations.Count));
            });

            string csvFilePath = Path.Combine(DevConfig.IntermediateFolder, "internalTravelInfo.csv");
            return DetermineAndExportBipartiteTravelInfo(internalLocations, stationLocations, "internal", csvFilePath);
        }

        static bool DetermineAndExportExternalTravelInfo(ExcelSheet externalDriverCompaniesSheet, List<LocationInfo> stationLocations) {
            List<LocationInfo> externalLocations = new List<LocationInfo>();
            externalDriverCompaniesSheet.ForEachRow(externalDriverCompaniesRow => {
                string name = externalDriverCompaniesSheet.GetStringValue(externalDriverCompaniesRow, "External driver type name");
                string address = externalDriverCompaniesSheet.GetStringValue(externalDriverCompaniesRow, "Driver starting address");
                externalLocations.Add(new LocationInfo(name, address, externalLocations.Count));
            });

            string csvFilePath = Path.Combine(DevConfig.IntermediateFolder, "externalTravelInfo.csv");
            return DetermineAndExportBipartiteTravelInfo(externalLocations, stationLocations, "external", csvFilePath);
        }


        /* Travel info helper methods */

        static bool DetermineAndExportFullyConnectedTravelInfo(List<LocationInfo> locations, string travelTypeName, string csvFilePath) {
            (int?[,] importedTravelTimes, int?[,] importedTravelDistances, List<LocationInfo> missingLocations) = TravelInfoImporter.ImportPartialFullyConnectedTravelInfo(locations, csvFilePath);

            string missingLocationCountStr = string.Format("Missing {0} {1} travel origin locations:\n{2}", missingLocations.Count, travelTypeName, JoinLocationInfoNames(missingLocations));
            int missingTravelInfoCount = missingLocations.Count * locations.Count;
            (bool isSuccess, bool shouldContinue) = LogMapsApiConfirmationPrompt(missingLocations.Count, missingLocationCountStr, missingTravelInfoCount, travelTypeName);
            if (!shouldContinue) return isSuccess;

            // Add missing info through the Google Maps API
            isSuccess = TravelInfoMapsApiHandler.AddMissingFullyConnectedTravelInfo(missingLocations, locations, importedTravelTimes, importedTravelDistances);
            if (!isSuccess) return false;
            int[,] travelTimes = NullableToNormalArray(importedTravelTimes);
            int[,] travelDistances = NullableToNormalArray(importedTravelDistances);

            // Write output to file
            TravelInfoCsvHandler.ExportFullyConnectedTravelInfoToCsv(locations, travelTimes, travelDistances, csvFilePath);
            return true;
        }

        static bool DetermineAndExportBipartiteTravelInfo(List<LocationInfo> originLocations, List<LocationInfo> destinationLocations, string travelTypeName, string csvFilePath) {
            (int?[][] importedTravelTimes, int?[][] importedTravelDistances, List<LocationInfo> missingOriginLocations, List<LocationInfo> missingDestinationLocations) = TravelInfoImporter.ImportPartialBipartiteTravelInfo(originLocations, destinationLocations, csvFilePath);

            int missingLocationCount = missingOriginLocations.Count + missingDestinationLocations.Count;
            string missingLocationCountStr = string.Format("Missing {0} {1} travel origin locations:\n{2}\n\nMissing {3} {1} travel destination locations:\n{4}", missingOriginLocations.Count, travelTypeName, JoinLocationInfoNames(missingOriginLocations), missingDestinationLocations.Count, JoinLocationInfoNames(missingDestinationLocations));
            int missingTravelInfoCount = missingOriginLocations.Count * destinationLocations.Count + (originLocations.Count - missingOriginLocations.Count) * missingDestinationLocations.Count;
            (bool isSuccess, bool shouldContinue) = LogMapsApiConfirmationPrompt(missingLocationCount, missingLocationCountStr, missingTravelInfoCount, travelTypeName);
            if (!shouldContinue) return isSuccess;

            // Add missing info through the Google Maps API
            isSuccess = TravelInfoMapsApiHandler.AddMissingBipartiteTravelInfo(missingOriginLocations, originLocations, missingDestinationLocations, destinationLocations, importedTravelTimes, importedTravelDistances);
            if (!isSuccess) return false;
            int[][] travelTimes = NullableToNormalArray(importedTravelTimes);
            int[][] travelDistances = NullableToNormalArray(importedTravelDistances);

            // Write output to file
            TravelInfoCsvHandler.ExportBipartiteTravelInfoToCsv(originLocations, destinationLocations, travelTimes, travelDistances, csvFilePath);
            return true;
        }

        static (bool, bool) LogMapsApiConfirmationPrompt(int missingLocationCount, string missingLocationsStr, int missingTravelInfoCount, string travelTypeName) {
            if (missingLocationCount == 0) {
                return (true, false);
            } else {
                Console.WriteLine("\n{0}\n\nTravel info for {1} pairs of {2} locations needs to be requested from the Google Maps API.", missingLocationsStr, missingTravelInfoCount, travelTypeName);
                Console.WriteLine("Type `Y` and hit enter to continue. Type anything else to exit.");
                string consoleLine = Console.ReadLine();
                if (consoleLine != "Y") {
                    Console.WriteLine("Unexpected input, exiting.");
                    return (false, false);
                }
            }
            return (true, true);
        }

        static int[,] NullableToNormalArray(int?[,] nullableArray) {
            int[,] array = new int[nullableArray.GetLength(0), nullableArray.GetLength(1)];
            for (int i = 0; i < nullableArray.GetLength(0); i++) {
                for (int j = 0; j < nullableArray.GetLength(1); j++) {
                    array[i, j] = nullableArray[i, j].Value;
                }
            }
            return array;
        }
        static int[][] NullableToNormalArray(int?[][] nullableArray) {
            int[][] array = new int[nullableArray.Length][];
            for (int i = 0; i < nullableArray.Length; i++) {
                array[i] = new int[nullableArray[i].Length];
                for (int j = 0; j < array[i].Length; j++) {
                    array[i][j] = nullableArray[i][j].Value;
                }
            }
            return array;
        }

        static string JoinLocationInfoNames(List<LocationInfo> locationInfos) {
            return string.Join("\n", locationInfos.Select(locationInfo => locationInfo.Name));
        }
    }

    public class LocationInfo {
        public string Name, Address;
        public int Index;

        public LocationInfo(string name, string address, int index) {
            Name = name;
            Address = address;
            Index = index;
        }
    }


}

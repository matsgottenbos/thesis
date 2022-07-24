using CsvHelper;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class TravelInfoExporter {
        /* Determine specific travel info */

        public static void DetermineAndExportAllTravelInfos() {
            XSSFWorkbook addressesBook = ExcelHelper.ReadExcelFile(Path.Combine(DevConfig.InputFolder, "stationAddresses.xlsx"));
            ExcelSheet stationAddressesSheet = new ExcelSheet("Station addresses", addressesBook);

            XSSFWorkbook settingsBook = ExcelHelper.ReadExcelFile(Path.Combine(DevConfig.InputFolder, "settings.xlsx"));
            ExcelSheet internalAddressesSheet = new ExcelSheet("Internal drivers", settingsBook);
            ExcelSheet externalAddressesSheet = new ExcelSheet("External driver companies", settingsBook);

            (List<LocationInfo> stationLocations, bool isSuccess) = DetermineAndExportStationTravelInfo(stationAddressesSheet);
            if (!isSuccess) return;
            isSuccess = DetermineAndExportInternalTravelInfo(internalAddressesSheet, stationLocations);
            if (!isSuccess) return;
            DetermineAndExportExternalTravelInfo(externalAddressesSheet, stationLocations);
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

        static bool DetermineAndExportInternalTravelInfo(ExcelSheet internalAddressesSheet, List<LocationInfo> stationLocations) {
            List<LocationInfo> internalLocations = new List<LocationInfo>();
            internalAddressesSheet.ForEachRow(internalAddressRow => {
                string name = internalAddressesSheet.GetStringValue(internalAddressRow, "Internal driver name");
                string address = internalAddressesSheet.GetStringValue(internalAddressRow, "Home address");
                internalLocations.Add(new LocationInfo(name, address, internalLocations.Count));
            });

            string csvFilePath = Path.Combine(DevConfig.IntermediateFolder, "internalTravelInfo.csv");
            return DetermineAndExportBipartiteTravelInfo(internalLocations, stationLocations, "internal", csvFilePath);
        }

        static bool DetermineAndExportExternalTravelInfo(ExcelSheet externalAddressesSheet, List<LocationInfo> stationLocations) {
            List<LocationInfo> externalLocations = new List<LocationInfo>();
            externalAddressesSheet.ForEachRow(internalAddressRow => {
                string name = externalAddressesSheet.GetStringValue(internalAddressRow, "External driver type name"); // TODO: column has changed
                string address = externalAddressesSheet.GetStringValue(internalAddressRow, "Driver starting address");
                externalLocations.Add(new LocationInfo(name, address, externalLocations.Count));
            });

            string csvFilePath = Path.Combine(DevConfig.IntermediateFolder, "externalTravelInfo.csv");
            return DetermineAndExportBipartiteTravelInfo(externalLocations, stationLocations, "external", csvFilePath);
        }


        /* Travel info helper methods */

        static bool DetermineAndExportFullyConnectedTravelInfo(List<LocationInfo> locations, string locationTypeName, string csvFilePath) {
            Console.WriteLine("\n* Checking {0} travel info *", locationTypeName);
            (int?[,] importedTravelTimes, int?[,] importedTravelDistances, List<LocationInfo> missingLocations) = TravelInfoImporter.ImportPartialFullyConnectedTravelInfo(locations, csvFilePath);

            int missingLocationCount = missingLocations.Count;
            string missingLocationCountStr = string.Format("{0}/{1}", missingLocations.Count, locations.Count);
            int missingTravelInfoCount = missingLocations.Count * locations.Count;
            (bool isSuccess, bool shouldContinue) = LogMapsApiConfirmationPrompt(missingLocationCount, missingLocationCountStr, missingTravelInfoCount);
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

        static bool DetermineAndExportBipartiteTravelInfo(List<LocationInfo> originLocations, List<LocationInfo> destinationLocations, string locationTypeName, string csvFilePath) {
            Console.WriteLine("\n* Checking {0} travel info *", locationTypeName);
            (int?[][] importedTravelTimes, int?[][] importedTravelDistances, List<LocationInfo> missingOriginLocations, List<LocationInfo> missingDestinationLocations) = TravelInfoImporter.ImportPartialBipartiteTravelInfo(originLocations, destinationLocations, csvFilePath);

            int missingLocationCount = missingOriginLocations.Count + missingDestinationLocations.Count;
            string missingLocationCountStr = string.Format("{0}/{1} origin and {2}/{3} destination", missingOriginLocations.Count, originLocations.Count, missingDestinationLocations.Count, destinationLocations.Count);
            int missingTravelInfoCount = missingOriginLocations.Count * destinationLocations.Count + (originLocations.Count - missingOriginLocations.Count) * missingDestinationLocations.Count;
            (bool isSuccess, bool shouldContinue) = LogMapsApiConfirmationPrompt(missingLocationCount, missingLocationCountStr, missingTravelInfoCount);
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

        static (bool, bool) LogMapsApiConfirmationPrompt(int missingLocationCount, string missingLocationCountStr, int missingTravelInfoCount) {
            if (missingLocationCount == 0) {
                Console.WriteLine("Already up to date.");
                return (true, false);
            } else {
                Console.WriteLine("Missing {0} locations. Travel info for {1} pairs of locations needs to be requested from the Google Maps API.", missingLocationCountStr ?? missingLocationCount.ToString(), missingTravelInfoCount);
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
    }

    class LocationInfo {
        public string Name, Address;
        public int Index;

        public LocationInfo(string name, string address, int index) {
            Name = name;
            Address = address;
            Index = index;
        }
    }

    
}

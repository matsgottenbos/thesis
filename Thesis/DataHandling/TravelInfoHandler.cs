using CsvHelper;
using Google.Maps;
using Google.Maps.DistanceMatrix;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class StationInfo {
        public string Name, Address;
        public int Index;

        public StationInfo(string name, string address, int index) {
            Name = name;
            Address = address;
            Index = index;
        }
    }

    static class TravelInfoHandler {
        // TODO: move to config
        const string googleMapsApiKey = "AIzaSyAnnCoTq3j55VQeQsTjxryHh4VYHyinoaA";
        static readonly string csvFilePath = System.IO.Path.Combine(AppConfig.DataFolder, "travelInfo.csv");

        public static void DetermineAndExportTravelInfo() {
            XSSFWorkbook excelBook = ExcelHelper.ReadExcelFile(System.IO.Path.Combine(AppConfig.DataFolder, "addresses.xlsx"));

            ExcelSheet stationAddressesSheet = new ExcelSheet("Station addresses", excelBook);

            List<StationInfo> stations = new List<StationInfo>();
            stationAddressesSheet.ForEachRow(stationAddressRow => {
                string stationName = ParseHelper.CleanDataString(stationAddressRow.GetCell(stationAddressesSheet.GetColumnIndex("Station"))?.StringCellValue);
                string address = ParseHelper.CleanDataString(stationAddressRow.GetCell(stationAddressesSheet.GetColumnIndex("Address"))?.StringCellValue);
                stations.Add(new StationInfo(stationName, address, stations.Count));
            });

            (int?[,] importedTravelTimes, int?[,] importedTravelDistances, List<string> importedStationNames) = ImportTravelInfo(stations);
            List<StationInfo> missingStations = stations.Copy();
            missingStations.RemoveAll(station => importedStationNames.Contains(station.Name));

            // Add missing info through the Google Maps API
            bool isSuccess = AddMissingTravelInfoFromMaps(missingStations, stations, importedTravelTimes, importedTravelDistances);
            if (!isSuccess) return;
            int[,] travelTimes = NullableToNormalArray(importedTravelTimes);
            int[,] travelDistances = NullableToNormalArray(importedTravelDistances);

            // Get CSV output
            List<TravelInfoCsv> travelInfoCsv = GetTravelInfoCsv(stations, travelTimes, travelDistances);

            // Write output to file
            using StreamWriter streamWriter = new StreamWriter(csvFilePath);
            using CsvWriter csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
            csvWriter.WriteRecords(travelInfoCsv);
        }

        static (int?[,], int?[,], List<string>) ImportTravelInfo(List<StationInfo> stations) {
            using StreamReader streamReader = new StreamReader(csvFilePath);
            using CsvReader csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
            List<TravelInfoCsv> travelInfoCsv = csvReader.GetRecords<TravelInfoCsv>().ToList();

            int?[,] importedTravelTimes = new int?[stations.Count, stations.Count];
            int?[,] importedTravelDistances = new int?[stations.Count, stations.Count];
            List<string> importedStationNames = new List<string>();

            // Set time and distance to self to 0
            for (int stationIndex = 0; stationIndex < stations.Count; stationIndex++) {
                importedTravelTimes[stationIndex, stationIndex] = 0;
                importedTravelDistances[stationIndex, stationIndex] = 0;
            }

            // Set imported times and distances; also store which station names appear in imported travel info
            for (int travelInfoIndex = 0; travelInfoIndex < travelInfoCsv.Count; travelInfoIndex++) {
                TravelInfoCsv travelInfo = travelInfoCsv[travelInfoIndex];
                if (!importedStationNames.Contains(travelInfo.station1Name)) importedStationNames.Add(travelInfo.station1Name);
                if (!importedStationNames.Contains(travelInfo.station2Name)) importedStationNames.Add(travelInfo.station2Name);

                int station1Index = stations.Find(station => station.Name == travelInfo.station1Name).Index;
                int station2Index = stations.Find(station => station.Name == travelInfo.station2Name).Index;
                if (station1Index == -1 || station2Index == -1) continue;

                importedTravelTimes[station1Index, station2Index] = travelInfo.travelTimeMinutes;
                importedTravelTimes[station2Index, station1Index] = travelInfo.travelTimeMinutes;
                importedTravelDistances[station1Index, station2Index] = travelInfo.travelDistanceKilometers;
                importedTravelDistances[station2Index, station1Index] = travelInfo.travelDistanceKilometers;
            }

            return (importedTravelTimes, importedTravelDistances, importedStationNames);
        }

        static bool AddMissingTravelInfoFromMaps(List<StationInfo> originStations, List<StationInfo> destinationStations, int?[,] importedTravelTimes, int?[,] importedTravelDistances) {
            if (originStations.Count == 0 || destinationStations.Count == 0) {
                return true;
            }

            // Set API key
            GoogleSigned.AssignAllServices(new GoogleSigned(googleMapsApiKey));

            // TODO: split requests (max 25 origins, max 25 destinations, max 100 elements)

            int maxDestinationGroupSize = 25;

            // Perform separate calls for each origin
            for (int originIndex = 0; originIndex < originStations.Count; originIndex++) {
                int destinationGroupCount = (int)Math.Ceiling((float)destinationStations.Count / maxDestinationGroupSize);
                List<StationInfo> requestOriginStations = originStations.GetRange(originIndex, 1);

                // Split destinations into groups if needed to avoid exceeding request size limits
                for (int destinationGroupIndex = 0; destinationGroupIndex < destinationGroupCount; destinationGroupIndex++) {
                    int requestDestinationsFirstIndex = destinationGroupIndex * maxDestinationGroupSize;
                    int requestDestinationsNextIndex = Math.Min((destinationGroupIndex + 1) * maxDestinationGroupSize, destinationStations.Count);
                    int requestDestinationCount = requestDestinationsNextIndex - requestDestinationsFirstIndex;

                    List<StationInfo> requestDestinationStations = destinationStations.GetRange(requestDestinationsFirstIndex, requestDestinationCount);

                    bool isSuccess = PerformMapsCallAndStoreTravelInfo(requestOriginStations, requestDestinationStations, importedTravelTimes, importedTravelDistances);
                    if (!isSuccess) return false;
                }
            }

            return true;
        }

        static bool PerformMapsCallAndStoreTravelInfo(List<StationInfo> originStations, List<StationInfo> destinationStations, int?[,] importedTravelTimes, int?[,] importedTravelDistances) {
            DistanceMatrixRequest request = new DistanceMatrixRequest();
            for (int originIndex = 0; originIndex < originStations.Count; originIndex++) {
                request.AddOrigin(originStations[originIndex].Address);
            }
            for (int destinationIndex = 0; destinationIndex < destinationStations.Count; destinationIndex++) {
                request.AddDestination(destinationStations[destinationIndex].Address);
            }

            DistanceMatrixResponse response = new DistanceMatrixService().GetResponse(request);

            if (response.Status != ServiceResponseStatus.Ok) {
                Console.WriteLine("Request failed with status `{0}` and error message `{1}`", response.Status, response.ErrorMessage);
                return false;
            }

            if (response.Rows.Length == 0) {
                Console.WriteLine("Request returned no results");
                return false;
            }

            // Store travel info
            for (int originIndex = 0; originIndex < response.Rows.Length; originIndex++) {
                StationInfo station1 = originStations[originIndex];
                DistanceMatrixResponse.DistanceMatrixRows row = response.Rows[originIndex];

                for (int destinationIndex = 0; destinationIndex < row.Elements.Length; destinationIndex++) {
                    StationInfo station2 = destinationStations[destinationIndex];
                    if (station1 == station2) continue;

                    DistanceMatrixResponse.DistanceMatrixElement cell = row.Elements[destinationIndex];
                    int durationMinutes = (int)Math.Round((float)cell.duration.Value / 60);
                    int distanceKm = (int)Math.Round((float)cell.distance.Value / 1000);

                    importedTravelTimes[station1.Index, station2.Index] = durationMinutes;
                    importedTravelTimes[station2.Index, station1.Index] = durationMinutes;
                    importedTravelDistances[station1.Index, station2.Index] = distanceKm;
                    importedTravelDistances[station2.Index, station1.Index] = distanceKm;
                }
            }

            return true;
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

        static List<TravelInfoCsv> GetTravelInfoCsv(List<StationInfo> stations, int[,] travelTimes, int[,] travelDistances) {
            List<TravelInfoCsv> travelInfoCsv = new List<TravelInfoCsv>();
            for (int station1Index = 0; station1Index < stations.Count; station1Index++) {
                StationInfo station1 = stations[station1Index];

                // Loop through all destinations later in the list than the origin
                for (int station2Index = station1Index + 1; station2Index < stations.Count; station2Index++) {
                    StationInfo station2 = stations[station2Index];
                    int durationMinutes = travelTimes[station1Index, station2Index];
                    int distanceKm = travelDistances[station1Index, station2Index];
                    travelInfoCsv.Add(new TravelInfoCsv(station1.Name, station2.Name, durationMinutes, distanceKm));
                }
            }

            return travelInfoCsv;
        }
    }

    // Type to export travel into CSV using CsvHelper.WriteRecords
    class TravelInfoCsv {
        public string station1Name { get; set; }
        public string station2Name { get; set; }
        public int travelTimeMinutes { get; set; }
        public int travelDistanceKilometers { get; set; }

        public TravelInfoCsv(string station1Name, string station2Name, int travelTimeMinutes, int travelDistanceKilometers) {
            this.station1Name = station1Name;
            this.station2Name = station2Name;
            this.travelTimeMinutes = travelTimeMinutes;
            this.travelDistanceKilometers = travelDistanceKilometers;
        }
    }
}

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
    static class TravelInfoHandler {
        // TODO: move to config
        static readonly string csvFilePath = System.IO.Path.Combine(AppConfig.DataFolder, "travelInfo.csv");

        public static void DetermineAndExportTravelInfo() {
            XSSFWorkbook excelBook = ExcelHelper.ReadExcelFile(System.IO.Path.Combine(AppConfig.DataFolder, "addresses.xlsx"));

            ExcelSheet stationAddressesSheet = new ExcelSheet("Station addresses", excelBook);

            List<StationInfo> stations = new List<StationInfo>();
            stationAddressesSheet.ForEachRow(stationAddressRow => {
                string stationName = stationAddressesSheet.GetStringValue(stationAddressRow, "Station");
                string address = stationAddressesSheet.GetStringValue(stationAddressRow, "Address");
                stations.Add(new StationInfo(stationName, address, stations.Count));
            });

            (int?[,] importedTravelTimes, int?[,] importedTravelDistances, List<StationInfo> missingStations) = ImportPartialTravelInfo(stations);

            if (missingStations.Count == 0) {
                Console.WriteLine("Travel info already contains all stations");
                return;
            } else {
                Console.WriteLine("Missing {0} stations in travel info", missingStations.Count);
                Console.WriteLine("Type `Y` and hit enter to confirm requesting these stations from the Google Maps API. Type anything else to exit.");
                string consoleLine = Console.ReadLine();
                if (consoleLine != "Y") {
                    Console.WriteLine("Unexpected input, exiting");
                    return;
                }
            }

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

        /** Returns 2D arrays of nullable travel times and distances for all given stations, filled in with imported data. Also returns missing station names in the imported data.  */
        static (int?[,], int?[,], List<StationInfo>) ImportPartialTravelInfo(List<StationInfo> allStations) {
            (int[,] importedTravelTimes, int[,] importedTravelDistances, string[] importedStationNames) = ImportTravelInfo();

            // Get objects of imported stations
            List<StationInfo> importedStations = new List<StationInfo>();
            for (int importedStationIndex = 0; importedStationIndex < importedStationNames.Length; importedStationIndex++) {
                int allStationsIndex = allStations.FindIndex(station => station.Name == importedStationNames[importedStationIndex]);
                if (allStationsIndex != -1) {
                    importedStations.Add(allStations[allStationsIndex]);
                }
            }

            // Create partial arrays
            int?[,] partialTravelTimes = new int?[allStations.Count, allStations.Count];
            int?[,] partialTravelDistances = new int?[allStations.Count, allStations.Count];
            for (int importedStation1Index = 0; importedStation1Index < importedStations.Count; importedStation1Index++) {
                StationInfo importedStation1 = importedStations[importedStation1Index];
                for (int importedStation2Index = 0; importedStation2Index < importedStations.Count; importedStation2Index++) {
                    StationInfo importedStation2 = importedStations[importedStation2Index];

                    partialTravelTimes[importedStation1.Index, importedStation2.Index] = importedTravelTimes[importedStation1Index, importedStation2Index];
                    partialTravelDistances[importedStation1.Index, importedStation2.Index] = importedTravelDistances[importedStation1Index, importedStation2Index];
                }
            }

            // Get list of missing stations
            List<StationInfo> missingStations = new List<StationInfo>(allStations);
            missingStations.RemoveAll(station => importedStations.Contains(station));

            return (partialTravelTimes, partialTravelDistances, missingStations);
        }

        public static (int[,], int[,], string[]) ImportTravelInfo() {
            List<TravelInfoCsv> travelInfoCsv;
            if (File.Exists(csvFilePath)) {
                using StreamReader streamReader = new StreamReader(csvFilePath);
                using CsvReader csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
                travelInfoCsv = csvReader.GetRecords<TravelInfoCsv>().ToList();
            } else {
                Console.WriteLine("File `data/travelInfo.csv` not found");
                travelInfoCsv = new List<TravelInfoCsv>();
            }

            // Get station names
            List<string> stationNames = new List<string>();
            for (int travelInfoIndex = 0; travelInfoIndex < travelInfoCsv.Count; travelInfoIndex++) {
                TravelInfoCsv travelInfo = travelInfoCsv[travelInfoIndex];
                if (!stationNames.Contains(travelInfo.station1Name)) stationNames.Add(travelInfo.station1Name);
                if (!stationNames.Contains(travelInfo.station2Name)) stationNames.Add(travelInfo.station2Name);
            }

            // Create travel time objects
            int[,] travelTimes = new int[stationNames.Count, stationNames.Count];
            int[,] travelDistances = new int[stationNames.Count, stationNames.Count];

            // Set time and distance to self to 0
            for (int stationIndex = 0; stationIndex < stationNames.Count; stationIndex++) {
                travelTimes[stationIndex, stationIndex] = 0;
                travelDistances[stationIndex, stationIndex] = 0;
            }

            // Set imported times and distances; also store which station names appear in imported travel info
            for (int travelInfoIndex = 0; travelInfoIndex < travelInfoCsv.Count; travelInfoIndex++) {
                TravelInfoCsv travelInfo = travelInfoCsv[travelInfoIndex];

                int station1Index = stationNames.IndexOf(travelInfo.station1Name);
                int station2Index = stationNames.IndexOf(travelInfo.station2Name);
                if (station1Index == -1 || station2Index == -1) continue;

                travelTimes[station1Index, station2Index] = travelInfo.travelTimeMinutes;
                travelTimes[station2Index, station1Index] = travelInfo.travelTimeMinutes;
                travelDistances[station1Index, station2Index] = travelInfo.travelDistanceKilometers;
                travelDistances[station2Index, station1Index] = travelInfo.travelDistanceKilometers;
            }

            return (travelTimes, travelDistances, stationNames.ToArray());
        }

        static bool AddMissingTravelInfoFromMaps(List<StationInfo> originStations, List<StationInfo> destinationStations, int?[,] importedTravelTimes, int?[,] importedTravelDistances) {
            if (originStations.Count == 0 || destinationStations.Count == 0) {
                return true;
            }

            // Set API key
            GoogleSigned.AssignAllServices(new GoogleSigned(DataConfig.GoogleMapsApiKey));

            // API limits per request: max 25 origins, max 25 destinations, max 100 elements
            // Perform separate calls for each origin
            for (int originIndex = 0; originIndex < originStations.Count; originIndex++) {
                int destinationGroupCount = (int)Math.Ceiling((float)destinationStations.Count / DataConfig.GoogleMapsMaxDestinationCountPerRequest);
                List<StationInfo> requestOriginStations = originStations.GetRange(originIndex, 1);

                // Split destinations into groups if needed to avoid exceeding request size limits
                for (int destinationGroupIndex = 0; destinationGroupIndex < destinationGroupCount; destinationGroupIndex++) {
                    int requestDestinationsFirstIndex = destinationGroupIndex * DataConfig.GoogleMapsMaxDestinationCountPerRequest;
                    int requestDestinationsNextIndex = Math.Min((destinationGroupIndex + 1) * DataConfig.GoogleMapsMaxDestinationCountPerRequest, destinationStations.Count);
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

    class StationInfo {
        public string Name, Address;
        public int Index;

        public StationInfo(string name, string address, int index) {
            Name = name;
            Address = address;
            Index = index;
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

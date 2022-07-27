/*
 * Helper methods to read and write travel info to CSV files
*/

using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public static class TravelInfoCsvHandler {
        /* Writing */

        public static void ExportFullyConnectedTravelInfoToCsv(List<LocationInfo> locations, int[,] travelTimes, int[,] travelDistances, string csvFilePath) {
            List<TravelInfoCsv> travelInfoCsv = new List<TravelInfoCsv>();
            for (int location1Index = 0; location1Index < locations.Count; location1Index++) {
                LocationInfo location1 = locations[location1Index];

                // Loop through all destinations later in the list than the origin to avoid duplicates
                for (int location2Index = location1Index + 1; location2Index < locations.Count; location2Index++) {
                    LocationInfo location2 = locations[location2Index];
                    int durationMinutes = travelTimes[location1Index, location2Index];
                    int distanceKm = travelDistances[location1Index, location2Index];
                    travelInfoCsv.Add(new TravelInfoCsv(location1.Name, location2.Name, durationMinutes, distanceKm));
                }
            }

            WriteCsvFile(travelInfoCsv, csvFilePath);
        }

        public static void ExportBipartiteTravelInfoToCsv(List<LocationInfo> originLocations, List<LocationInfo> destinationLocations, int[][] travelTimes, int[][] travelDistances, string csvFilePath) {
            List<TravelInfoCsv> travelInfoCsv = new List<TravelInfoCsv>();
            for (int originLocationIndex = 0; originLocationIndex < originLocations.Count; originLocationIndex++) {
                LocationInfo location1 = originLocations[originLocationIndex];
                for (int destinationLocationIndex = 0; destinationLocationIndex < destinationLocations.Count; destinationLocationIndex++) {
                    LocationInfo location2 = destinationLocations[destinationLocationIndex];
                    int durationMinutes = travelTimes[originLocationIndex][destinationLocationIndex];
                    int distanceKm = travelDistances[originLocationIndex][destinationLocationIndex];
                    travelInfoCsv.Add(new TravelInfoCsv(location1.Name, location2.Name, durationMinutes, distanceKm));
                }
            }

            WriteCsvFile(travelInfoCsv, csvFilePath);
        }

        static void WriteCsvFile(List<TravelInfoCsv> travelInfoCsv, string csvFilePath) {
            using StreamWriter streamWriter = new StreamWriter(csvFilePath);
            using CsvWriter csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
            csvWriter.WriteRecords(travelInfoCsv);
        }


        /* Reading */

        public static List<TravelInfoCsv> ImportTravelInfoFromCsv(bool shouldIgnoreEmpty, string csvFilePath) {
            List<TravelInfoCsv> travelInfoCsv;
            if (File.Exists(csvFilePath)) {
                using StreamReader streamReader = new StreamReader(csvFilePath);
                using CsvReader csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
                travelInfoCsv = csvReader.GetRecords<TravelInfoCsv>().ToList();
            } else if (shouldIgnoreEmpty) {
                Console.WriteLine("File `{0}` not found\nWill generate relevant travel info", csvFilePath);
                travelInfoCsv = new List<TravelInfoCsv>();
            } else {
                throw new Exception(string.Format("File `{0}` not found", csvFilePath));
            }

            return travelInfoCsv;
        }
    }

    // Type to export travel into CSV using CsvHelper.WriteRecords
    public class TravelInfoCsv {
        #pragma warning disable IDE1006 // Disable naming styles warning
        public string location1Name { get; set; }
        public string location2Name { get; set; }
        public int travelTimeMinutes { get; set; }
        public int travelDistanceKilometers { get; set; }
        #pragma warning restore IDE1006 // Re-enable naming styles warning

        public TravelInfoCsv(string location1Name, string location2Name, int travelTimeMinutes, int travelDistanceKilometers) {
            this.location1Name = location1Name;
            this.location2Name = location2Name;
            this.travelTimeMinutes = travelTimeMinutes;
            this.travelDistanceKilometers = travelDistanceKilometers;
        }
    }
}

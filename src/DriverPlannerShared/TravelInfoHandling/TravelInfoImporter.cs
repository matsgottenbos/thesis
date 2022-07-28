/*
 * Helper methods to import travel info
*/

using System.Collections.Generic;

namespace DriverPlannerShared {
    public static class TravelInfoImporter {
        /* Import full info */

        public static (int[,], int[,], string[]) ImportFullyConnectedTravelInfo(string csvFileName, bool shouldIgnoreEmpty = false) {
            List<TravelInfoCsv> travelInfoCsv = TravelInfoCsvHandler.ImportTravelInfoFromCsv(shouldIgnoreEmpty, csvFileName);

            // Get location names
            List<string> locationNames = new List<string>();
            for (int travelInfoIndex = 0; travelInfoIndex < travelInfoCsv.Count; travelInfoIndex++) {
                TravelInfoCsv travelInfo = travelInfoCsv[travelInfoIndex];
                if (!locationNames.Contains(travelInfo.location1Name)) locationNames.Add(travelInfo.location1Name);
                if (!locationNames.Contains(travelInfo.location2Name)) locationNames.Add(travelInfo.location2Name);
            }

            // Create travel time objects
            int[,] travelTimes = new int[locationNames.Count, locationNames.Count];
            int[,] travelDistances = new int[locationNames.Count, locationNames.Count];

            // Set time and distance to self to 0
            for (int locationIndex = 0; locationIndex < locationNames.Count; locationIndex++) {
                travelTimes[locationIndex, locationIndex] = 0;
                travelDistances[locationIndex, locationIndex] = 0;
            }

            // Set imported times and distances
            for (int travelInfoIndex = 0; travelInfoIndex < travelInfoCsv.Count; travelInfoIndex++) {
                TravelInfoCsv travelInfo = travelInfoCsv[travelInfoIndex];

                int location1Index = locationNames.IndexOf(travelInfo.location1Name);
                int location2Index = locationNames.IndexOf(travelInfo.location2Name);
                if (location1Index == -1 || location2Index == -1) continue;

                travelTimes[location1Index, location2Index] = travelInfo.travelTimeMinutes;
                travelTimes[location2Index, location1Index] = travelInfo.travelTimeMinutes;
                travelDistances[location1Index, location2Index] = travelInfo.travelDistanceKilometers;
                travelDistances[location2Index, location1Index] = travelInfo.travelDistanceKilometers;
            }

            return (travelTimes, travelDistances, locationNames.ToArray());
        }

        public static (int[][], int[][], string[], string[]) ImportBipartiteTravelInfo(string csvFileName, bool shouldIgnoreEmpty = false) {
            List<TravelInfoCsv> travelInfoCsv = TravelInfoCsvHandler.ImportTravelInfoFromCsv(shouldIgnoreEmpty, csvFileName);

            // Get location names
            List<string> originLocationNames = new List<string>();
            List<string> destinationLocationNames = new List<string>();
            for (int travelInfoIndex = 0; travelInfoIndex < travelInfoCsv.Count; travelInfoIndex++) {
                TravelInfoCsv travelInfo = travelInfoCsv[travelInfoIndex];
                if (!originLocationNames.Contains(travelInfo.location1Name)) originLocationNames.Add(travelInfo.location1Name);
                if (!destinationLocationNames.Contains(travelInfo.location2Name)) destinationLocationNames.Add(travelInfo.location2Name);
            }

            // Create travel time objects
            int[][] travelTimes = GetInstantiatedJaggedArray<int>(originLocationNames.Count, destinationLocationNames.Count);
            int[][] travelDistances = GetInstantiatedJaggedArray<int>(originLocationNames.Count, destinationLocationNames.Count);

            // Set imported times and distances
            for (int travelInfoIndex = 0; travelInfoIndex < travelInfoCsv.Count; travelInfoIndex++) {
                TravelInfoCsv travelInfo = travelInfoCsv[travelInfoIndex];

                int originLocationIndex = originLocationNames.IndexOf(travelInfo.location1Name);
                int destinationLocationIndex = destinationLocationNames.IndexOf(travelInfo.location2Name);
                if (originLocationIndex == -1 || destinationLocationIndex == -1) continue;

                travelTimes[originLocationIndex][destinationLocationIndex] = travelInfo.travelTimeMinutes;
                travelDistances[originLocationIndex][destinationLocationIndex] = travelInfo.travelDistanceKilometers;
            }

            return (travelTimes, travelDistances, originLocationNames.ToArray(), destinationLocationNames.ToArray());
        }


        /* Import partial info */

        /** Returns 2D arrays of nullable travel times and distances for all given locations, filled in with imported data. Also returns missing locations names in the imported data. */
        public static (int?[,], int?[,], List<LocationInfo>) ImportPartialFullyConnectedTravelInfo(List<LocationInfo> locations, string csvFileName) {
            (int[,] importedTravelTimes, int[,] importedTravelDistances, string[] importedLocationNames) = ImportFullyConnectedTravelInfo(csvFileName, true);

            // Get objects of imported locations
            List<LocationInfo> importedLocations = GetImportedLocations(importedLocationNames, locations);

            // Create partial arrays
            int?[,] partialTravelTimes = new int?[locations.Count, locations.Count];
            int?[,] partialTravelDistances = new int?[locations.Count, locations.Count];
            for (int importedLocation1Index = 0; importedLocation1Index < importedLocations.Count; importedLocation1Index++) {
                LocationInfo importedLocation1 = importedLocations[importedLocation1Index];
                if (importedLocation1 == null) continue;
                for (int importedLocation2Index = 0; importedLocation2Index < importedLocations.Count; importedLocation2Index++) {
                    LocationInfo importedLocation2 = importedLocations[importedLocation2Index];
                    if (importedLocation2 == null) continue;

                    partialTravelTimes[importedLocation1.Index, importedLocation2.Index] = importedTravelTimes[importedLocation1Index, importedLocation2Index];
                    partialTravelDistances[importedLocation1.Index, importedLocation2.Index] = importedTravelDistances[importedLocation1Index, importedLocation2Index];
                }
            }

            // Set time and distance to self to 0
            for (int locationIndex = 0; locationIndex < locations.Count; locationIndex++) {
                partialTravelTimes[locationIndex, locationIndex] = 0;
                partialTravelDistances[locationIndex, locationIndex] = 0;
            }

            // Get list of missing locations
            List<LocationInfo> missingLocations = new List<LocationInfo>(locations);
            missingLocations.RemoveAll(location => importedLocations.Contains(location));

            return (partialTravelTimes, partialTravelDistances, missingLocations);
        }

        public static (int?[][], int?[][], List<LocationInfo>, List<LocationInfo>) ImportPartialBipartiteTravelInfo(List<LocationInfo> originLocations, List<LocationInfo> destinationLocations, string csvFileName) {
            (int[][] importedTravelTimes, int[][] importedTravelDistances, string[] importedOriginLocationNames, string[] importedDestinationLocationNames) = ImportBipartiteTravelInfo(csvFileName, true);

            // Get objects of imported locations
            List<LocationInfo> importedOriginLocations = GetImportedLocations(importedOriginLocationNames, originLocations);
            List<LocationInfo> importedDestinationLocations = GetImportedLocations(importedDestinationLocationNames, destinationLocations);

            // Create partial arrays
            int?[][] partialTravelTimes = GetInstantiatedJaggedArray<int?>(originLocations.Count, destinationLocations.Count);
            int?[][] partialTravelDistances = GetInstantiatedJaggedArray<int?>(originLocations.Count, destinationLocations.Count);
            for (int importedOriginLocationIndex = 0; importedOriginLocationIndex < importedOriginLocations.Count; importedOriginLocationIndex++) {
                LocationInfo importedOriginLocation = importedOriginLocations[importedOriginLocationIndex];
                if (importedOriginLocation == null) continue;
                for (int importedDestinationLocationIndex = 0; importedDestinationLocationIndex < importedDestinationLocations.Count; importedDestinationLocationIndex++) {
                    LocationInfo importedDestinationLocation = importedDestinationLocations[importedDestinationLocationIndex];
                    if (importedDestinationLocation == null) continue;

                    partialTravelTimes[importedOriginLocation.Index][importedDestinationLocation.Index] = importedTravelTimes[importedOriginLocationIndex][importedDestinationLocationIndex];
                    partialTravelDistances[importedOriginLocation.Index][importedDestinationLocation.Index] = importedTravelDistances[importedOriginLocationIndex][importedDestinationLocationIndex];
                }
            }

            // Get list of missing locations
            List<LocationInfo> missingOriginLocations = new List<LocationInfo>(originLocations);
            missingOriginLocations.RemoveAll(location => importedOriginLocations.Contains(location));
            List<LocationInfo> missingDestinationLocations = new List<LocationInfo>(destinationLocations);
            missingDestinationLocations.RemoveAll(location => importedDestinationLocations.Contains(location));

            return (partialTravelTimes, partialTravelDistances, missingOriginLocations, missingDestinationLocations);
        }

        static List<LocationInfo> GetImportedLocations(string[] importedLocationNames, List<LocationInfo> locations) {
            List<LocationInfo> importedLocations = new List<LocationInfo>();
            for (int importedLocationIndex = 0; importedLocationIndex < importedLocationNames.Length; importedLocationIndex++) {
                int allLocationsIndex = locations.FindIndex(location => location.Name == importedLocationNames[importedLocationIndex]);
                if (allLocationsIndex == -1) {
                    importedLocations.Add(null);
                } else {
                    importedLocations.Add(locations[allLocationsIndex]);
                }
            }
            return importedLocations;
        }

        static T[][] GetInstantiatedJaggedArray<T>(int length1, int length2) {
            T[][] arr = new T[length1][];
            for (int i = 0; i < length1; i++) {
                arr[i] = new T[length2];
            }
            return arr;
        }
    }
}

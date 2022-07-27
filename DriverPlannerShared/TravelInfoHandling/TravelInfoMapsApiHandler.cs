/*
 * Helper methods for Google Maps API requests
*/

using Google.Maps;
using Google.Maps.DistanceMatrix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public static class TravelInfoMapsApiHandler {
        public static bool AddMissingFullyConnectedTravelInfo(List<LocationInfo> missingLocations, List<LocationInfo> allLocations, int?[,] importedTravelTimes, int?[,] importedTravelDistances) {
            return AddMissingTravelInfoFromMaps(missingLocations, allLocations, (originLocationIndex, destionationLocationIndex, durationMinutes, distanceKm) => {
                importedTravelTimes[originLocationIndex, destionationLocationIndex] = durationMinutes;
                importedTravelTimes[destionationLocationIndex, originLocationIndex] = durationMinutes;
                importedTravelDistances[originLocationIndex, destionationLocationIndex] = distanceKm;
                importedTravelDistances[destionationLocationIndex, originLocationIndex] = distanceKm;
            });
        }

        public static bool AddMissingBipartiteTravelInfo(List<LocationInfo> missingOriginLocations, List<LocationInfo> allOriginLocations, List<LocationInfo> missingDestinationLocations, List<LocationInfo> allDestinationLocations, int?[][] importedTravelTimes, int?[][] importedTravelDistances) {
            bool isSuccess = AddMissingTravelInfoFromMaps(missingOriginLocations, allDestinationLocations, (originLocationIndex, destionationLocationIndex, durationMinutes, distanceKm) => {
                importedTravelTimes[originLocationIndex][destionationLocationIndex] = durationMinutes;
                importedTravelDistances[originLocationIndex][destionationLocationIndex] = distanceKm;
            });
            if (!isSuccess) return false;

            List<LocationInfo> knownOriginStations = new List<LocationInfo>(allOriginLocations);
            knownOriginStations.RemoveAll(location => missingOriginLocations.Contains(location));

            return AddMissingTravelInfoFromMaps(knownOriginStations, missingDestinationLocations, (originLocationIndex, destionationLocationIndex, durationMinutes, distanceKm) => {
                importedTravelTimes[originLocationIndex][destionationLocationIndex] = durationMinutes;
                importedTravelDistances[originLocationIndex][destionationLocationIndex] = distanceKm;
            });
        }

        static bool AddMissingTravelInfoFromMaps(List<LocationInfo> originLocations, List<LocationInfo> destinationLocations, Action<int, int, int, int> storeTravelInfoFunc) {
            if (originLocations.Count == 0 || destinationLocations.Count == 0) return true;

            Console.WriteLine("Performing Google Maps API requests...");

            // Set API key
            GoogleSigned.AssignAllServices(new GoogleSigned(AppConfig.GoogleMapsApiKey));

            // API limits per request: max 25 origins, max 25 destinations, max 100 elements
            // Perform separate calls for each origin
            for (int missingStationIndex = 0; missingStationIndex < originLocations.Count; missingStationIndex++) {
                int allStationsGroupCount = (int)Math.Ceiling((float)destinationLocations.Count / AppConfig.GoogleMapsMaxDestinationCountPerRequest);
                List<LocationInfo> requestOriginLocations = originLocations.GetRange(missingStationIndex, 1);

                // Split destinations into groups if needed to avoid exceeding request size limits
                for (int allStationsGroupIndex = 0; allStationsGroupIndex < allStationsGroupCount; allStationsGroupIndex++) {
                    int requestDestinationsFirstIndex = allStationsGroupIndex * AppConfig.GoogleMapsMaxDestinationCountPerRequest;
                    int requestDestinationsNextIndex = Math.Min((allStationsGroupIndex + 1) * AppConfig.GoogleMapsMaxDestinationCountPerRequest, destinationLocations.Count);
                    int requestDestinationCount = requestDestinationsNextIndex - requestDestinationsFirstIndex;

                    List<LocationInfo> requestDestinationLocations = destinationLocations.GetRange(requestDestinationsFirstIndex, requestDestinationCount);

                    bool isSuccess = PerformMapsCallAndStoreTravelInfo(requestOriginLocations, requestDestinationLocations, storeTravelInfoFunc);
                    if (!isSuccess) return false;
                }
            }

            Console.WriteLine("Requests complete.");

            return true;
        }

        static bool PerformMapsCallAndStoreTravelInfo(List<LocationInfo> originLocations, List<LocationInfo> destinationLocations, Action<int, int, int, int> storeTravelInfoFunc) {
            DistanceMatrixRequest request = new DistanceMatrixRequest();
            for (int originIndex = 0; originIndex < originLocations.Count; originIndex++) {
                request.AddOrigin(originLocations[originIndex].Address);
            }
            for (int destinationIndex = 0; destinationIndex < destinationLocations.Count; destinationIndex++) {
                request.AddDestination(destinationLocations[destinationIndex].Address);
            }

            DistanceMatrixResponse response;
            try {
                 response = new DistanceMatrixService().GetResponse(request);
            } catch {
                Console.WriteLine("Request failed with unknown error.");
                return false;
            }

            if (response.Status != ServiceResponseStatus.Ok) {
                Console.WriteLine("Request failed with status `{0}` and error message `{1}`.", response.Status, response.ErrorMessage);
                return false;
            }

            if (response.Rows.Length == 0) {
                Console.WriteLine("Request returned no results.");
                return false;
            }

            // Store travel info
            for (int originIndex = 0; originIndex < response.Rows.Length; originIndex++) {
                LocationInfo originLocation = originLocations[originIndex];
                DistanceMatrixResponse.DistanceMatrixRows row = response.Rows[originIndex];

                for (int destinationIndex = 0; destinationIndex < row.Elements.Length; destinationIndex++) {
                    LocationInfo destinationLocation = destinationLocations[destinationIndex];
                    if (originLocation == destinationLocation) continue;

                    DistanceMatrixResponse.DistanceMatrixElement cell = row.Elements[destinationIndex];
                    int durationMinutes = (int)Math.Round((float)cell.duration.Value / 60);
                    int distanceKm = (int)Math.Round((float)cell.distance.Value / 1000);
                    
                    storeTravelInfoFunc(originLocation.Index, destinationLocation.Index, durationMinutes, distanceKm);
                }
            }

            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class DataGenerator {
        public static Instance GenerateInstance(XorShiftRandom rand) {
            // Travel times
            int[,] trainTravelTimes = GenerateTrainTravelTimes(Config.GenStationCount, rand);
            int[,] carTravelTimes = GenerateCarTravelTimes(Config.GenStationCount, trainTravelTimes, rand);

            // Raw trips
            Trip[] rawTrips = GenerateRawTrips(Config.GenTripCount, Config.GenStationCount, trainTravelTimes, rand);
            string[] stationCodes = new string[Config.GenStationCount];

            // Driver data
            string[] internalDriverNames = new string[Config.GenInternalDriverCount]; // Names will be empty in generated data
            int[][] internalDriversHomeTravelTimes = GenerateInternalDriverHomeTravelTimes(Config.GenInternalDriverCount, Config.GenStationCount, rand);
            bool[][,] internalDriverTrackProficiencies = GenerateInternalDriverTrackProficiencies(Config.GenInternalDriverCount, Config.GenStationCount, rand);
            int[] externalDriverCounts = GenerateExternalDriverCounts(Config.GenExternaDriverTypeCount, Config.GenExternalDriverMinCountPerType, Config.GenExternalDriverMaxCountPerType, rand);
            int[][] externalDriversHomeTravelTimes = GenerateExternalDriverHomeTravelTimes(Config.GenExternaDriverTypeCount, Config.GenStationCount, rand);

            return new Instance(rand, rawTrips, stationCodes, carTravelTimes, internalDriverNames, internalDriversHomeTravelTimes, internalDriverTrackProficiencies, Config.GenInternalDriverContractTime, externalDriverCounts, externalDriversHomeTravelTimes);
        }

        public static int[,] GenerateTrainTravelTimes(int stationCount, XorShiftRandom rand) {
            int[,] trainTravelTimes = new int[stationCount, stationCount];
            for (int i = 0; i < stationCount; i++) {
                for (int j = i; j < stationCount; j++) {
                    if (i == j) continue;

                    // Train travel times are randomly generated within [minDist, maxDist]
                    int trainTravelTime = (int)(rand.NextDouble() * (Config.GenMaxStationTravelTime - Config.GenMinStationTravelTime) + Config.GenMinStationTravelTime);
                    trainTravelTimes[i, j] = trainTravelTime;
                    trainTravelTimes[j, i] = trainTravelTime;
                }
            }
            return trainTravelTimes;
        }

        public static int[,] GenerateCarTravelTimes(int stationCount, int[,] trainTravelTimes, XorShiftRandom rand) {
            int[,] carTravelTimes = new int[stationCount, stationCount];
            for (int i = 0; i < stationCount; i++) {
                for (int j = i; j < stationCount; j++) {
                    if (i == j) continue;

                    // Car travel times are randomly generated within specified factors of the train travel times
                    float carTravelTimeFactor = (float)rand.NextDouble() * (Config.GenMaxCarTravelTimeFactor - Config.GenMinCarTravelTimeFactor) + Config.GenMinCarTravelTimeFactor;
                    int carTravelTime = (int)(trainTravelTimes[i, j] * carTravelTimeFactor);
                    if (carTravelTime % 2 > 0) carTravelTime++; // Ensure car travel times are even, since we will half it sometimes
                    carTravelTimes[i, j] = carTravelTime;
                    carTravelTimes[j, i] = carTravelTime;
                }
            }
            return carTravelTimes;
        }

        public static Trip[] GenerateRawTrips(int tripCount, int stationCount, int[,] trainTravelTimes, XorShiftRandom rand) {
            Trip[] trips = new Trip[tripCount];
            for (int tripIndex = 0; tripIndex < tripCount; tripIndex++) {
                // Start and end station
                int startStationIndex = rand.Next(stationCount);
                int endStationIndex;
                do {
                    endStationIndex = rand.Next(stationCount);
                } while (endStationIndex == startStationIndex);

                // Start and end time
                int tripDuration = trainTravelTimes[startStationIndex, endStationIndex];
                int startTime = (int)(rand.NextDouble() * (Config.GenTimeframeLength - tripDuration));
                int endTime = startTime + tripDuration;

                Trip trip = new Trip("", "", "", "", startStationIndex, endStationIndex, startTime, endTime, tripDuration);
                trips[tripIndex] = trip;
            }
            return trips;
        }

        public static int[][] GenerateInternalDriverHomeTravelTimes(int internalDriverCount, int stationCount, XorShiftRandom rand) {
            int[][] internalDriversHomeTravelTimes = new int[internalDriverCount][];
            for (int internalDriverIndex = 0; internalDriverIndex < internalDriverCount; internalDriverIndex++) {
                int[] homeTravelTimes = new int[stationCount];
                for (int i = 0; i < stationCount; i++) {
                    homeTravelTimes[i] = rand.Next(Config.GenMaxHomeTravelTime + 1);
                }
                internalDriversHomeTravelTimes[internalDriverIndex] = homeTravelTimes;
            }
            return internalDriversHomeTravelTimes;
        }

        public static bool[][,] GenerateInternalDriverTrackProficiencies(int internalDriverCount, int stationCount, XorShiftRandom rand) {
            bool[][,] internalDriverTrackProficiencies = new bool[internalDriverCount][,];
            for (int internalDriverIndex = 0; internalDriverIndex < internalDriverCount; internalDriverIndex++) {
                bool[,] driverTrackProficiencies = new bool[stationCount, stationCount];
                for (int i = 0; i < stationCount; i++) {
                    for (int j = i; j < stationCount; j++) {
                        bool isProficient;
                        if (i == j) {
                            isProficient = true;
                        } else {
                            isProficient = rand.NextDouble() < Config.GenTrackProficiencyProb;
                        }

                        driverTrackProficiencies[i, j] = isProficient;
                        driverTrackProficiencies[j, i] = isProficient;
                    }
                }
                internalDriverTrackProficiencies[internalDriverIndex] = driverTrackProficiencies;
            }
            return internalDriverTrackProficiencies;
        }

        public static int[] GenerateExternalDriverCounts(int externalDriverTypeCount, int minCountPerType, int maxCountPerType, XorShiftRandom rand) {
            int[] externalDriverCounts = new int[externalDriverTypeCount];
            for (int externalDriverIndex = 0; externalDriverIndex < externalDriverTypeCount; externalDriverIndex++) {
                int count = rand.Next(minCountPerType, maxCountPerType + 1);
                externalDriverCounts[externalDriverIndex] = count;
            }
            return externalDriverCounts;
        }

        public static int[][] GenerateExternalDriverHomeTravelTimes(int externalDriverTypeCount, int stationCount, XorShiftRandom rand) {
            int[][] externalDriversHomeTravelTimes = new int[externalDriverTypeCount][];
            for (int externalDriverIndex = 0; externalDriverIndex < externalDriverTypeCount; externalDriverIndex++) {
                int[] homeTravelTimes = new int[stationCount];
                for (int i = 0; i < stationCount; i++) {
                    homeTravelTimes[i] = rand.Next(Config.GenMaxHomeTravelTime + 1);
                }
                externalDriversHomeTravelTimes[externalDriverIndex] = homeTravelTimes;
            }
            return externalDriversHomeTravelTimes;
        }
    }
}

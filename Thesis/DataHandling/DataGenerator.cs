using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class DataGenerator {
        public static int[,] GenerateCarTravelTimes(int stationCount, int[,] trainTravelTimes, XorShiftRandom rand) {
            int[,] carTravelTimes = new int[stationCount, stationCount];
            for (int i = 0; i < stationCount; i++) {
                for (int j = i; j < stationCount; j++) {
                    if (i == j) continue;

                    // Car travel times are randomly generated within specified factors of the train travel times
                    float carTravelTimeFactor = (float)rand.NextDouble() * (DataConfig.GenMaxCarTravelTimeFactor - DataConfig.GenMinCarTravelTimeFactor) + DataConfig.GenMinCarTravelTimeFactor;
                    int carTravelTime = (int)(trainTravelTimes[i, j] * carTravelTimeFactor);
                    if (carTravelTime % 2 > 0) carTravelTime++; // Ensure car travel times are even, since we will half it sometimes
                    carTravelTimes[i, j] = carTravelTime;
                    carTravelTimes[j, i] = carTravelTime;
                }
            }
            return carTravelTimes;
        }

        public static int[][] GenerateInternalDriverHomeTravelTimes(int internalDriverCount, int stationCount, XorShiftRandom rand) {
            int[][] internalDriversHomeTravelTimes = new int[internalDriverCount][];
            for (int internalDriverIndex = 0; internalDriverIndex < internalDriverCount; internalDriverIndex++) {
                int[] homeTravelTimes = new int[stationCount];
                for (int i = 0; i < stationCount; i++) {
                    homeTravelTimes[i] = rand.Next(DataConfig.GenMaxHomeTravelTime + 1);
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
                            isProficient = rand.NextDouble() < DataConfig.GenTrackProficiencyProb;
                        }

                        driverTrackProficiencies[i, j] = isProficient;
                        driverTrackProficiencies[j, i] = isProficient;
                    }
                }
                internalDriverTrackProficiencies[internalDriverIndex] = driverTrackProficiencies;
            }
            return internalDriverTrackProficiencies;
        }

        public static int[][] GenerateExternalDriverHomeTravelTimes(int externalDriverTypeCount, int stationCount, XorShiftRandom rand) {
            int[][] externalDriversHomeTravelTimes = new int[externalDriverTypeCount][];
            for (int externalDriverIndex = 0; externalDriverIndex < externalDriverTypeCount; externalDriverIndex++) {
                int[] homeTravelTimes = new int[stationCount];
                for (int i = 0; i < stationCount; i++) {
                    homeTravelTimes[i] = rand.Next(DataConfig.GenMaxHomeTravelTime + 1);
                }
                externalDriversHomeTravelTimes[externalDriverIndex] = homeTravelTimes;
            }
            return externalDriversHomeTravelTimes;
        }
    }
}

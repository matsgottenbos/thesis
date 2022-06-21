using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class DataGenerator {
        public static bool[,] GenerateInternalDriverTrackProficiencies(int stationCount, XorShiftRandom rand) {
            bool[,] trackProficiencies = new bool[stationCount, stationCount];
            for (int i = 0; i < stationCount; i++) {
                for (int j = i; j < stationCount; j++) {
                    bool isProficient;
                    if (i == j) {
                        isProficient = true;
                    } else {
                        isProficient = rand.NextDouble() < DataConfig.GenTrackProficiencyProb;
                    }

                    trackProficiencies[i, j] = isProficient;
                    trackProficiencies[j, i] = isProficient;
                }
            }
            return trackProficiencies;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class SaInfo {
        public readonly Instance Instance;
        public readonly Random Rand;
        public readonly XorShiftRandom FastRand;
        public Driver[] Assignment;
        public List<Trip>[] DriverPaths;
        public bool[] IsHotelStayAfterTrip;
        public double Cost, CostWithoutPenalty, Penalty, Satisfaction;
        public int[] DriverPathIndices, ExternalDriverCountsByType;
        public DriverInfo[] DriverInfos;
        public PenaltyInfo PenaltyInfo;
        public int IterationNum, CycleNum;
        public float Temperature, SatisfactionFactor;

        public SaInfo(Instance instance, Random rand, XorShiftRandom fastRand) {
            Instance = instance;
            Rand = rand;
            FastRand = fastRand;
        }

        public void ReassignTrip(Trip trip, Driver oldDriver, Driver newDriver) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                DebugCheckDriverPaths();
            }
            #endif

            // Update assignment
            Assignment[trip.Index] = newDriver;

            // Remove from old driver path
            List<Trip> oldDriverPath = DriverPaths[oldDriver.AllDriversIndex];
            int tripOldPathIndex = DriverPathIndices[trip.Index];
            oldDriverPath.RemoveAt(tripOldPathIndex);
            for (int i = tripOldPathIndex; i < oldDriverPath.Count; i++) {
                DriverPathIndices[oldDriverPath[i].Index]--;
            }

            // Add to new driver path
            List<Trip> newDriverPath = DriverPaths[newDriver.AllDriversIndex];
            int pathInsertIndex;
            for (pathInsertIndex = 0; pathInsertIndex < newDriverPath.Count; pathInsertIndex++) {
                if (newDriverPath[pathInsertIndex].Index > trip.Index) break;
            }
            newDriverPath.Insert(pathInsertIndex, trip);
            DriverPathIndices[trip.Index] = pathInsertIndex;
            for (int j = pathInsertIndex + 1; j < newDriverPath.Count; j++) {
                DriverPathIndices[newDriverPath[j].Index]++;
            }

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                DebugCheckDriverPaths();
            }
            #endif
        }

        public void DebugCheckDriverPaths() {
            for (int tripIndex = 0; tripIndex < Instance.Trips.Length; tripIndex++) {
                Trip debugTrip = Instance.Trips[tripIndex];
                Driver debugDriver = Assignment[tripIndex];
                List<Trip> debugDriverPath = DriverPaths[debugDriver.AllDriversIndex];

                if (!debugDriverPath.Contains(debugTrip)) {
                    throw new Exception(string.Format("Missing trip {0} in path of driver {1}", debugTrip.Index, debugDriver.GetId()));
                }
            }
            for (int driverIndex = 0; driverIndex < Instance.AllDrivers.Length; driverIndex++) {
                Driver debugDriver = Instance.AllDrivers[driverIndex];
                List<Trip> debugDriverPath = DriverPaths[debugDriver.AllDriversIndex];

                for (int i = 0; i < debugDriverPath.Count; i++) {
                    Trip debugTrip = debugDriverPath[i];
                    if (Assignment[debugTrip.Index] != debugDriver) {
                        throw new Exception(string.Format("Incorrect trip {0} in path of driver {1}", debugTrip.Index, debugDriver.GetId()));
                    }
                    if (DriverPathIndices[debugTrip.Index] != i) {
                        throw new Exception(string.Format("Trip {0} of driver {1} has stored path index {2} but is at index {3}", debugTrip.Index, debugDriver.GetId(), DriverPathIndices[debugTrip.Index], i));
                    }
                }
            }
        }

        public SaInfo CopyForBestInfo() {
            return new SaInfo(Instance, Rand, FastRand) {
                Cost = Cost,
                Satisfaction = Satisfaction,
                Assignment = (Driver[])Assignment.Clone(),
                IsHotelStayAfterTrip = (bool[])IsHotelStayAfterTrip.Clone(),
            };
        }
    }
}

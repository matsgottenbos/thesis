/*
 * Used to store all changing variables of the simulated annealing algorithm
*/

using System;
using System.Collections.Generic;

namespace DriverPlannerShared {
    public class SaInfo {
        public readonly Instance Instance;
        public Driver[] Assignment;
        public List<Activity>[] DriverPaths;
        public bool[] IsHotelStayAfterActivity;
        public int[] DriverPathIndices;
        public SaTotalInfo TotalInfo;
        public SaDriverInfo[] DriverInfos;
        public SaExternalDriverTypeInfo[] ExternalDriverTypeInfos;
        public long IterationNum;
        public int CycleNum;
        public long? LastImprovementIteration;
        public float Temperature, SatisfactionFactor;
        public bool HasImprovementSinceLog, HasHadFeasibleSolutionInCycle;

        public SaInfo(Instance instance) {
            Instance = instance;
        }

        public void ReassignActivity(Activity activity, Driver oldDriver, Driver newDriver) {
#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                DebugCheckDriverPaths();
            }
#endif

            // Update assignment
            Assignment[activity.Index] = newDriver;

            // Remove from old driver path
            List<Activity> oldDriverPath = DriverPaths[oldDriver.AllDriversIndex];
            int activityOldPathIndex = DriverPathIndices[activity.Index];
            oldDriverPath.RemoveAt(activityOldPathIndex);
            for (int i = activityOldPathIndex; i < oldDriverPath.Count; i++) {
                DriverPathIndices[oldDriverPath[i].Index]--;
            }

            // Add to new driver path
            List<Activity> newDriverPath = DriverPaths[newDriver.AllDriversIndex];
            int pathInsertIndex;
            for (pathInsertIndex = 0; pathInsertIndex < newDriverPath.Count; pathInsertIndex++) {
                if (newDriverPath[pathInsertIndex].Index > activity.Index) break;
            }
            newDriverPath.Insert(pathInsertIndex, activity);
            DriverPathIndices[activity.Index] = pathInsertIndex;
            for (int j = pathInsertIndex + 1; j < newDriverPath.Count; j++) {
                DriverPathIndices[newDriverPath[j].Index]++;
            }

#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                DebugCheckDriverPaths();
            }
#endif
        }

        public void DebugCheckDriverPaths() {
            for (int activityIndex = 0; activityIndex < Instance.Activities.Length; activityIndex++) {
                Activity debugActivity = Instance.Activities[activityIndex];
                Driver debugDriver = Assignment[activityIndex];
                List<Activity> debugDriverPath = DriverPaths[debugDriver.AllDriversIndex];

                if (!debugDriverPath.Contains(debugActivity)) {
                    throw new Exception(string.Format("Missing activity {0} in path of driver {1}", debugActivity.Index, debugDriver.GetId()));
                }
            }
            for (int driverIndex = 0; driverIndex < Instance.AllDrivers.Length; driverIndex++) {
                Driver debugDriver = Instance.AllDrivers[driverIndex];
                List<Activity> debugDriverPath = DriverPaths[debugDriver.AllDriversIndex];

                for (int i = 0; i < debugDriverPath.Count; i++) {
                    Activity debugActivity = debugDriverPath[i];
                    if (Assignment[debugActivity.Index] != debugDriver) {
                        throw new Exception(string.Format("Incorrect activity {0} in path of driver {1}", debugActivity.Index, debugDriver.GetId()));
                    }
                    if (DriverPathIndices[debugActivity.Index] != i) {
                        throw new Exception(string.Format("Activity {0} of driver {1} has stored path index {2} but is at index {3}", debugActivity.Index, debugDriver.GetId(), DriverPathIndices[debugActivity.Index], i));
                    }
                }
            }
        }

        public SaInfo CopyForBestInfo() {
            return new SaInfo(Instance) {
                TotalInfo = TotalInfo,
                Assignment = (Driver[])Assignment.Clone(),
                IsHotelStayAfterActivity = (bool[])IsHotelStayAfterActivity.Clone(),
            };
        }

        public void ProcessDriverPaths(bool shouldIgnoreEmpty = false) {
            DriverPaths = new List<Activity>[Instance.AllDrivers.Length];
            DriverPathIndices = new int[Instance.Activities.Length];
            for (int driverIndex = 0; driverIndex < Instance.AllDrivers.Length; driverIndex++) {
                DriverPaths[driverIndex] = new List<Activity>();
            }
            for (int activityIndex = 0; activityIndex < Instance.Activities.Length; activityIndex++) {
                Activity activity = Instance.Activities[activityIndex];
                Driver driver = Assignment[activityIndex];
                if (driver == null) {
                    if (shouldIgnoreEmpty) continue;
                    throw new Exception(string.Format("Activity {0} has no driver assigned", activityIndex));
                }

                List<Activity> driverPath = DriverPaths[driver.AllDriversIndex];
                DriverPathIndices[activity.Index] = driverPath.Count;
                driverPath.Add(activity);
            }
        }
    }
}

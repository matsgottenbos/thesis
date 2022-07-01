using MathNet.Numerics.Distributions;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class DataActivityProcessor {
        public static (Activity[], bool[,], float[,], bool[,], int, int) ProcessRawActivities(XSSFWorkbook stationAddressesBook, RawActivity[] rawActivities, string[] stationNames, int[,] expectedCarTravelTimes) {
            if (rawActivities.Length == 0) {
                throw new Exception("No activities found in timeframe");
            }

            // Sort activities by start time
            rawActivities = rawActivities.OrderBy(activity => activity.StartTime).ToArray();

            // Get dictionary mapping station names in data to their index in the address list
            Dictionary<string, int> stationDataNameToAddressIndex = GetStationDataNameToAddressIndexDict(stationAddressesBook, stationNames);

            // Create activity objects
            Activity[] activities = new Activity[rawActivities.Length];
            for (int activityIndex = 0; activityIndex < activities.Length; activityIndex++) {
                RawActivity rawActivity = rawActivities[activityIndex];

                // Get index of start and end station addresses
                if (!stationDataNameToAddressIndex.ContainsKey(rawActivity.StartStationName)) throw new Exception(string.Format("Unknown station `{0}`", rawActivity.StartStationName));
                int startStationAddressIndex = stationDataNameToAddressIndex[rawActivity.StartStationName];
                if (!stationDataNameToAddressIndex.ContainsKey(rawActivity.EndStationName)) throw new Exception(string.Format("Unknown station `{0}`", rawActivity.EndStationName));
                int endStationAddressIndex = stationDataNameToAddressIndex[rawActivity.EndStationName];

                activities[activityIndex] = new Activity(rawActivity, activityIndex, startStationAddressIndex, endStationAddressIndex);
            }

            // Get info about activity succession
            (bool[,] activitySuccession, float[,] activitySuccessionRobustness) = GetActivitySuccessionInfo(activities, expectedCarTravelTimes);

            // Preprocess whether activities could belong to the same shift
            bool[,] activitiesAreSameShift = new bool[activities.Length, activities.Length];
            for (int activity1Index = 0; activity1Index < activities.Length; activity1Index++) {
                Activity activity1 = activities[activity1Index];
                for (int activity2Index = activity1Index; activity2Index < activities.Length; activity2Index++) {
                    Activity activity2 = activities[activity2Index];
                    activitiesAreSameShift[activity1.Index, activity2.Index] = GetExpectedWaitingTime(activity1, activity2, expectedCarTravelTimes) <= SaConfig.ShiftWaitingTimeThreshold;
                }
            }

            // Timeframe length is the last end time of all activities
            int timeframeLength = 0;
            for (int activityIndex = 0; activityIndex < activities.Length; activityIndex++) {
                timeframeLength = Math.Max(timeframeLength, activities[activityIndex].EndTime);
            }

            // Set shared route indices on all activities
            int uniqueSharedRouteCount = SetActivitiesSharedRouteIndices(activities);

            return (activities, activitySuccession, activitySuccessionRobustness, activitiesAreSameShift, timeframeLength, uniqueSharedRouteCount);
        }


        /* Station name to address index dictionary */

        /** Get a dictionary that converts from station name in data to station index in address list */
        static Dictionary<string, int> GetStationDataNameToAddressIndexDict(XSSFWorkbook stationAddressesBook, string[] stationNames) {
            ExcelSheet linkingStationNamesSheet = new ExcelSheet("Linking station names", stationAddressesBook);

            Dictionary<string, int> stationDataNameToAddressIndex = new Dictionary<string, int>();
            linkingStationNamesSheet.ForEachRow(linkingStationNamesRow => {
                string dataStationName = linkingStationNamesSheet.GetStringValue(linkingStationNamesRow, "Station name in data");
                string addressStationName = linkingStationNamesSheet.GetStringValue(linkingStationNamesRow, "Station name in address list");

                int addressStationIndex = Array.IndexOf(stationNames, addressStationName);
                if (addressStationIndex == -1) {
                    throw new Exception();
                }
                stationDataNameToAddressIndex.Add(dataStationName, addressStationIndex);
            });
            return stationDataNameToAddressIndex;
        }


        /* Succession info */

        static (bool[,], float[,]) GetActivitySuccessionInfo(Activity[] activities, int[,] expectedCarTravelTimes) {
            // Create 2D bool array indicating whether activities can succeed each other without overlapping
            // Also preprocess the robustness scores of activities when used in successsion
            bool[,] activitySuccession = new bool[activities.Length, activities.Length];
            float[,] activitySuccessionRobustness = new float[activities.Length, activities.Length];
            for (int activity1Index = 0; activity1Index < activities.Length; activity1Index++) {
                Activity activity1 = activities[activity1Index];
                for (int activity2Index = 0; activity2Index < activities.Length; activity2Index++) {
                    Activity activity2 = activities[activity2Index];
                    int travelTimeBetween = expectedCarTravelTimes[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex];
                    if (activity1.EndTime + travelTimeBetween > activity2.StartTime) continue;

                    activitySuccession[activity1Index, activity2.Index] = true;

                    int expectedWaitingTime = GetExpectedWaitingTime(activity1, activity2, expectedCarTravelTimes);
                    activitySuccessionRobustness[activity1Index, activity2.Index] = GetSuccessionRobustness(activity1, activity2, activity1.Duration, expectedWaitingTime);
                }
            }

            return (activitySuccession, activitySuccessionRobustness);
        }

        static int GetExpectedWaitingTime(Activity activity1, Activity activity2, int[,] expectedCarTravelTimes) {
            return activity2.StartTime - activity1.EndTime - expectedCarTravelTimes[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex];
        }

        static float GetSuccessionRobustness(Activity activity1, Activity activity2, int plannedDuration, int waitingTime) {
            double conflictProb = GetConflictProbability(plannedDuration, waitingTime);

            bool areSameDuty = activity1.DutyId == activity2.DutyId;
            bool areSameProject = activity1.ProjectName == activity2.ProjectName && activity1.ProjectName != "";

            double robustnessCost;
            if (areSameDuty) {
                robustnessCost = conflictProb * RulesConfig.RobustnessCostFactorSameDuty;
            } else {
                if (areSameProject) {
                    robustnessCost = conflictProb * RulesConfig.RobustnessCostFactorSameProject;
                } else {
                    robustnessCost = conflictProb * RulesConfig.RobustnessCostFactorDifferentProject;
                }
            }

            return (float)robustnessCost;
        }

        static double GetConflictProbability(int plannedDuration, int waitingTime) {
            double meanDelay = RulesConfig.ActivityMeanDelayFunc(plannedDuration);
            double delayAlpha = RulesConfig.ActivityDelayGammaDistributionAlphaFunc(meanDelay);
            double delayBeta = RulesConfig.ActivityDelayGammaDistributionBetaFunc(meanDelay);
            float delayProb = RulesConfig.ActivityDelayProbability;
            double conflictProbWhenDelayed = 1 - Gamma.CDF(delayAlpha, delayBeta, waitingTime);
            double conflictProb = delayProb * conflictProbWhenDelayed;
            return conflictProb;
        }


        /* Shared route indices */

        static int SetActivitiesSharedRouteIndices(Activity[] activities) {
            // Determine list of unique activity routes
            List<(int, int, int)> routeCounts = new List<(int, int, int)>();
            for (int activityIndex = 0; activityIndex < activities.Length; activityIndex++) {
                Activity activity = activities[activityIndex];
                if (activity.StartStationAddressIndex == activity.EndStationAddressIndex) continue;
                (int lowStationIndex, int highStationIndex) = GetLowHighStationIndices(activity);

                bool isExistingRoute = routeCounts.Any(route => route.Item1 == lowStationIndex && route.Item2 == highStationIndex);
                if (isExistingRoute) {
                    int routeIndex = routeCounts.FindIndex(route => route.Item1 == lowStationIndex && route.Item2 == highStationIndex);
                    routeCounts[routeIndex] = (routeCounts[routeIndex].Item1, routeCounts[routeIndex].Item2, routeCounts[routeIndex].Item3 + 1);
                } else {
                    routeCounts.Add((activity.StartStationAddressIndex, activity.EndStationAddressIndex, 1));
                }
            }

            // Store indices of shared routes for activities
            List<(int, int, int)> sharedRouteCounts = routeCounts.FindAll(route => route.Item3 > 1);
            for (int activityIndex = 0; activityIndex < activities.Length; activityIndex++) {
                Activity activity = activities[activityIndex];
                (int lowStationIndex, int highStationIndex) = GetLowHighStationIndices(activity);

                int sharedRouteIndex = sharedRouteCounts.FindIndex(route => route.Item1 == lowStationIndex && route.Item2 == highStationIndex);
                if (sharedRouteIndex != -1) {
                    activity.SetSharedRouteIndex(sharedRouteIndex);
                }
            }
            int uniqueSharedRouteCount = sharedRouteCounts.Count;
            return uniqueSharedRouteCount;
        }

        static (int, int) GetLowHighStationIndices(Activity activity) {
            int lowStationIndex, highStationIndex;
            if (activity.StartStationAddressIndex < activity.EndStationAddressIndex) {
                lowStationIndex = activity.StartStationAddressIndex;
                highStationIndex = activity.EndStationAddressIndex;
            } else {
                lowStationIndex = activity.EndStationAddressIndex;
                highStationIndex = activity.StartStationAddressIndex;
            }
            return (lowStationIndex, highStationIndex);
        }
    }
}

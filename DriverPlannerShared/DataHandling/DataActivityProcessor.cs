using MathNet.Numerics.Distributions;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public static class DataActivityProcessor {
        public static (Activity[], bool[,], float[,], bool[,], int, int) ProcessRawActivities(XSSFWorkbook stationAddressesBook, RawActivity[] rawActivities, string[] stationNames, string[] borderStationNames, string[] borderRegionStationNames, int[,] expectedCarTravelTimes) {
            Console.WriteLine("Found {0} activities in timeframe", rawActivities.Length);

            // Sort activities by start time
            rawActivities = rawActivities.OrderBy(activity => activity.StartTime).ToArray();

            // Remove duplicate activities
            rawActivities = RemoveDuplicateRawActivities(rawActivities);
            rawActivities = CombineBorderRawActivities(rawActivities, borderStationNames, borderRegionStationNames);

            // Get dictionary mapping station names in data to their index in the address list
            Dictionary<string, int> stationDataNameToAddressIndex = GetStationDataNameToAddressIndexDict(stationAddressesBook, stationNames);

            // Create activity objects
            List<Activity> activitiesList = new List<Activity>();
            List<string> missingStations = new List<string>();
            int skippedActivitiesForMissingStationsCount = 0;
            for (int rawActivityIndex = 0; rawActivityIndex < rawActivities.Length; rawActivityIndex++) {
                RawActivity rawActivity = rawActivities[rawActivityIndex];

                // Get index of start and end station addresses
                if (!stationDataNameToAddressIndex.ContainsKey(rawActivity.StartStationName)) {
                    if (!missingStations.Contains(rawActivity.StartStationName)) missingStations.Add(rawActivity.StartStationName);
                    skippedActivitiesForMissingStationsCount++;
                    continue;
                }
                int startStationAddressIndex = stationDataNameToAddressIndex[rawActivity.StartStationName];
                if (!stationDataNameToAddressIndex.ContainsKey(rawActivity.EndStationName)) {
                    if (!missingStations.Contains(rawActivity.EndStationName)) missingStations.Add(rawActivity.EndStationName);
                    skippedActivitiesForMissingStationsCount++;
                    continue;
                }
                int endStationAddressIndex = stationDataNameToAddressIndex[rawActivity.EndStationName];

                activitiesList.Add(new Activity(rawActivity, activitiesList.Count, startStationAddressIndex, endStationAddressIndex));
            }
            if (missingStations.Count > 0) {
                Console.WriteLine("Skipping {0} activities with unknown stations: {1}", skippedActivitiesForMissingStationsCount, string.Join(", ", missingStations));
            }
            Activity[] activities = activitiesList.ToArray();

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

        static RawActivity[] RemoveDuplicateRawActivities(RawActivity[] rawActivities) {
            if (DevConfig.DebugLogDataRepairs) Console.WriteLine();
            List<RawActivity> rawActivitiesWithoutDuplicatesList = new List<RawActivity>();
            int duplicateCount = 0;
            for (int activityIndex = 0; activityIndex < rawActivities.Length; activityIndex++) {
                RawActivity rawActivity = rawActivities[activityIndex];

                RawActivity duplicateActivity = rawActivitiesWithoutDuplicatesList.Find(filteredRawActivity => RawActivity.AreDetailsEqual(rawActivity, filteredRawActivity));
                if (duplicateActivity == null) {
                    rawActivitiesWithoutDuplicatesList.Add(rawActivity);
                } else {
                    duplicateCount++;
                    if (DevConfig.DebugLogDataRepairs) DebugLogRawActivity("Duplicate", rawActivity);
                }
            }

            Console.WriteLine("Ignoring {0} duplicate activities", duplicateCount);

            return rawActivitiesWithoutDuplicatesList.ToArray();
        }

        /** Combine activities start/ending at border, because those locations are not actual stations */
        static RawActivity[] CombineBorderRawActivities(RawActivity[] rawActivities, string[] borderStationNames, string[] borderRegionStationNames) {
            if (DevConfig.DebugLogDataRepairs) Console.WriteLine();
            List<RawActivity> combinedRawActivitiesList = new List<RawActivity>();
            List<RawActivity> unmatchedRawActivitesList = new List<RawActivity>();
            int combinedCountOnStations = 0;
            for (int activityIndex = 0; activityIndex < rawActivities.Length; activityIndex++) {
                RawActivity rawActivity = rawActivities[activityIndex];

                bool isStartBorder = borderStationNames.Contains(rawActivity.StartStationName);
                bool isEndBorder = borderStationNames.Contains(rawActivity.EndStationName);

                if (isStartBorder) {
                    // Check if we can combine the start of this activity with an unmatched activity
                    RawActivity rawActivityToCombine = unmatchedRawActivitesList.Find(unmatchedRawActivity => unmatchedRawActivity.DutyId == rawActivity.DutyId && unmatchedRawActivity.ActivityType == rawActivity.ActivityType && unmatchedRawActivity.EndStationName == rawActivity.StartStationName && unmatchedRawActivity.EndTime == rawActivity.StartTime);
                    if (rawActivityToCombine == null) {
                        unmatchedRawActivitesList.Add(rawActivity);
                    } else {
                        bool isCombinedStartBorderRegion = borderRegionStationNames.Contains(rawActivityToCombine.StartStationName);
                        bool isCombinedEndBorderRegion = borderRegionStationNames.Contains(rawActivity.EndStationName);

                        string[] combinedRequiredCountryQualifications;
                        if (isCombinedStartBorderRegion && isCombinedEndBorderRegion) {
                            combinedRequiredCountryQualifications = Array.Empty<string>();
                        } else if (isCombinedStartBorderRegion) {
                            combinedRequiredCountryQualifications = rawActivity.RequiredCountryQualifications;
                        } else if (isCombinedEndBorderRegion) {
                            combinedRequiredCountryQualifications = rawActivityToCombine.RequiredCountryQualifications;
                        } else {
                            List<string> combinedRequiredCountryQualificationsList = new List<string>();
                            combinedRequiredCountryQualificationsList.AddRange(rawActivityToCombine.RequiredCountryQualifications);
                            for (int i = 0; i < rawActivity.RequiredCountryQualifications.Length; i++) {
                                string countryQualification = rawActivity.RequiredCountryQualifications[i];
                                if (!combinedRequiredCountryQualificationsList.Contains(countryQualification)) {
                                    combinedRequiredCountryQualificationsList.Add(countryQualification);
                                }
                            }
                            combinedRequiredCountryQualifications = combinedRequiredCountryQualificationsList.ToArray();
                        }

                        RawActivity combinedActivity = new RawActivity(rawActivityToCombine.DutyName, rawActivityToCombine.ActivityType, rawActivityToCombine.DutyId, rawActivityToCombine.ProjectName, rawActivityToCombine.TrainNumber, rawActivityToCombine.StartStationName, rawActivity.EndStationName, combinedRequiredCountryQualifications, rawActivityToCombine.StartTime, rawActivity.EndTime, rawActivityToCombine.DataAssignedCompanyName, rawActivityToCombine.DataAssignedEmployeeName, new RawActivity[] { rawActivityToCombine, rawActivity });

                        unmatchedRawActivitesList.Remove(rawActivityToCombine);
                        if (isEndBorder) unmatchedRawActivitesList.Add(combinedActivity);
                        else combinedRawActivitiesList.Add(combinedActivity);
                        combinedCountOnStations++;

                        if (DevConfig.DebugLogDataRepairs) {
                            DebugLogRawActivity("Combined part 1", rawActivityToCombine);
                            DebugLogRawActivity("Combined part 2", rawActivity);
                        }
                    }
                } else if (isEndBorder) {
                    unmatchedRawActivitesList.Add(rawActivity);
                } else {
                    combinedRawActivitiesList.Add(rawActivity);
                }
            }

            if (DevConfig.DebugLogDataRepairs) {
                Console.WriteLine("Combined {0} pairs of activities starting/ending at borders", combinedCountOnStations);
                Console.WriteLine();
                DebugLogRawActivities("Not combined", unmatchedRawActivitesList);
            }
            Console.WriteLine("Ignoring {0} activities starting/ending at borders that could not be combined", unmatchedRawActivitesList.Count);

            return combinedRawActivitiesList.OrderBy(activity => activity.StartTime).ToArray();
        }

        static void DebugLogRawActivities(string comment, List<RawActivity> rawActivitiesList) {
            for (int i = 0; i < rawActivitiesList.Count; i++) {
                DebugLogRawActivity(comment, rawActivitiesList[i]);
            }
        }

        static void DebugLogRawActivity(string comment, RawActivity rawActivity) {
            Console.WriteLine("{0}: {1,5}-{2,5}  {3,-45} -> {4,-45}  {5,-30}  {6,-30}", comment, rawActivity.StartTime, rawActivity.EndTime, rawActivity.StartStationName, rawActivity.EndStationName, rawActivity.ActivityType, rawActivity.DutyName);
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

                    int expectedActivity1Duration = activity1.EndTime - activity1.StartTime;
                    int expectedWaitingTime = GetExpectedWaitingTime(activity1, activity2, expectedCarTravelTimes);
                    activitySuccessionRobustness[activity1Index, activity2.Index] = GetSuccessionRobustness(activity1, activity2, expectedActivity1Duration, expectedWaitingTime);
                }
            }

            return (activitySuccession, activitySuccessionRobustness);
        }

        static int GetExpectedWaitingTime(Activity activity1, Activity activity2, int[,] expectedCarTravelTimes) {
            return activity2.StartTime - activity1.EndTime - expectedCarTravelTimes[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex];
        }

        static float GetSuccessionRobustness(Activity activity1, Activity activity2, int plannedDuration, int waitingTime) {
            double conflictProb = GetConflictProbability(plannedDuration, waitingTime, activity1.ActivityType);

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

        static double GetConflictProbability(int plannedDuration, int waitingTime, string activityType) {
            bool isDrivingActivity = RulesConfig.DrivingActivityTypes.Contains(activityType);

            double meanDelay = RulesConfig.ActivityMeanDelayFunc(plannedDuration);
            double delayAlpha = RulesConfig.ActivityDelayGammaDistributionAlphaFunc(meanDelay);
            double delayBeta = RulesConfig.ActivityDelayGammaDistributionBetaFunc(meanDelay);
            float delayProb = isDrivingActivity ? RulesConfig.DrivingActivityDelayProbability : RulesConfig.NonDrivingActivityDelayProbability;
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

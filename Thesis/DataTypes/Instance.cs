using MathNet.Numerics.Distributions;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class Instance {
        readonly int timeframeLength;
        public readonly int UniqueSharedRouteCount;
        readonly int[,] plannedCarTravelTimes, expectedCarTravelTimes, carTravelDistances;
        public readonly Activity[] Activities;
        public readonly string[] StationNames;
        readonly ShiftInfo[,] shiftInfos;
        readonly float[,] activitySuccessionRobustness;
        readonly bool[,] activitySuccession, activitiesAreSameShift;
        public readonly InternalDriver[] InternalDrivers;
        public readonly ExternalDriverType[] ExternalDriverTypes;
        public readonly ExternalDriver[][] ExternalDriversByType;
        public readonly Driver[] AllDrivers, DataAssignment;

        public Instance(XorShiftRandom rand, RawActivity[] rawActivities) {
            XSSFWorkbook addressesBook = ExcelHelper.ReadExcelFile(Path.Combine(AppConfig.InputFolder, "stationAddresses.xlsx"));
            XSSFWorkbook settingsBook = ExcelHelper.ReadExcelFile(Path.Combine(AppConfig.InputFolder, "settings.xlsx"));

            (StationNames, plannedCarTravelTimes, expectedCarTravelTimes, carTravelDistances) = GetStationNamesAndExpectedCarTravelInfo();
            (Activities, activitySuccession, activitySuccessionRobustness, activitiesAreSameShift, timeframeLength, UniqueSharedRouteCount) = ProcessRawActivities(addressesBook, rawActivities, StationNames, expectedCarTravelTimes);
            shiftInfos = GetShiftInfos(Activities, timeframeLength);
            InternalDrivers = CreateInternalDrivers(settingsBook, rand);
            Dictionary<(string, bool), ExternalDriver[]> externalDriversByTypeDict;
            (ExternalDriverTypes, ExternalDriversByType, externalDriversByTypeDict) = CreateExternalDrivers(settingsBook, InternalDrivers.Length, rand);
            DataAssignment = GetDataAssignment(settingsBook, Activities, InternalDrivers, externalDriversByTypeDict);

            // Create all drivers array
            List<Driver> allDriversList = new List<Driver>();
            allDriversList.AddRange(InternalDrivers);
            for (int i = 0; i < ExternalDriversByType.Length; i++) {
                allDriversList.AddRange(ExternalDriversByType[i]);
            }
            AllDrivers = allDriversList.ToArray();

            // Pass instance object to drivers
            for (int driverIndex = 0; driverIndex < AllDrivers.Length; driverIndex++) {
                AllDrivers[driverIndex].SetInstance(this);
            }
        }

        static (string[], int[,], int[,], int[,]) GetStationNamesAndExpectedCarTravelInfo() {
            (int[,] plannedCarTravelTimes, int[,] carTravelDistances, string[] stationNames) = TravelInfoImporter.ImportFullyConnectedTravelInfo(Path.Combine(AppConfig.IntermediateFolder, "stationTravelInfo.csv"));

            int stationCount = plannedCarTravelTimes.GetLength(0);
            int[,] expectedCarTravelTimes = new int[stationCount, stationCount];
            for (int location1Index = 0; location1Index < stationCount; location1Index++) {
                for (int location2Index = location1Index; location2Index < stationCount; location2Index++) {
                    int plannedTravelTimeBetween = plannedCarTravelTimes[location1Index, location2Index];
                    int expectedTravelTimeBetween;
                    if (plannedTravelTimeBetween == 0) {
                        expectedTravelTimeBetween = 0;
                    } else {
                        expectedTravelTimeBetween = plannedTravelTimeBetween + RulesConfig.TravelDelayExpectedFunc(plannedTravelTimeBetween);
                    }
                    expectedCarTravelTimes[location1Index, location2Index] = expectedTravelTimeBetween;
                    expectedCarTravelTimes[location2Index, location1Index] = expectedTravelTimeBetween;
                }
            }

            return (stationNames, plannedCarTravelTimes, expectedCarTravelTimes, carTravelDistances);
        }

        (Activity[], bool[,], float[,], bool[,], int, int) ProcessRawActivities(XSSFWorkbook addressesBook, RawActivity[] rawActivities, string[] stationNames, int[,] expectedCarTravelTimes) {
            if (rawActivities.Length == 0) {
                throw new Exception("No activities found in timeframe");
            }

            // Sort activities by start time
            rawActivities = rawActivities.OrderBy(activity => activity.StartTime).ToArray();

            // Get dictionary mapping station names in data to their index in the address list
            Dictionary<string, int> stationDataNameToAddressIndex = GetStationDataNameToAddressIndexDict(addressesBook, stationNames);

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

            // Determine valid successors constraints
            for (int activity1Index = 0; activity1Index < activities.Length; activity1Index++) {
                for (int activity2Index = activity1Index; activity2Index < activities.Length; activity2Index++) {
                    Activity activity1 = activities[activity1Index];
                    Activity activity2 = activities[activity2Index];
                    int travelTimeBetween = expectedCarTravelTimes[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex];

                    if (activity1.EndTime + travelTimeBetween <= activity2.StartTime) {
                        activity1.AddSuccessor(activity2);
                    }
                }
            }

            // Create 2D bool array indicating whether activities can succeed each other without overlapping
            // Also preprocess the robustness scores of activities when used in successsion
            bool[,] activitySuccession = new bool[activities.Length, activities.Length];
            float[,] activitySuccessionRobustness = new float[activities.Length, activities.Length];
            for (int activityIndex = 0; activityIndex < activities.Length; activityIndex++) {
                Activity activity = activities[activityIndex];
                for (int successorIndex = 0; successorIndex < activity.Successors.Count; successorIndex++) {
                    Activity successor = activity.Successors[successorIndex];
                    activitySuccession[activityIndex, successor.Index] = true;

                    int plannedWaitingTime = ExpectedWaitingTime(activity, successor);
                    activitySuccessionRobustness[activityIndex, successor.Index] = GetSuccessionRobustness(activity, successor, activity.Duration, plannedWaitingTime);
                }
            }

            // Preprocess whether activities could belong to the same shift
            bool[,] activitiesAreSameShift = new bool[activities.Length, activities.Length];
            for (int activity1Index = 0; activity1Index < activities.Length; activity1Index++) {
                Activity activity1 = activities[activity1Index];
                for (int activity2Index = activity1Index; activity2Index < activities.Length; activity2Index++) {
                    Activity activity2 = activities[activity2Index];
                    activitiesAreSameShift[activity1.Index, activity2.Index] = ExpectedWaitingTime(activity1, activity2) <= SaConfig.ShiftWaitingTimeThreshold;
                }
            }

            // Timeframe length is the last end time of all activities
            int timeframeLength = 0;
            for (int activityIndex = 0; activityIndex < activities.Length; activityIndex++) {
                timeframeLength = Math.Max(timeframeLength, activities[activityIndex].EndTime);
            }

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

            return (activities, activitySuccession, activitySuccessionRobustness, activitiesAreSameShift, timeframeLength, uniqueSharedRouteCount);
        }

        /** Get a dictionary that converts from station name in data to station index in address list */
        static Dictionary<string, int> GetStationDataNameToAddressIndexDict(XSSFWorkbook addressesBook, string[] stationNames) {
            ExcelSheet linkingStationNamesSheet = new ExcelSheet("Linking station names", addressesBook);

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

        /** Preprocess shift driving times, night fractions and weekend fractions */
        static ShiftInfo[,] GetShiftInfos(Activity[] activities, int timeframeLength) {
            ShiftInfo[,] shiftInfos = new ShiftInfo[activities.Length, activities.Length];
            for (int firstActivityIndex = 0; firstActivityIndex < activities.Length; firstActivityIndex++) {
                for (int lastActivityIndex = 0; lastActivityIndex < activities.Length; lastActivityIndex++) {
                    Activity shiftFirstActivity = activities[firstActivityIndex];
                    Activity shiftLastActivity = activities[lastActivityIndex];

                    // Determine driving time
                    int drivingStartTime = shiftFirstActivity.StartTime;
                    int drivingEndTime = shiftLastActivity.EndTime;
                    int drivingTime = Math.Max(0, drivingEndTime - drivingStartTime);

                    // Determine driving costs for driver types
                    SalarySettings[] salarySettingsByDriverType = new SalarySettings[] {
                        SalaryConfig.InternalNationalSalaryInfo,
                        SalaryConfig.InternalInternationalSalaryInfo,
                        SalaryConfig.ExternalNationalSalaryInfo,
                        SalaryConfig.ExternalInternationalSalaryInfo,
                    };
                    int[] administrativeDrivingTimeByDriverType = new int[salarySettingsByDriverType.Length];
                    float[] drivingCostsByDriverType = new float[salarySettingsByDriverType.Length];
                    List<ComputedSalaryRateBlock>[] computeSalaryRateBlocksByType = new List<ComputedSalaryRateBlock>[salarySettingsByDriverType.Length];
                    for (int driverTypeIndex = 0; driverTypeIndex < salarySettingsByDriverType.Length; driverTypeIndex++) {
                        SalarySettings typeSalarySettings = salarySettingsByDriverType[driverTypeIndex];
                        typeSalarySettings.SetDriverTypeIndex(driverTypeIndex);
                        (administrativeDrivingTimeByDriverType[driverTypeIndex], drivingCostsByDriverType[driverTypeIndex], computeSalaryRateBlocksByType[driverTypeIndex]) = GetDrivingCost(shiftFirstActivity, shiftLastActivity, typeSalarySettings, timeframeLength);
                    }

                    // Get time in night and weekend
                    (int drivingTimeAtNight, int drivingTimeInWeekend) = GetShiftNightWeekendTime(shiftFirstActivity, shiftLastActivity, timeframeLength);

                    bool isNightShiftByLaw = RulesConfig.IsNightShiftByLawFunc(drivingTimeAtNight, drivingTime);
                    bool isNightShiftByCompanyRules = RulesConfig.IsNightShiftByCompanyRulesFunc(drivingTimeAtNight, drivingTime);
                    bool isWeekendShiftByCompanyRules = RulesConfig.IsWeekendShiftByCompanyRulesFunc(drivingTimeInWeekend, drivingTime);

                    int maxShiftLengthWithoutTravel, maxShiftLengthWithTravel, minRestTimeAfter;
                    if (isNightShiftByLaw) {
                        maxShiftLengthWithoutTravel = RulesConfig.NightShiftMaxLengthWithoutTravel;
                        maxShiftLengthWithTravel = RulesConfig.NightShiftMaxLengthWithTravel;
                        minRestTimeAfter = RulesConfig.NightShiftMinRestTime;
                    } else {
                        maxShiftLengthWithoutTravel = RulesConfig.NormalShiftMaxLengthWithoutTravel;
                        maxShiftLengthWithTravel = RulesConfig.NormalShiftMaxLengthWithTravel;
                        minRestTimeAfter = RulesConfig.NormalShiftMinRestTime;
                    }

                    shiftInfos[firstActivityIndex, lastActivityIndex] = new ShiftInfo(drivingTime, maxShiftLengthWithoutTravel, maxShiftLengthWithTravel, minRestTimeAfter, administrativeDrivingTimeByDriverType, drivingCostsByDriverType, computeSalaryRateBlocksByType, isNightShiftByLaw, isNightShiftByCompanyRules, isWeekendShiftByCompanyRules);
                }
            }

            return shiftInfos;
        }

        static (int, float, List<ComputedSalaryRateBlock>) GetDrivingCost(Activity shiftFirstActivity, Activity shiftLastActivity, SalarySettings salaryInfo, int timeframeLength) {
            // Repeat salary rate to cover entire week
            int timeframeDayCount = (int)Math.Floor((float)timeframeLength / MiscConfig.DayLength) + 1;
            List<SalaryRateBlock> processedSalaryRates = new List<SalaryRateBlock>();
            int weekPartIndex = 0;
            bool isCurrentlyWeekend = RulesConfig.WeekPartsForWeekend[weekPartIndex].IsSelected;
            for (int dayIndex = 0; dayIndex < timeframeDayCount; dayIndex++) {
                for (int salaryRateIndex = 0; salaryRateIndex < salaryInfo.WeekdaySalaryRates.Length; salaryRateIndex++) {
                    int rateStartTime = dayIndex * MiscConfig.DayLength + salaryInfo.WeekdaySalaryRates[salaryRateIndex].StartTime;

                    while (weekPartIndex + 1 < RulesConfig.WeekPartsForWeekend.Length && RulesConfig.WeekPartsForWeekend[weekPartIndex + 1].StartTime <= rateStartTime) {
                        weekPartIndex++;
                        isCurrentlyWeekend = RulesConfig.WeekPartsForWeekend[weekPartIndex].IsSelected;

                        SalaryRateBlock previousSalaryRateInfo = salaryRateIndex > 0 ? salaryInfo.WeekdaySalaryRates[salaryRateIndex - 1] : new SalaryRateBlock(-1, 0, 0);
                        if (isCurrentlyWeekend) {
                            // Start weekend within previous salary rate
                            processedSalaryRates.Add(new SalaryRateBlock(RulesConfig.WeekPartsForWeekend[weekPartIndex].StartTime, salaryInfo.WeekendSalaryRate, previousSalaryRateInfo.ContinuingRate));
                        } else {
                            // End weekend within previous salary rate
                            processedSalaryRates.Add(new SalaryRateBlock(RulesConfig.WeekPartsForWeekend[weekPartIndex].StartTime, previousSalaryRateInfo.SalaryRate, previousSalaryRateInfo.ContinuingRate));
                        }
                    }

                    // Start current salary rate
                    float currentSalaryRate = isCurrentlyWeekend ? salaryInfo.WeekendSalaryRate : salaryInfo.WeekdaySalaryRates[salaryRateIndex].SalaryRate;
                    processedSalaryRates.Add(new SalaryRateBlock(rateStartTime, currentSalaryRate, salaryInfo.WeekdaySalaryRates[salaryRateIndex].ContinuingRate));
                }
            }

            // Determine driving time, while keeping in mind the minimum paid time
            int drivingStartTime = shiftFirstActivity.StartTime;
            int drivingEndTimeReal = shiftLastActivity.EndTime;
            int administrativeDrivingTime = Math.Max(salaryInfo.MinPaidShiftTime, drivingEndTimeReal - drivingStartTime);
            int administrativeDrivingEndTime = drivingStartTime + administrativeDrivingTime;

            // Determine driving cost from the different salary rates; final block is skipped since we copied beyond timeframe length
            float? shiftContinuingRate = null;
            float drivingCost = 0;
            List<ComputedSalaryRateBlock> computeSalaryRateBlocks = new List<ComputedSalaryRateBlock>();
            for (int salaryRateIndex = 0; salaryRateIndex < processedSalaryRates.Count - 1; salaryRateIndex++) {
                SalaryRateBlock salaryRateInfo = processedSalaryRates[salaryRateIndex];
                SalaryRateBlock nextSalaryRateInfo = processedSalaryRates[salaryRateIndex + 1];
                int drivingTimeInRate = GetTimeInRange(drivingStartTime, administrativeDrivingEndTime, salaryRateInfo.StartTime, nextSalaryRateInfo.StartTime);

                if (drivingTimeInRate == 0) continue;

                // If the shift starts in a continuing rate, store this continuing rate
                if (!shiftContinuingRate.HasValue) {
                    shiftContinuingRate = salaryRateInfo.ContinuingRate;
                }

                float applicableSalaryRate = Math.Max(salaryRateInfo.SalaryRate, shiftContinuingRate.Value);
                float drivingCostInRate = drivingTimeInRate * applicableSalaryRate;
                drivingCost += drivingCostInRate;

                int salaryStartTime = Math.Max(salaryRateInfo.StartTime, drivingStartTime);
                int salaryEndTime = Math.Min(nextSalaryRateInfo.StartTime, administrativeDrivingEndTime);
                bool usesContinuingRate = shiftContinuingRate.Value > salaryRateInfo.SalaryRate;

                ComputedSalaryRateBlock prevComputedSalaryBlock = computeSalaryRateBlocks.Count > 0 ? computeSalaryRateBlocks[^1] : null;
                if (prevComputedSalaryBlock == null || prevComputedSalaryBlock.SalaryRate != applicableSalaryRate || prevComputedSalaryBlock.UsesContinuingRate != usesContinuingRate) {
                    computeSalaryRateBlocks.Add(new ComputedSalaryRateBlock(salaryRateInfo.StartTime, nextSalaryRateInfo.StartTime, salaryStartTime, salaryEndTime, drivingTimeInRate, applicableSalaryRate, usesContinuingRate, drivingCostInRate));
                } else {
                    prevComputedSalaryBlock.RateEndTime = nextSalaryRateInfo.StartTime;
                    prevComputedSalaryBlock.SalaryEndTime = salaryEndTime;
                    prevComputedSalaryBlock.SalaryDuration += drivingTimeInRate;
                    prevComputedSalaryBlock.DrivingCostInRate += drivingCostInRate;
                }
            }

            return (administrativeDrivingTime, drivingCost, computeSalaryRateBlocks);
        }

        static (int, int) GetShiftNightWeekendTime(Activity shiftFirstActivity, Activity shiftLastActivity, int timeframeLength) {
            // Repeat day parts for night info to cover entire week
            int timeframeDayCount = (int)Math.Floor((float)timeframeLength / MiscConfig.DayLength) + 1;
            List<TimePart> weekPartsForNight = new List<TimePart>();
            for (int dayIndex = 0; dayIndex < timeframeDayCount; dayIndex++) {
                for (int i = 0; i < RulesConfig.DayPartsForNight.Length; i++) {
                    int rateStartTime = dayIndex * MiscConfig.DayLength + RulesConfig.DayPartsForNight[i].StartTime;
                    weekPartsForNight.Add(new TimePart(rateStartTime, RulesConfig.DayPartsForNight[i].IsSelected));
                }
            }

            // Determine driving time, while keeping in mind the minimum paid time
            int drivingStartTime = shiftFirstActivity.StartTime;
            int drivingEndTime = shiftLastActivity.EndTime;

            // Determine driving time at night
            int drivingTimeAtNight = GetTimeInSelectedTimeParts(drivingStartTime, drivingEndTime, weekPartsForNight.ToArray());
            int drivingTimeInWeekend = GetTimeInSelectedTimeParts(drivingStartTime, drivingEndTime, RulesConfig.WeekPartsForWeekend);

            return (drivingTimeAtNight, drivingTimeInWeekend);
        }

        // Note that last part is not counted, since this should be the end of the timeframe
        static int GetTimeInSelectedTimeParts(int startTime, int endTime, TimePart[] timeParts) {
            int timeInParts = 0;
            for (int weekPartIndex = 0; weekPartIndex < timeParts.Length - 1; weekPartIndex++) {
                TimePart weekPart = timeParts[weekPartIndex];
                if (!weekPart.IsSelected) continue;

                TimePart nextWeekPart = timeParts[weekPartIndex + 1];
                int timeInPart = GetTimeInRange(startTime, endTime, weekPart.StartTime, nextWeekPart.StartTime);

                timeInParts += timeInPart;
            }
            return timeInParts;
        }

        static int GetTimeInRange(int startTime, int endTime, int rangeStartTime, int rangeEndTime) {
            int timeBeforeRange = Math.Max(0, rangeStartTime - startTime);
            int timeAfterRange = Math.Max(0, endTime - rangeEndTime);
            int timeInRange = Math.Max(0, endTime - startTime - timeBeforeRange - timeAfterRange);
            return timeInRange;
        }

        static InternalDriver[] CreateInternalDrivers(XSSFWorkbook settingsBook, XorShiftRandom rand) {
            ExcelSheet internalDriverSettingsSheet = new ExcelSheet("Internal drivers", settingsBook);

            (int[][] internalDriversHomeTravelTimes, int[][] internalDriversHomeTravelDistances, string[] travelInfoInternalDriverNames, _) = TravelInfoImporter.ImportBipartiteTravelInfo(Path.Combine(AppConfig.IntermediateFolder, "internalTravelInfo.csv"));

            List<InternalDriver> internalDrivers = new List<InternalDriver>();
            internalDriverSettingsSheet.ForEachRow(internalDriverSettingsRow => {
                string driverName = internalDriverSettingsSheet.GetStringValue(internalDriverSettingsRow, "Internal driver name");
                int? contractTime = internalDriverSettingsSheet.GetIntValue(internalDriverSettingsRow, "Hours per week") * MiscConfig.HourLength;
                bool? isInternationalDriver = internalDriverSettingsSheet.GetBoolValue(internalDriverSettingsRow, "Is international?");
                if (driverName == null || !contractTime.HasValue || !isInternationalDriver.HasValue) return;
                if (contractTime.Value == 0) return;

                int travelInfoInternalDriverIndex = Array.IndexOf(travelInfoInternalDriverNames, driverName);
                if (travelInfoInternalDriverIndex == -1) {
                    throw new Exception(string.Format("Could not find internal driver `{0}` in internal travel info", driverName));
                }
                int[] homeTravelTimes = internalDriversHomeTravelTimes[travelInfoInternalDriverIndex];
                int[] homeTravelDistance = internalDriversHomeTravelDistances[travelInfoInternalDriverIndex];

                // Temp: generate track proficiencies
                bool[,] trackProficiencies = DataGenerator.GenerateInternalDriverTrackProficiencies(homeTravelTimes.Length, rand);

                InternalSalarySettings salaryInfo = isInternationalDriver.Value ? SalaryConfig.InternalInternationalSalaryInfo : SalaryConfig.InternalNationalSalaryInfo;

                int internalDriverIndex = internalDrivers.Count;
                internalDrivers.Add(new InternalDriver(internalDriverIndex, internalDriverIndex, driverName, isInternationalDriver.Value, homeTravelTimes, homeTravelDistance, trackProficiencies, contractTime.Value, salaryInfo));
            });
            return internalDrivers.ToArray();
        }

        // TODO: use this when route knowledge data is available
        static bool[][,] ParseInternalDriverTrackProficiencies(ExcelSheet routeKnowledgeTable, string[] internalDriverNames, string[] stationNames) {
            bool[][,] internalDriverProficiencies = new bool[internalDriverNames.Length][,];
            for (int driverIndex = 0; driverIndex < internalDriverNames.Length; driverIndex++) {
                internalDriverProficiencies[driverIndex] = new bool[stationNames.Length, stationNames.Length];

                // Everyone is proficient when staying in the same location
                for (int stationIndex = 0; stationIndex < stationNames.Length; stationIndex++) {
                    internalDriverProficiencies[driverIndex][stationIndex, stationIndex] = true;
                }
            }

            routeKnowledgeTable.ForEachRow(routeKnowledgeRow => {
                // Get station indices
                string station1Name = routeKnowledgeTable.GetStringValue(routeKnowledgeRow, "OriginLocationName");
                int station1Index = Array.IndexOf(stationNames, station1Name);
                string station2Name = routeKnowledgeTable.GetStringValue(routeKnowledgeRow, "DestinationLocationName");
                int station2Index = Array.IndexOf(stationNames, station2Name);
                if (station1Index == -1 || station2Index == -1) return;

                // Get driver index
                string driverName = routeKnowledgeTable.GetStringValue(routeKnowledgeRow, "EmployeeName");
                int driverIndex = Array.IndexOf(internalDriverNames, driverName);
                if (driverIndex == -1) return;

                internalDriverProficiencies[driverIndex][station1Index, station2Index] = true;
                internalDriverProficiencies[driverIndex][station2Index, station1Index] = true;
            });

            return internalDriverProficiencies;
        }

        static (ExternalDriverType[], ExternalDriver[][], Dictionary<(string, bool), ExternalDriver[]>) CreateExternalDrivers(XSSFWorkbook settingsBook, int internalDriverCount, XorShiftRandom rand) {
            ExcelSheet externalDriverCompanySettingsSheet = new ExcelSheet("External driver companies", settingsBook);

            (int[][] externalDriversHomeTravelTimes, int[][] externalDriversHomeTravelDistances, string[] travelInfoExternalCompanyNames, _) = TravelInfoImporter.ImportBipartiteTravelInfo(Path.Combine(AppConfig.IntermediateFolder, "externalTravelInfo.csv"));

            List<ExternalDriverType> externalDriverTypes = new List<ExternalDriverType>();
            List<ExternalDriver[]> externalDriversByType = new List<ExternalDriver[]>();
            Dictionary<(string, bool), ExternalDriver[]> externalDriversByTypeDict = new Dictionary<(string, bool), ExternalDriver[]>();
            int allDriverIndex = internalDriverCount;
            int externalDriverTypeIndex = 0;
            externalDriverCompanySettingsSheet.ForEachRow(externalDriverCompanySettingsRow => {
                string companyName = externalDriverCompanySettingsSheet.GetStringValue(externalDriverCompanySettingsRow, "External company name");
                string shortCompanyName = externalDriverCompanySettingsSheet.GetStringValue(externalDriverCompanySettingsRow, "Short name");
                bool? isHotelAllowed = externalDriverCompanySettingsSheet.GetBoolValue(externalDriverCompanySettingsRow, "Allows hotel stays?");
                int? nationalMinShiftCount = externalDriverCompanySettingsSheet.GetIntValue(externalDriverCompanySettingsRow, "National min shift count");
                int? nationalMaxShiftCount = externalDriverCompanySettingsSheet.GetIntValue(externalDriverCompanySettingsRow, "National max shift count");
                int? internationalMinShiftCount = externalDriverCompanySettingsSheet.GetIntValue(externalDriverCompanySettingsRow, "International min shift count");
                int? internationalMaxShiftCount = externalDriverCompanySettingsSheet.GetIntValue(externalDriverCompanySettingsRow, "International max shift count");
                if (companyName == null || shortCompanyName == null || !isHotelAllowed.HasValue || !nationalMinShiftCount.HasValue || !nationalMaxShiftCount.HasValue || !internationalMinShiftCount.HasValue || !internationalMaxShiftCount.HasValue) return;

                int travelInfoExternalCompanyIndex = Array.IndexOf(travelInfoExternalCompanyNames, companyName);
                if (travelInfoExternalCompanyIndex == -1) {
                    throw new Exception(string.Format("Could not find external company `{0}` in external travel info", companyName));
                }
                int[] homeTravelTimes = externalDriversHomeTravelTimes[travelInfoExternalCompanyIndex];
                int[] homeTravelDistances = externalDriversHomeTravelDistances[travelInfoExternalCompanyIndex];

                // National drivers
                if (nationalMaxShiftCount.Value > 0) {
                    string typeName = string.Format("{0}", companyName);
                    externalDriverTypes.Add(new ExternalDriverType(typeName, false, isHotelAllowed.Value, nationalMinShiftCount.Value, nationalMaxShiftCount.Value));

                    ExternalDriver[] currentTypeNationalDrivers = new ExternalDriver[nationalMaxShiftCount.Value];
                    for (int indexInType = 0; indexInType < nationalMaxShiftCount; indexInType++) {
                        ExternalDriver newExternalDriver = new ExternalDriver(allDriverIndex, externalDriverTypeIndex, indexInType, companyName, shortCompanyName, false, isHotelAllowed.Value, homeTravelTimes, homeTravelDistances, SalaryConfig.ExternalNationalSalaryInfo);
                        currentTypeNationalDrivers[indexInType] = newExternalDriver;
                        allDriverIndex++;
                    }
                    externalDriversByType.Add(currentTypeNationalDrivers);
                    externalDriverTypeIndex++;

                    externalDriversByTypeDict.Add((companyName, false), currentTypeNationalDrivers);
                }

                // International drivers
                if (internationalMaxShiftCount.Value > 0) {
                    string typeName = string.Format("{0}", companyName);
                    externalDriverTypes.Add(new ExternalDriverType(typeName, true, isHotelAllowed.Value, internationalMinShiftCount.Value, internationalMaxShiftCount.Value));

                    ExternalDriver[] currentTypeInternationalDrivers = new ExternalDriver[internationalMaxShiftCount.Value];
                    for (int indexInType = 0; indexInType < internationalMaxShiftCount; indexInType++) {
                        ExternalDriver newExternalDriver = new ExternalDriver(allDriverIndex, externalDriverTypeIndex, indexInType, companyName, shortCompanyName, true, isHotelAllowed.Value, homeTravelTimes, homeTravelDistances, SalaryConfig.ExternalInternationalSalaryInfo);
                        currentTypeInternationalDrivers[indexInType] = newExternalDriver;
                        allDriverIndex++;
                    }
                    externalDriversByType.Add(currentTypeInternationalDrivers);
                    externalDriverTypeIndex++;

                    externalDriversByTypeDict.Add((companyName, true), currentTypeInternationalDrivers);
                }

            });
            return (externalDriverTypes.ToArray(), externalDriversByType.ToArray(), externalDriversByTypeDict);
        }

        static Driver[] GetDataAssignment(XSSFWorkbook settingsBook, Activity[] activities, InternalDriver[] internalDrivers, Dictionary<(string, bool), ExternalDriver[]> externalDriversByTypeDict) {
            ExcelSheet externalDriversSettingsSheet = new ExcelSheet("External drivers", settingsBook);
            List<(string, string)> externalInternationalDriverNames = new List<(string, string)>();
            externalDriversSettingsSheet.ForEachRow(externalDriverSettingsRow => {
                bool? isInternationalDriver = externalDriversSettingsSheet.GetBoolValue(externalDriverSettingsRow, "Is international?");
                if (!isInternationalDriver.HasValue || !isInternationalDriver.Value) return;

                string driverName = externalDriversSettingsSheet.GetStringValue(externalDriverSettingsRow, "External driver name");
                string companyName = externalDriversSettingsSheet.GetStringValue(externalDriverSettingsRow, "Company name");
                externalInternationalDriverNames.Add((driverName, companyName));
            });

            Driver[] dataAssignment = new Driver[activities.Length];
            Dictionary<(string, bool), List<string>> externalDriverNamesByTypeDict = new Dictionary<(string, bool), List<string>>();
            for (int activityIndex = 0; activityIndex < dataAssignment.Length; activityIndex++) {
                Activity activity = activities[activityIndex];
                if (activity.DataAssignedCompanyName == null || activity.DataAssignedEmployeeName == null) {
                    // Unassigned activity
                    continue;
                }

                if (DataConfig.ExcelInternalDriverCompanyNames.Contains(activity.DataAssignedCompanyName)) {
                    // Assigned to internal driver
                    dataAssignment[activityIndex] = Array.Find(internalDrivers, internalDriver => internalDriver.GetInternalDriverName(true) == activity.DataAssignedEmployeeName);
                } else {
                    // Assigned to external driver
                    bool isInternational = externalInternationalDriverNames.Contains((activity.DataAssignedEmployeeName, activity.DataAssignedCompanyName));

                    // Get list of already encountered names of this type
                    List<string> externalDriverNamesOfType;
                    if (externalDriverNamesByTypeDict.ContainsKey((activity.DataAssignedCompanyName, isInternational))) {
                        externalDriverNamesOfType = externalDriverNamesByTypeDict[(activity.DataAssignedCompanyName, isInternational)];
                    } else {
                        externalDriverNamesOfType = new List<string>();
                        externalDriverNamesByTypeDict.Add((activity.DataAssignedCompanyName, isInternational), externalDriverNamesOfType);
                    }

                    // Determine index of this driver in the type
                    int externalDriverIndexInType = externalDriverNamesOfType.IndexOf(activity.DataAssignedEmployeeName);
                    if (externalDriverIndexInType == -1) {
                        externalDriverIndexInType = externalDriverNamesOfType.Count;
                        externalDriverNamesOfType.Add(activity.DataAssignedEmployeeName);
                    }

                    ExternalDriver[] externalDriversOfType = externalDriversByTypeDict[(activity.DataAssignedCompanyName, isInternational)];
                    dataAssignment[activityIndex] = externalDriversOfType[externalDriverIndexInType];
                }
            }
            return dataAssignment;
        }


        /* Helper methods */

        public ShiftInfo ShiftInfo(Activity activity1, Activity activity2) {
            return shiftInfos[activity1.Index, activity2.Index];
        }

        public bool IsValidSuccession(Activity activity1, Activity activity2) {
            return activitySuccession[activity1.Index, activity2.Index];
        }

        public float ActivitySuccessionRobustness(Activity activity1, Activity activity2) {
            return activitySuccessionRobustness[activity1.Index, activity2.Index];
        }

        public int PlannedCarTravelTime(Activity activity1, Activity activity2) {
            return plannedCarTravelTimes[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex];
        }

        public int ExpectedCarTravelTime(Activity activity1, Activity activity2) {
            return expectedCarTravelTimes[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex];
        }

        public int CarTravelDistance(Activity activity1, Activity activity2) {
            return carTravelDistances[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex];
        }

        public int PlannedTravelTimeViaHotel(Activity activity1, Activity activity2) {
            return plannedCarTravelTimes[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex] + RulesConfig.HotelExtraTravelTime;
        }

        public int ExpectedTravelTimeViaHotel(Activity activity1, Activity activity2) {
            return expectedCarTravelTimes[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex] + RulesConfig.HotelExtraTravelTime;
        }

        public int PlannedHalfTravelTimeViaHotel(Activity activity1, Activity activity2) {
            return (plannedCarTravelTimes[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex] + RulesConfig.HotelExtraTravelTime) / 2;
        }

        public int ExpectedHalfTravelTimeViaHotel(Activity activity1, Activity activity2) {
            return (expectedCarTravelTimes[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex] + RulesConfig.HotelExtraTravelTime) / 2;
        }

        public int HalfTravelDistanceViaHotel(Activity activity1, Activity activity2) {
            return (carTravelDistances[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex] + RulesConfig.HotelExtraTravelDistance) / 2;
        }

        public int RestTimeWithTravelTime(Activity activity1, Activity activity2, int travelTime) {
            return activity2.StartTime - activity1.EndTime - travelTime;
        }

        public int RestTimeViaHotel(Activity activity1, Activity activity2) {
            return RestTimeWithTravelTime(activity1, activity2, ExpectedTravelTimeViaHotel(activity1, activity2));
        }

        int PlannedWaitingTime(Activity activity1, Activity activity2) {
            return activity2.StartTime - activity1.EndTime - PlannedCarTravelTime(activity1, activity2);
        }

        int ExpectedWaitingTime(Activity activity1, Activity activity2) {
            return activity2.StartTime - activity1.EndTime - ExpectedCarTravelTime(activity1, activity2);
        }

        /** Check if two activites belong to the same shift or not, based on whether their waiting time is within the threshold */
        public bool AreSameShift(Activity activity1, Activity activity2) {
            return activitiesAreSameShift[activity1.Index, activity2.Index];
        }

        void DebugLogProcessedSalaryRates(List<SalaryRateBlock> processedSalaryRates) {
            for (int i = 0; i < processedSalaryRates.Count; i++) {
                SalaryRateBlock rate = processedSalaryRates[i];

                int dayNum = rate.StartTime / (24 * 60);
                int hourNum = rate.StartTime % (24 * 60) / 60;

                string[] weekdays = new string[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun", "Mon2" };

                Console.WriteLine("{0,4} {1,2}:00  |  Rate: {3,5}  |  Cont.: {4,5}", weekdays[dayNum], hourNum, rate.StartTime, rate.SalaryRate * 60, rate.ContinuingRate * 60);
            }
        }
    }

    class ComputedSalaryRateBlock {
        public int RateStartTime, RateEndTime, SalaryStartTime, SalaryEndTime, SalaryDuration;
        public float SalaryRate, DrivingCostInRate;
        public bool UsesContinuingRate;

        public ComputedSalaryRateBlock(int rateStartTime, int rateEndTime, int salaryStartTime, int salaryEndTime, int salaryDuration, float salaryRate, bool usesContinuingRate, float drivingCostInRate) {
            RateStartTime = rateStartTime;
            RateEndTime = rateEndTime;
            SalaryStartTime = salaryStartTime;
            SalaryEndTime = salaryEndTime;
            SalaryDuration = salaryDuration;
            SalaryRate = salaryRate;
            UsesContinuingRate = usesContinuingRate;
            DrivingCostInRate = drivingCostInRate;
        }
    }
}

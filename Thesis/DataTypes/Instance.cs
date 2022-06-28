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
        public readonly Trip[] Trips;
        public readonly string[] StationNames;
        readonly ShiftInfo[,] shiftInfos;
        readonly float[,] tripSuccessionRobustness;
        readonly bool[,] tripSuccession, tripsAreSameShift;
        public readonly InternalDriver[] InternalDrivers;
        public readonly ExternalDriverType[] ExternalDriverTypes;
        public readonly ExternalDriver[][] ExternalDriversByType;
        public readonly Driver[] AllDrivers, DataAssignment;

        public Instance(XorShiftRandom rand, RawTrip[] rawTrips) {
            XSSFWorkbook addressesBook = ExcelHelper.ReadExcelFile(Path.Combine(AppConfig.InputFolder, "stationAddresses.xlsx"));
            XSSFWorkbook settingsBook = ExcelHelper.ReadExcelFile(Path.Combine(AppConfig.InputFolder, "settings.xlsx"));

            (StationNames, plannedCarTravelTimes, expectedCarTravelTimes, carTravelDistances) = GetStationNamesAndExpectedCarTravelInfo();
            (Trips, tripSuccession, tripSuccessionRobustness, tripsAreSameShift, timeframeLength, UniqueSharedRouteCount) = ProcessRawTrips(addressesBook, rawTrips, StationNames, expectedCarTravelTimes);
            shiftInfos = GetShiftInfos(Trips, timeframeLength);
            InternalDrivers = CreateInternalDrivers(settingsBook, rand);
            Dictionary<(string, bool), ExternalDriver[]> externalDriversByTypeDict;
            (ExternalDriverTypes, ExternalDriversByType, externalDriversByTypeDict) = CreateExternalDrivers(settingsBook, InternalDrivers.Length, rand);
            DataAssignment = GetDataAssignment(settingsBook, Trips, InternalDrivers, externalDriversByTypeDict);

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

        (Trip[], bool[,], float[,], bool[,], int, int) ProcessRawTrips(XSSFWorkbook addressesBook, RawTrip[] rawTrips, string[] stationNames, int[,] expectedCarTravelTimes) {
            if (rawTrips.Length == 0) {
                throw new Exception("No trips found in timeframe");
            }

            // Sort trips by start time
            rawTrips = rawTrips.OrderBy(trip => trip.StartTime).ToArray();

            // Get dictionary mapping station names in data to their index in the address list
            Dictionary<string, int> stationDataNameToAddressIndex = GetStationDataNameToAddressIndexDict(addressesBook, stationNames);

            // Create trip objects
            Trip[] trips = new Trip[rawTrips.Length];
            for (int tripIndex = 0; tripIndex < trips.Length; tripIndex++) {
                RawTrip rawTrip = rawTrips[tripIndex];

                // Get index of start and end station addresses
                if (!stationDataNameToAddressIndex.ContainsKey(rawTrip.StartStationName)) throw new Exception(string.Format("Unknown station `{0}`", rawTrip.StartStationName));
                int startStationAddressIndex = stationDataNameToAddressIndex[rawTrip.StartStationName];
                if (!stationDataNameToAddressIndex.ContainsKey(rawTrip.EndStationName)) throw new Exception(string.Format("Unknown station `{0}`", rawTrip.EndStationName));
                int endStationAddressIndex = stationDataNameToAddressIndex[rawTrip.EndStationName];

                trips[tripIndex] = new Trip(rawTrip, tripIndex, startStationAddressIndex, endStationAddressIndex);
            }

            // Generate precedence constraints
            for (int trip1Index = 0; trip1Index < trips.Length; trip1Index++) {
                for (int trip2Index = trip1Index; trip2Index < trips.Length; trip2Index++) {
                    Trip trip1 = trips[trip1Index];
                    Trip trip2 = trips[trip2Index];
                    int travelTimeBetween = expectedCarTravelTimes[trip1.EndStationAddressIndex, trip2.StartStationAddressIndex];

                    if (trip1.EndTime + travelTimeBetween <= trip2.StartTime) {
                        trip1.AddSuccessor(trip2);
                    }
                }
            }

            // Create 2D bool array indicating whether trips can succeed each other
            // Also preprocess the robustness scores of trips when used in successsion
            bool[,] tripSuccession = new bool[trips.Length, trips.Length];
            float[,] tripSuccessionRobustness = new float[trips.Length, trips.Length];
            for (int tripIndex = 0; tripIndex < trips.Length; tripIndex++) {
                Trip trip = trips[tripIndex];
                for (int successorIndex = 0; successorIndex < trip.Successors.Count; successorIndex++) {
                    Trip successor = trip.Successors[successorIndex];
                    tripSuccession[tripIndex, successor.Index] = true;

                    int plannedWaitingTime = ExpectedWaitingTime(trip, successor);
                    tripSuccessionRobustness[tripIndex, successor.Index] = GetSuccessionRobustness(trip, successor, trip.Duration, plannedWaitingTime);
                }
            }

            // Preprocess whether trips could belong to the same shift
            bool[,] tripsAreSameShift = new bool[trips.Length, trips.Length];
            for (int trip1Index = 0; trip1Index < trips.Length; trip1Index++) {
                Trip trip1 = trips[trip1Index];
                for (int trip2Index = trip1Index; trip2Index < trips.Length; trip2Index++) {
                    Trip trip2 = trips[trip2Index];
                    tripsAreSameShift[trip1.Index, trip2.Index] = ExpectedWaitingTime(trip1, trip2) <= SaConfig.ShiftWaitingTimeThreshold;
                }
            }

            // Timeframe length is the last end time of all trips
            int timeframeLength = 0;
            for (int tripIndex = 0; tripIndex < trips.Length; tripIndex++) {
                timeframeLength = Math.Max(timeframeLength, trips[tripIndex].EndTime);
            }

            // Determine list of unique trip routes
            List<(int, int, int)> routeCounts = new List<(int, int, int)>();
            for (int tripIndex = 0; tripIndex < trips.Length; tripIndex++) {
                Trip trip = trips[tripIndex];
                if (trip.StartStationAddressIndex == trip.EndStationAddressIndex) continue;
                (int lowStationIndex, int highStationIndex) = GetLowHighStationIndices(trip);

                bool isExistingRoute = routeCounts.Any(route => route.Item1 == lowStationIndex && route.Item2 == highStationIndex);
                if (isExistingRoute) {
                    int routeIndex = routeCounts.FindIndex(route => route.Item1 == lowStationIndex && route.Item2 == highStationIndex);
                    routeCounts[routeIndex] = (routeCounts[routeIndex].Item1, routeCounts[routeIndex].Item2, routeCounts[routeIndex].Item3 + 1);
                } else {
                    routeCounts.Add((trip.StartStationAddressIndex, trip.EndStationAddressIndex, 1));
                }
            }

            // Store indices of shared routes for trips
            List<(int, int, int)> sharedRouteCounts = routeCounts.FindAll(route => route.Item3 > 1);
            for (int tripIndex = 0; tripIndex < trips.Length; tripIndex++) {
                Trip trip = trips[tripIndex];
                (int lowStationIndex, int highStationIndex) = GetLowHighStationIndices(trip);

                int sharedRouteIndex = sharedRouteCounts.FindIndex(route => route.Item1 == lowStationIndex && route.Item2 == highStationIndex);
                if (sharedRouteIndex != -1) {
                    trip.SetSharedRouteIndex(sharedRouteIndex);
                }
            }
            int uniqueSharedRouteCount = sharedRouteCounts.Count;

            return (trips, tripSuccession, tripSuccessionRobustness, tripsAreSameShift, timeframeLength, uniqueSharedRouteCount);
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

        static float GetSuccessionRobustness(Trip trip1, Trip trip2, int plannedDuration, int waitingTime) {
            double conflictProb = GetConflictProbability(plannedDuration, waitingTime);

            bool areSameDuty = trip1.DutyId == trip2.DutyId;
            bool areSameProject = trip1.ProjectName == trip2.ProjectName && trip1.ProjectName != "";

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
            double meanDelay = RulesConfig.TripMeanDelayFunc(plannedDuration);
            double delayAlpha = RulesConfig.TripDelayGammaDistributionAlphaFunc(meanDelay);
            double delayBeta = RulesConfig.TripDelayGammaDistributionBetaFunc(meanDelay);
            float delayProb = RulesConfig.TripDelayProbability;
            double conflictProbWhenDelayed = 1 - Gamma.CDF(delayAlpha, delayBeta, waitingTime);
            double conflictProb = delayProb * conflictProbWhenDelayed;
            return conflictProb;
        }

        static (int, int) GetLowHighStationIndices(Trip trip) {
            int lowStationIndex, highStationIndex;
            if (trip.StartStationAddressIndex < trip.EndStationAddressIndex) {
                lowStationIndex = trip.StartStationAddressIndex;
                highStationIndex = trip.EndStationAddressIndex;
            } else {
                lowStationIndex = trip.EndStationAddressIndex;
                highStationIndex = trip.StartStationAddressIndex;
            }
            return (lowStationIndex, highStationIndex);
        }

        /** Preprocess shift driving times, night fractions and weekend fractions */
        static ShiftInfo[,] GetShiftInfos(Trip[] trips, int timeframeLength) {
            ShiftInfo[,] shiftInfos = new ShiftInfo[trips.Length, trips.Length];
            for (int firstTripIndex = 0; firstTripIndex < trips.Length; firstTripIndex++) {
                for (int lastTripIndex = 0; lastTripIndex < trips.Length; lastTripIndex++) {
                    Trip firstTripInternal = trips[firstTripIndex];
                    Trip lastTripInternal = trips[lastTripIndex];

                    // Determine driving time
                    int drivingStartTime = firstTripInternal.StartTime;
                    int drivingEndTime = lastTripInternal.EndTime;
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
                        (administrativeDrivingTimeByDriverType[driverTypeIndex], drivingCostsByDriverType[driverTypeIndex], computeSalaryRateBlocksByType[driverTypeIndex]) = GetDrivingCost(firstTripInternal, lastTripInternal, typeSalarySettings, timeframeLength);
                    }

                    // Get time in night and weekend
                    (int drivingTimeAtNight, int drivingTimeInWeekend) = GetShiftNightWeekendTime(firstTripInternal, lastTripInternal, timeframeLength);

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

                    shiftInfos[firstTripIndex, lastTripIndex] = new ShiftInfo(drivingTime, maxShiftLengthWithoutTravel, maxShiftLengthWithTravel, minRestTimeAfter, administrativeDrivingTimeByDriverType, drivingCostsByDriverType, computeSalaryRateBlocksByType, isNightShiftByLaw, isNightShiftByCompanyRules, isWeekendShiftByCompanyRules);
                }
            }

            return shiftInfos;
        }

        static (int, float, List<ComputedSalaryRateBlock>) GetDrivingCost(Trip firstTripInternal, Trip lastTripInternal, SalarySettings salaryInfo, int timeframeLength) {
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
            int drivingStartTime = firstTripInternal.StartTime;
            int drivingEndTimeReal = lastTripInternal.EndTime;
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

        static (int, int) GetShiftNightWeekendTime(Trip firstTripInternal, Trip lastTripInternal, int timeframeLength) {
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
            int drivingStartTime = firstTripInternal.StartTime;
            int drivingEndTime = lastTripInternal.EndTime;

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

        static Driver[] GetDataAssignment(XSSFWorkbook settingsBook, Trip[] trips, InternalDriver[] internalDrivers, Dictionary<(string, bool), ExternalDriver[]> externalDriversByTypeDict) {
            ExcelSheet externalDriversSettingsSheet = new ExcelSheet("External drivers", settingsBook);
            List<(string, string)> externalInternationalDriverNames = new List<(string, string)>();
            externalDriversSettingsSheet.ForEachRow(externalDriverSettingsRow => {
                bool? isInternationalDriver = externalDriversSettingsSheet.GetBoolValue(externalDriverSettingsRow, "Is international?");
                if (!isInternationalDriver.HasValue || !isInternationalDriver.Value) return;

                string driverName = externalDriversSettingsSheet.GetStringValue(externalDriverSettingsRow, "External driver name");
                string companyName = externalDriversSettingsSheet.GetStringValue(externalDriverSettingsRow, "Company name");
                externalInternationalDriverNames.Add((driverName, companyName));
            });

            Driver[] dataAssignment = new Driver[trips.Length];
            Dictionary<(string, bool), List<string>> externalDriverNamesByTypeDict = new Dictionary<(string, bool), List<string>>();
            for (int tripIndex = 0; tripIndex < dataAssignment.Length; tripIndex++) {
                Trip trip = trips[tripIndex];
                if (trip.DataAssignedCompanyName == null || trip.DataAssignedEmployeeName == null) {
                    // Unassigned trip
                    continue;
                }

                if (DataConfig.ExcelInternalDriverCompanyNames.Contains(trip.DataAssignedCompanyName)) {
                    // Assigned to internal driver
                    dataAssignment[tripIndex] = Array.Find(internalDrivers, internalDriver => internalDriver.GetInternalDriverName(true) == trip.DataAssignedEmployeeName);
                } else {
                    // Assigned to external driver
                    bool isInternational = externalInternationalDriverNames.Contains((trip.DataAssignedEmployeeName, trip.DataAssignedCompanyName));

                    // Get list of already encountered names of this type
                    List<string> externalDriverNamesOfType;
                    if (externalDriverNamesByTypeDict.ContainsKey((trip.DataAssignedCompanyName, isInternational))) {
                        externalDriverNamesOfType = externalDriverNamesByTypeDict[(trip.DataAssignedCompanyName, isInternational)];
                    } else {
                        externalDriverNamesOfType = new List<string>();
                        externalDriverNamesByTypeDict.Add((trip.DataAssignedCompanyName, isInternational), externalDriverNamesOfType);
                    }

                    // Determine index of this driver in the type
                    int externalDriverIndexInType = externalDriverNamesOfType.IndexOf(trip.DataAssignedEmployeeName);
                    if (externalDriverIndexInType == -1) {
                        externalDriverIndexInType = externalDriverNamesOfType.Count;
                        externalDriverNamesOfType.Add(trip.DataAssignedEmployeeName);
                    }

                    ExternalDriver[] externalDriversOfType = externalDriversByTypeDict[(trip.DataAssignedCompanyName, isInternational)];
                    dataAssignment[tripIndex] = externalDriversOfType[externalDriverIndexInType];
                }
            }
            return dataAssignment;
        }


        /* Helper methods */

        public ShiftInfo ShiftInfo(Trip trip1, Trip trip2) {
            return shiftInfos[trip1.Index, trip2.Index];
        }

        public bool IsValidPrecedence(Trip trip1, Trip trip2) {
            return tripSuccession[trip1.Index, trip2.Index];
        }

        public float TripSuccessionRobustness(Trip trip1, Trip trip2) {
            return tripSuccessionRobustness[trip1.Index, trip2.Index];
        }

        public int PlannedCarTravelTime(Trip trip1, Trip trip2) {
            return plannedCarTravelTimes[trip1.EndStationAddressIndex, trip2.StartStationAddressIndex];
        }

        public int ExpectedCarTravelTime(Trip trip1, Trip trip2) {
            return expectedCarTravelTimes[trip1.EndStationAddressIndex, trip2.StartStationAddressIndex];
        }

        public int CarTravelDistance(Trip trip1, Trip trip2) {
            return carTravelDistances[trip1.EndStationAddressIndex, trip2.StartStationAddressIndex];
        }

        public int PlannedTravelTimeViaHotel(Trip trip1, Trip trip2) {
            return plannedCarTravelTimes[trip1.EndStationAddressIndex, trip2.StartStationAddressIndex] + RulesConfig.HotelExtraTravelTime;
        }

        public int ExpectedTravelTimeViaHotel(Trip trip1, Trip trip2) {
            return expectedCarTravelTimes[trip1.EndStationAddressIndex, trip2.StartStationAddressIndex] + RulesConfig.HotelExtraTravelTime;
        }

        public int PlannedHalfTravelTimeViaHotel(Trip trip1, Trip trip2) {
            return (plannedCarTravelTimes[trip1.EndStationAddressIndex, trip2.StartStationAddressIndex] + RulesConfig.HotelExtraTravelTime) / 2;
        }

        public int ExpectedHalfTravelTimeViaHotel(Trip trip1, Trip trip2) {
            return (expectedCarTravelTimes[trip1.EndStationAddressIndex, trip2.StartStationAddressIndex] + RulesConfig.HotelExtraTravelTime) / 2;
        }

        public int HalfTravelDistanceViaHotel(Trip trip1, Trip trip2) {
            return (carTravelDistances[trip1.EndStationAddressIndex, trip2.StartStationAddressIndex] + RulesConfig.HotelExtraTravelDistance) / 2;
        }

        public int RestTimeWithTravelTime(Trip trip1, Trip trip2, int travelTime) {
            return trip2.StartTime - trip1.EndTime - travelTime;
        }

        public int RestTimeViaHotel(Trip trip1, Trip trip2) {
            return RestTimeWithTravelTime(trip1, trip2, ExpectedTravelTimeViaHotel(trip1, trip2));
        }

        int PlannedWaitingTime(Trip trip1, Trip trip2) {
            return trip2.StartTime - trip1.EndTime - PlannedCarTravelTime(trip1, trip2);
        }

        int ExpectedWaitingTime(Trip trip1, Trip trip2) {
            return trip2.StartTime - trip1.EndTime - ExpectedCarTravelTime(trip1, trip2);
        }

        /** Check if two trips belong to the same shift or not, based on whether their waiting time is within the threshold */
        public bool AreSameShift(Trip trip1, Trip trip2) {
            return tripsAreSameShift[trip1.Index, trip2.Index];
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

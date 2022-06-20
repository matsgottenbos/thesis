using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class Instance {
        public readonly XorShiftRandom Rand;
        readonly int timeframeLength;
        public readonly int UniqueSharedRouteCount;
        readonly int[,] expectedCarTravelTimes;
        public readonly Trip[] Trips;
        public readonly string[] StationNames;
        readonly ShiftInfo[,] shiftInfos;
        readonly float[,] tripSuccessionRobustness;
        readonly bool[,] tripSuccession, tripsAreSameShift;
        public readonly InternalDriver[] InternalDrivers;
        public readonly ExternalDriver[][] ExternalDriversByType;
        public readonly Driver[] AllDrivers;

        public Instance(XorShiftRandom rand, Trip[] rawTrips, string[] stationNames, int[,] plannedCarTravelTimes, string[] internalDriverNames, int[][] internalDriversHomeTravelTimes, bool[][,] internalDriversTrackProficiencies, int[] internalDriverContractTimes, bool[] internalDriverIsInternational, int[][] externalDriverHomeTravelTimes) {
            Rand = rand;
            expectedCarTravelTimes = GetExpectedCarTravelTimes(plannedCarTravelTimes);
            (Trips, tripSuccession, tripSuccessionRobustness, tripsAreSameShift, timeframeLength, UniqueSharedRouteCount) = PrepareTrips(rawTrips, expectedCarTravelTimes);
            StationNames = stationNames;
            shiftInfos = GetShiftInfos(Trips, timeframeLength);
            InternalDrivers = CreateInternalDrivers(internalDriverNames, internalDriversHomeTravelTimes, internalDriversTrackProficiencies, internalDriverContractTimes, internalDriverIsInternational);
            ExternalDriversByType = CreateExternalDrivers(externalDriverHomeTravelTimes, InternalDrivers.Length);

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

        int[,] GetExpectedCarTravelTimes(int[,] plannedCarTravelTimes) {
            int stationCount = plannedCarTravelTimes.GetLength(0);
            int[,] expectedCarTravelTimes = new int[stationCount, stationCount];
            for (int location1Index = 0; location1Index < stationCount; location1Index++) {
                for (int location2Index = location1Index; location2Index < stationCount; location2Index++) {
                    int plannedTravelTimeBetween = plannedCarTravelTimes[location1Index, location2Index];
                    if (plannedTravelTimeBetween == 0) {
                        expectedCarTravelTimes[location1Index, location2Index] = 0;
                    } else {
                        expectedCarTravelTimes[location1Index, location2Index] = RulesConfig.TravelDelayExpectedFunc(plannedTravelTimeBetween);
                    }
                }
            }
            return expectedCarTravelTimes;
        }

        (Trip[], bool[,], float[,], bool[,], int, int) PrepareTrips(Trip[] rawTrips, int[,] expectedCarTravelTimes) {
            // Sort trips by start time
            Trip[] trips = rawTrips.OrderBy(trip => trip.StartTime).ToArray();

            // Add trip indices
            for (int tripIndex = 0; tripIndex < trips.Length; tripIndex++) {
                trips[tripIndex].SetIndex(tripIndex);
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

                    int plannedWaitingTime = WaitingTime(trip, successor);
                    tripSuccessionRobustness[tripIndex, successor.Index] = GetSuccessionRobustness(trip, successor, trip.Duration, plannedWaitingTime);
                }
            }

            // Preprocess whether trips could belong to the same shift
            bool[,] tripsAreSameShift = new bool[trips.Length, trips.Length];
            for (int trip1Index = 0; trip1Index < trips.Length; trip1Index++) {
                Trip trip1 = trips[trip1Index];
                for (int trip2Index = trip1Index; trip2Index < trips.Length; trip2Index++) {
                    Trip trip2 = trips[trip2Index];
                    tripsAreSameShift[trip1.Index, trip2.Index] = WaitingTime(trip1, trip2) <= SaConfig.ShiftWaitingTimeThreshold;
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

        static InternalDriver[] CreateInternalDrivers(string[] internalDriverNames, int[][] internalDriversHomeTravelTimes, bool[][,] internalDriversTrackProficiencies, int[] internalDriverContractTimes, bool[] internalDriverIsInternational) {
            InternalDriver[] internalDrivers = new InternalDriver[internalDriverNames.Length];
            for (int internalDriverIndex = 0; internalDriverIndex < internalDriverNames.Length; internalDriverIndex++) {
                string driverName = internalDriverNames[internalDriverIndex];
                int[] homeTravelTimes = internalDriversHomeTravelTimes[internalDriverIndex];
                bool[,] trackProficiencies = internalDriversTrackProficiencies[internalDriverIndex];
                int contractTime = internalDriverContractTimes[internalDriverIndex];
                bool isInternational = internalDriverIsInternational[internalDriverIndex];

                SalarySettings salaryInfo = isInternational ? SalaryConfig.InternalInternationalSalaryInfo : SalaryConfig.InternalNationalSalaryInfo;

                internalDrivers[internalDriverIndex] = new InternalDriver(internalDriverIndex, internalDriverIndex, driverName, homeTravelTimes, trackProficiencies, contractTime, salaryInfo);
            }
            return internalDrivers;
        }

        static ExternalDriver[][] CreateExternalDrivers(int[][] externalDriversHomeTravelTimes, int indexOffset) {
            ExternalDriver[][] externalDriversByType = new ExternalDriver[DataConfig.ExternalDriverTypes.Length][];
            int allDriverIndex = indexOffset;
            for (int externalDriverTypeIndex = 0; externalDriverTypeIndex < DataConfig.ExternalDriverTypes.Length; externalDriverTypeIndex++) {
                ExternalDriverTypeSettings externalCompanyInfo = DataConfig.ExternalDriverTypes[externalDriverTypeIndex];
                int[] homeTravelTimes = externalDriversHomeTravelTimes[externalDriverTypeIndex];
                SalarySettings salaryInfo = externalCompanyInfo.IsInternational ? SalaryConfig.ExternalInternationalSalaryInfo : SalaryConfig.ExternalNationalSalaryInfo;

                ExternalDriver[] currentTypeDrivers = new ExternalDriver[externalCompanyInfo.MaxShiftCount];
                for (int indexInType = 0; indexInType < externalCompanyInfo.MaxShiftCount; indexInType++) {
                    ExternalDriver newExternalDriver = new ExternalDriver(allDriverIndex, externalDriverTypeIndex, indexInType, homeTravelTimes, salaryInfo);
                    currentTypeDrivers[indexInType] = newExternalDriver;
                    allDriverIndex++;
                }

                externalDriversByType[externalDriverTypeIndex] = currentTypeDrivers;
            }
            return externalDriversByType;
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
                    float[] drivingCostsByDriverType = new float[salarySettingsByDriverType.Length];
                    for (int driverTypeIndex = 0; driverTypeIndex < salarySettingsByDriverType.Length; driverTypeIndex++) {
                        SalarySettings typeSalarySettings = salarySettingsByDriverType[driverTypeIndex];
                        typeSalarySettings.SetDriverTypeIndex(driverTypeIndex);
                        drivingCostsByDriverType[driverTypeIndex] = GetDrivingCost(firstTripInternal, lastTripInternal, typeSalarySettings, timeframeLength);
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

                    shiftInfos[firstTripIndex, lastTripIndex] = new ShiftInfo(drivingTime, maxShiftLengthWithoutTravel, maxShiftLengthWithTravel, minRestTimeAfter, drivingCostsByDriverType, isNightShiftByLaw, isNightShiftByCompanyRules, isWeekendShiftByCompanyRules);
                }
            }

            return shiftInfos;
        }

        static float GetDrivingCost(Trip firstTripInternal, Trip lastTripInternal, SalarySettings salaryInfo, int timeframeLength) {
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
            int drivingTime = Math.Max(salaryInfo.MinPaidShiftTime, drivingEndTimeReal - drivingStartTime);
            int drivingEndTimeAdministrative = drivingStartTime + drivingTime;

            // Determine driving cost from the different salary rates; final block is skipped since we copied beyond timeframe length
            float? shiftContinuingRate = null;
            float drivingCost = 0;
            for (int salaryRateIndex = 0; salaryRateIndex < processedSalaryRates.Count - 1; salaryRateIndex++) {
                SalaryRateBlock salaryRateInfo = processedSalaryRates[salaryRateIndex];
                SalaryRateBlock nextSalaryRateInfo = processedSalaryRates[salaryRateIndex + 1];
                int drivingTimeInRate = GetTimeInRange(drivingStartTime, drivingEndTimeAdministrative, salaryRateInfo.StartTime, nextSalaryRateInfo.StartTime);

                if (drivingTimeInRate == 0) continue;

                // If the shift starts in a continuing rate, store this continuing rate
                if (!shiftContinuingRate.HasValue) {
                    shiftContinuingRate = salaryRateInfo.ContinuingRate;
                }

                float applicableSalaryRate = Math.Max(salaryRateInfo.SalaryRate, shiftContinuingRate.Value);
                drivingCost += drivingTimeInRate * applicableSalaryRate;
            }

            return drivingCost;
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

        public int CarTravelTime(Trip trip1, Trip trip2) {
            return expectedCarTravelTimes[trip1.EndStationAddressIndex, trip2.StartStationAddressIndex];
        }

        public int TravelTimeViaHotel(Trip trip1, Trip trip2) {
            return expectedCarTravelTimes[trip1.EndStationAddressIndex, trip2.StartStationAddressIndex] + RulesConfig.HotelExtraTravelTime;
        }

        public int HalfTravelTimeViaHotel(Trip trip1, Trip trip2) {
            return (expectedCarTravelTimes[trip1.EndStationAddressIndex, trip2.StartStationAddressIndex] + RulesConfig.HotelExtraTravelTime) / 2;
        }

        public int RestTimeWithTravelTime(Trip trip1, Trip trip2, int travelTime) {
            return trip2.StartTime - trip1.EndTime - travelTime;
        }

        public int RestTimeViaHotel(Trip trip1, Trip trip2) {
            return RestTimeWithTravelTime(trip1, trip2, TravelTimeViaHotel(trip1, trip2));
        }

        int WaitingTime(Trip trip1, Trip trip2) {
            return trip2.StartTime - trip1.EndTime - CarTravelTime(trip1, trip2);
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
}

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
        public readonly string[] StationCodes;
        readonly ShiftInfo[,] shiftInfos;
        readonly float[,] tripSuccessionRobustness;
        readonly bool[,] tripSuccession, tripsAreSameShift;
        public readonly InternalDriver[] InternalDrivers;
        public readonly ExternalDriver[][] ExternalDriversByType;
        public readonly Driver[] AllDrivers;

        public Instance(XorShiftRandom rand, Trip[] rawTrips, string[] stationCodes, int[,] plannedCarTravelTimes, string[] internalDriverNames, int[][] internalDriversHomeTravelTimes, bool[][,] internalDriversTrackProficiencies, int internalDriverContractTime, int[] externalDriverCounts, int[][] externalDriversHomeTravelTimes) {
            Rand = rand;
            expectedCarTravelTimes = GetExpectedCarTravelTimes(plannedCarTravelTimes);
            (Trips, tripSuccession, tripSuccessionRobustness, tripsAreSameShift, timeframeLength, UniqueSharedRouteCount) = PrepareTrips(rawTrips, expectedCarTravelTimes);
            StationCodes = stationCodes;
            shiftInfos = GetShiftInfos(Trips, timeframeLength);
            InternalDrivers = CreateInternalDrivers(internalDriverNames, internalDriversHomeTravelTimes, internalDriversTrackProficiencies, internalDriverContractTime);
            ExternalDriversByType = GenerateExternalDrivers(externalDriverCounts, externalDriversHomeTravelTimes, InternalDrivers.Length);

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
                    expectedCarTravelTimes[location1Index, location2Index] = Config.TravelDelayExpectedFunc(plannedTravelTimeBetween);
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
                    int travelTimeBetween = expectedCarTravelTimes[trip1.EndStationIndex, trip2.StartStationIndex];
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
                    tripsAreSameShift[trip1.Index, trip2.Index] = WaitingTime(trip1, trip2) <= Config.ShiftWaitingTimeThreshold;
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
                if (trip.StartStationIndex == trip.EndStationIndex) continue;
                (int lowStationIndex, int highStationIndex) = GetLowHighStationIndices(trip);

                bool isExistingRoute = routeCounts.Any(route => route.Item1 == lowStationIndex && route.Item2 == highStationIndex);
                if (isExistingRoute) {
                    int routeIndex = routeCounts.FindIndex(route => route.Item1 == lowStationIndex && route.Item2 == highStationIndex);
                    routeCounts[routeIndex] = (routeCounts[routeIndex].Item1, routeCounts[routeIndex].Item2, routeCounts[routeIndex].Item3 + 1);
                } else {
                    routeCounts.Add((trip.StartStationIndex, trip.EndStationIndex, 1));
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
                robustnessCost = conflictProb * Config.RobustnessCostFactorSameDuty;
            } else {
                if (areSameProject) {
                    robustnessCost = conflictProb * Config.RobustnessCostFactorSameProject;
                } else {
                    robustnessCost = conflictProb * Config.RobustnessCostFactorDifferentProject;
                }
            }
            return (float)robustnessCost;
        }

        static double GetConflictProbability(int plannedDuration, int waitingTime) {
            double meanDelay = Config.TripMeanDelayFunc(plannedDuration);
            double delayAlpha = Config.TripDelayGammaDistributionAlphaFunc(meanDelay);
            double delayBeta = Config.TripDelayGammaDistributionBetaFunc(meanDelay);
            float delayProb = Config.TripDelayProbability;
            double conflictProbWhenDelayed = 1 - Gamma.CDF(delayAlpha, delayBeta, waitingTime);
            double conflictProb = delayProb * conflictProbWhenDelayed;
            return conflictProb;
        }

        static (int, int) GetLowHighStationIndices(Trip trip) {
            int lowStationIndex, highStationIndex;
            if (trip.StartStationIndex < trip.EndStationIndex) {
                lowStationIndex = trip.StartStationIndex;
                highStationIndex = trip.EndStationIndex;
            } else {
                lowStationIndex = trip.EndStationIndex;
                highStationIndex = trip.StartStationIndex;
            }
            return (lowStationIndex, highStationIndex);
        }

        static InternalDriver[] CreateInternalDrivers(string[] internalDriverNames, int[][] internalDriversHomeTravelTimes, bool[][,] internalDriversTrackProficiencies, int internalDriverContractTime) {
            InternalDriver[] internalDrivers = new InternalDriver[internalDriverNames.Length];
            for (int internalDriverIndex = 0; internalDriverIndex < internalDriverNames.Length; internalDriverIndex++) {
                string driverName = internalDriverNames[internalDriverIndex];
                int[] homeTravelTimes = internalDriversHomeTravelTimes[internalDriverIndex];
                bool[,] trackProficiencies = internalDriversTrackProficiencies[internalDriverIndex];

                internalDrivers[internalDriverIndex] = new InternalDriver(internalDriverIndex, internalDriverIndex, driverName, homeTravelTimes, internalDriverContractTime, trackProficiencies);
            }
            return internalDrivers;
        }

        static ExternalDriver[][] GenerateExternalDrivers(int[] externalDriverCounts, int[][] externalDriversHomeTravelTimes, int indexOffset) {
            ExternalDriver[][] externalDriversByType = new ExternalDriver[externalDriverCounts.Length][];
            int allDriverIndex = indexOffset;
            for (int externalDriverTypeIndex = 0; externalDriverTypeIndex < externalDriverCounts.Length; externalDriverTypeIndex++) {
                int count = externalDriverCounts[externalDriverTypeIndex];
                int[] homeTravelTimes = externalDriversHomeTravelTimes[externalDriverTypeIndex];

                ExternalDriver[] currentTypeDrivers = new ExternalDriver[count];
                externalDriversByType[externalDriverTypeIndex] = currentTypeDrivers;
                for (int indexInType = 0; indexInType < count; indexInType++) {
                    ExternalDriver newExternalDriver = new ExternalDriver(allDriverIndex, externalDriverTypeIndex, indexInType, homeTravelTimes);
                    currentTypeDrivers[indexInType] = newExternalDriver;
                    allDriverIndex++;
                }
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

                    // Determine driving costs for driver categories
                    (float internalDrivingCost, int drivingTimeAtNight, int drivingTimeInWeekend) = GetDrivingCost(firstTripInternal, lastTripInternal, Config.InternalDriverWeekdaySalaryRates, Config.InternalDriverWeekendSalaryRate, Config.InternalDriverMinPaidShiftTime, timeframeLength);
                    (float externalDrivingCost, _, _) = GetDrivingCost(firstTripInternal, lastTripInternal, Config.ExternalDriverWeekdaySalaryRates, Config.ExternalDriverWeekendSalaryRate, Config.ExternalDriverMinPaidShiftTime, timeframeLength);

                    bool isNightShiftByLaw = Config.IsNightShiftByLawFunc(drivingTimeAtNight, drivingTime);
                    bool isNightShiftByCompanyRules = Config.IsNightShiftByCompanyRulesFunc(drivingTimeAtNight, drivingTime);
                    bool isWeekendShiftByCompanyRules = Config.IsWeekendShiftByCompanyRulesFunc(drivingTimeInWeekend, drivingTime);

                    int maxShiftLengthWithoutTravel, maxShiftLengthWithTravel, minRestTimeAfter;
                    if (isNightShiftByLaw) {
                        maxShiftLengthWithoutTravel = Config.NightShiftMaxLengthWithoutTravel;
                        maxShiftLengthWithTravel = Config.NightShiftMaxLengthWithTravel;
                        minRestTimeAfter = Config.NightShiftMinRestTime;
                    } else {
                        maxShiftLengthWithoutTravel = Config.NormalShiftMaxLengthWithoutTravel;
                        maxShiftLengthWithTravel = Config.NormalShiftMaxLengthWithTravel;
                        minRestTimeAfter = Config.NormalShiftMinRestTime;
                    }

                    shiftInfos[firstTripIndex, lastTripIndex] = new ShiftInfo(drivingTime, maxShiftLengthWithoutTravel, maxShiftLengthWithTravel, minRestTimeAfter, internalDrivingCost, externalDrivingCost, isNightShiftByLaw, isNightShiftByCompanyRules, isWeekendShiftByCompanyRules);
                }
            }

            return shiftInfos;
        }

        static (float, int, int) GetDrivingCost(Trip firstTripInternal, Trip lastTripInternal, SalaryRateInfo[] salaryRates, float weekendSalaryRate, int minPaidShiftTime, int timeframeLength) {
            // Repeat salary rate to cover entire week
            int timeframeDayCount = (int)Math.Floor((float)timeframeLength / Config.DayLength) + 1;
            List<SalaryRateInfo> processedSalaryRates = new List<SalaryRateInfo>();
            int salaryTypeIndex = 0;
            bool isCurrentlyWeekend = Config.WeekSalaryTypes[salaryTypeIndex].IsWeekend;
            for (int dayIndex = 0; dayIndex < timeframeDayCount; dayIndex++) {
                for (int i = 0; i < salaryRates.Length; i++) {
                    int rateStartTime = dayIndex * Config.DayLength + salaryRates[i].StartTime;

                    while (salaryTypeIndex + 1 < Config.WeekSalaryTypes.Length && Config.WeekSalaryTypes[salaryTypeIndex + 1].StartTime <= rateStartTime) {
                        salaryTypeIndex++;
                        isCurrentlyWeekend = Config.WeekSalaryTypes[salaryTypeIndex].IsWeekend;

                        SalaryRateInfo previousSalaryRateInfo = i > 0 ? salaryRates[i - 1] : new SalaryRateInfo(-1, 0, false, false);
                        if (isCurrentlyWeekend) {
                            // Start weekend within previous salary rate
                            processedSalaryRates.Add(new SalaryRateInfo(Config.WeekSalaryTypes[salaryTypeIndex].StartTime, weekendSalaryRate, previousSalaryRateInfo.ContinuingRate, previousSalaryRateInfo.IsNight, true));
                        } else {
                            // End weekend within previous salary rate
                            processedSalaryRates.Add(new SalaryRateInfo(Config.WeekSalaryTypes[salaryTypeIndex].StartTime, previousSalaryRateInfo.SalaryRate, previousSalaryRateInfo.ContinuingRate, previousSalaryRateInfo.IsNight, false));
                        }
                    }

                    // Start current salary rate
                    float currentSalaryRate = isCurrentlyWeekend ? weekendSalaryRate : salaryRates[i].SalaryRate;
                    processedSalaryRates.Add(new SalaryRateInfo(rateStartTime, currentSalaryRate, salaryRates[i].ContinuingRate, salaryRates[i].IsNight, isCurrentlyWeekend));
                }
            }

            // Determine driving time, while keeping in mind the minimum paid time
            int drivingStartTime = firstTripInternal.StartTime;
            int drivingEndTimeReal = lastTripInternal.EndTime;
            int drivingTime = Math.Max(minPaidShiftTime, drivingEndTimeReal - drivingStartTime);
            int drivingEndTimeAdministrative = drivingStartTime + drivingTime;

            // Determine driving cost from the different salary rates
            float? shiftContinuingRate = null;
            float drivingCost = 0;
            int drivingTimeAtNight = 0;
            int drivingTimeInWeekend = 0;
            for (int salaryRateIndex = 0; salaryRateIndex < processedSalaryRates.Count - 1; salaryRateIndex++) {
                SalaryRateInfo salaryRateInfo = processedSalaryRates[salaryRateIndex];
                SalaryRateInfo nextSalaryRateInfo = processedSalaryRates[salaryRateIndex + 1];
                int drivingTimeInRate = GetTimeInRange(drivingStartTime, drivingEndTimeAdministrative, salaryRateInfo.StartTime, nextSalaryRateInfo.StartTime);

                if (drivingTimeInRate == 0) continue;

                // If the shift starts in a continuing rate, store this continuing rate
                if (!shiftContinuingRate.HasValue) {
                    shiftContinuingRate = salaryRateInfo.ContinuingRate;
                }

                float applicableSalaryRate = Math.Max(salaryRateInfo.SalaryRate, shiftContinuingRate.Value);
                drivingCost += drivingTimeInRate * applicableSalaryRate;

                if (salaryRateInfo.IsNight.HasValue && salaryRateInfo.IsNight.Value) {
                    drivingTimeAtNight += drivingTimeInRate;
                }
                if (salaryRateInfo.IsNight.HasValue && salaryRateInfo.IsWeekend.Value) {
                    drivingTimeInWeekend += drivingTimeInRate;
                }
            }

            return (drivingCost, drivingTimeAtNight, drivingTimeInWeekend);
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
            return expectedCarTravelTimes[trip1.EndStationIndex, trip2.StartStationIndex];
        }

        public int TravelTimeViaHotel(Trip trip1, Trip trip2) {
            return expectedCarTravelTimes[trip1.EndStationIndex, trip2.StartStationIndex] + Config.HotelExtraTravelTime;
        }

        public int HalfTravelTimeViaHotel(Trip trip1, Trip trip2) {
            return (expectedCarTravelTimes[trip1.EndStationIndex, trip2.StartStationIndex] + Config.HotelExtraTravelTime) / 2;
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

        void DebugLogProcessedSalaryRates(List<SalaryRateInfo> processedSalaryRates) {
            for (int i = 0; i < processedSalaryRates.Count; i++) {
                SalaryRateInfo rate = processedSalaryRates[i];

                int dayNum = rate.StartTime / (24 * 60);
                int hourNum = rate.StartTime % (24 * 60) / 60;

                string[] weekdays = new string[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun", "Mon2" };

                Console.WriteLine("{0,4} {1,2}:00  |  Rate: {3,5}  |  Cont.: {4,5}  |  Night: {5,-5}  |  Weekend: {6,-5}", weekdays[dayNum], hourNum, rate.StartTime, rate.SalaryRate * 60, rate.ContinuingRate * 60, rate.IsNight, rate.IsWeekend);
            }
        }
    }
}

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
        readonly int[,] carTravelTimes;
        public readonly Trip[] Trips;
        public readonly string[] StationCodes;
        readonly ShiftInfo[,] shiftInfos;
        readonly float[,] tripSuccessionRobustness;
        readonly bool[,] tripSuccession, tripsAreSameShift;
        public readonly InternalDriver[] InternalDrivers;
        public readonly ExternalDriver[][] ExternalDriversByType;
        public readonly Driver[] AllDrivers;

        public Instance(XorShiftRandom rand, Trip[] rawTrips, string[] stationCodes, int[,] carTravelTimes, string[] internalDriverNames, int[][] internalDriversHomeTravelTimes, bool[][,] internalDriversTrackProficiencies, int internalDriverContractTime, int[] externalDriverCounts, int[][] externalDriversHomeTravelTimes) {
            Rand = rand;
            this.carTravelTimes = carTravelTimes;
            (Trips, tripSuccession, tripSuccessionRobustness, tripsAreSameShift, timeframeLength, UniqueSharedRouteCount) = PrepareTrips(rawTrips, carTravelTimes);
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

        (Trip[], bool[,], float[,], bool[,], int, int) PrepareTrips(Trip[] rawTrips, int[,] carTravelTimes) {
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
                    float travelTimeBetween = carTravelTimes[trip1.EndStationIndex, trip2.StartStationIndex];
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

                    int travelTimeBetween = CarTravelTime(trip, successor);
                    int waitingTime = WaitingTime(trip, successor);
                    tripSuccessionRobustness[tripIndex, successor.Index] = GetSuccessionRobustness(trip, successor, trip.Duration, travelTimeBetween, waitingTime);
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

        static float GetSuccessionRobustness(Trip trip1, Trip trip2, int plannedDuration, int plannedTravelTime, int waitingTime) {
            double conflictProb = GetConflictProbability(plannedDuration, plannedTravelTime, waitingTime);

            // Trips belong to the same project is their project names are equal and not empty
            bool areSameProject = trip1.ProjectName == trip2.ProjectName && trip1.ProjectName != "";

            double robustnessCost;
            if (areSameProject) {
                robustnessCost = conflictProb * Config.RobustnessCostFactorSameProject;
            } else {
                robustnessCost = conflictProb * Config.RobustnessCostFactorDifferentProject;
            }
            return (float)robustnessCost;
        }

        static double GetConflictProbability(int plannedDuration, int plannedTravelTime, int waitingTime) {
            double conflictProb = 0;

            // Trip delay only
            double tripMeanDelay = Config.TripMeanDelayFunc(plannedDuration);
            double tripDelayAlpha = Config.TripDelayGammaDistributionAlphaFunc(tripMeanDelay);
            double tripDelayBeta = Config.TripDelayGammaDistributionBetaFunc(tripMeanDelay);
            float onlyTripDelayProb = Config.TripDelayProbability * (1 - Config.TravelDelayProbability);
            double conflictProbWhenOnlyTripDelayed = 1 - Gamma.CDF(tripDelayAlpha, 1 / tripDelayBeta, waitingTime);
            conflictProb += onlyTripDelayProb * conflictProbWhenOnlyTripDelayed;

            // Travel delay only
            double travelMeanDelay = Config.TravelMeanDelayFunc(plannedTravelTime);
            double travelDelayAlpha = Config.TravelDelayGammaDistributionAlphaFunc(travelMeanDelay);
            double travelDelayBeta = Config.TravelDelayGammaDistributionBetaFunc(travelMeanDelay);
            float onlyTravelDelayProb = (1 - Config.TripDelayProbability) * Config.TravelDelayProbability;
            double conflictProbWhenOnlyTravelDelayed = 1 - Gamma.CDF(travelDelayAlpha, 1 / travelDelayBeta, waitingTime);
            conflictProb += onlyTravelDelayProb * conflictProbWhenOnlyTravelDelayed;

            // Both trip and travel delays
            // Use approximation of Gamma distribution sum using the Welch–Satterthwaite equation
            double sumOfAlphaBetaQuotients = tripDelayAlpha / tripDelayBeta + travelDelayAlpha / travelDelayBeta;
            double summedAlpha = sumOfAlphaBetaQuotients * sumOfAlphaBetaQuotients / (tripDelayAlpha / tripDelayBeta / tripDelayBeta + travelDelayAlpha / travelDelayBeta / travelDelayBeta);
            double summedBeta = summedAlpha / sumOfAlphaBetaQuotients;
            float bothDelayedProb = Config.TripDelayProbability * Config.TravelDelayProbability;
            double conflictProbWhenBothDelayed = 1 - Gamma.CDF(summedAlpha, 1 / summedBeta, waitingTime);
            conflictProb += bothDelayedProb * conflictProbWhenBothDelayed;

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

                // Contract time
                int minContractTime = (int)Math.Ceiling(internalDriverContractTime * (1 - Config.ContractTimeMaxDeviationFactor));
                int maxContractTime = (int)Math.Floor(internalDriverContractTime * (1 + Config.ContractTimeMaxDeviationFactor));

                internalDrivers[internalDriverIndex] = new InternalDriver(internalDriverIndex, internalDriverIndex, driverName, homeTravelTimes, internalDriverContractTime, minContractTime, maxContractTime, trackProficiencies);
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
                    float internalDrivingCost = GetDrivingCost(firstTripInternal, lastTripInternal, Config.InternalDriverWeekdaySalaryRates, Config.InternalDriverWeekendSalaryRate, Config.InternalDriverMinPaidShiftTime, timeframeLength);
                    float externalDrivingCost = GetDrivingCost(firstTripInternal, lastTripInternal, Config.ExternalDriverWeekdaySalaryRates, Config.ExternalDriverWeekendSalaryRate, Config.ExternalDriverMinPaidShiftTime, timeframeLength);

                    bool isNightShift, isWeekendShift;
                    if (drivingTime == 0) {
                        isNightShift = false;
                        isWeekendShift = false;
                    } else {
                        // Determine fraction of shift during the night
                        int drivingStartTimeInDay = drivingStartTime % Config.DayLength;
                        int drivingEndTimeInDay = drivingEndTime % Config.DayLength;
                        int drivingTimeInNight = GetTimeInRange(drivingStartTimeInDay, drivingEndTimeInDay, Config.NightStartTimeInDay, Config.DayLength) + GetTimeInRange(drivingStartTime, drivingEndTime, 0, Config.NightEndTimeInDay);
                        isNightShift = drivingTimeInNight > Config.NightShiftNightTimeThreshold;

                        // Determine fraction of shift during the weekend
                        isWeekendShift = drivingStartTime > Config.WeekendStartTime && drivingStartTime < Config.WeekendEndTime;
                    }

                    shiftInfos[firstTripIndex, lastTripIndex] = new ShiftInfo(drivingTime, internalDrivingCost, externalDrivingCost, isNightShift, isWeekendShift);
                }
            }

            return shiftInfos;
        }

        /** Preprocess an internal driver's shift lengths and costs */
        static float[,] GetDriverShiftCosts(Trip[] trips, SalaryRateInfo[] salaryRates, float weekendSalaryRate, int minPaidShiftTime, int timeframeLength) {
            float[,] drivingCosts = new float[trips.Length, trips.Length];
            for (int firstTripIndex = 0; firstTripIndex < trips.Length; firstTripIndex++) {
                for (int lastTripIndex = 0; lastTripIndex < trips.Length; lastTripIndex++) {
                    Trip firstTripInternal = trips[firstTripIndex];
                    Trip lastTripInternal = trips[lastTripIndex];

                    // Determine driving cost from the different salary rates
                    float drivingCost = GetDrivingCost(firstTripInternal, lastTripInternal, salaryRates, weekendSalaryRate, minPaidShiftTime, timeframeLength);
                    drivingCosts[firstTripIndex, lastTripIndex] = drivingCost;
                }
            }

            return drivingCosts;
        }

        static float GetDrivingCost(Trip firstTripInternal, Trip lastTripInternal, SalaryRateInfo[] salaryRates, float weekendSalaryRate, int minPaidShiftTime, int timeframeLength) {
            // Repeat salary rate to cover entire week
            int timeframeDayCount = (int)Math.Ceiling((float)timeframeLength / Config.DayLength);
            List<SalaryRateInfo> processedSalaryRates = new List<SalaryRateInfo>();
            bool isCurrentlyWeekend = false;
            bool isWeekendDone = false;
            for (int dayIndex = 0; dayIndex < timeframeDayCount; dayIndex++) {
                for (int i = 0; i < salaryRates.Length; i++) {
                    int rateStartTime = dayIndex * Config.DayLength + salaryRates[i].StartTime;

                    if (isCurrentlyWeekend) {
                        if (rateStartTime > Config.WeekendEndTime) {
                            isCurrentlyWeekend = false;
                            isWeekendDone = true;

                            // End weekend within previous salary rate
                            processedSalaryRates.Add(new SalaryRateInfo(Config.WeekendEndTime, processedSalaryRates[^1].SalaryRate));

                            // Start current weekday salary rate
                            processedSalaryRates.Add(new SalaryRateInfo(rateStartTime, salaryRates[i].SalaryRate));
                        }
                    } else {
                        if (!isWeekendDone && rateStartTime > Config.WeekendStartTime) {
                            isCurrentlyWeekend = true;

                            // Start weekend within previous salary rate
                            processedSalaryRates.Add(new SalaryRateInfo(Config.WeekendStartTime, weekendSalaryRate));
                        } else {
                            // Start current weekday salary rate
                            processedSalaryRates.Add(new SalaryRateInfo(rateStartTime, salaryRates[i].SalaryRate));
                        }
                    }
                }
            }

            // Determine driving time, while keeping in mind the minimum paid time
            int drivingStartTime = firstTripInternal.StartTime;
            int drivingEndTime = lastTripInternal.EndTime;
            int drivingTime = Math.Max(minPaidShiftTime, drivingEndTime - drivingStartTime);

            // Determine driving cost from the different salary rates
            float drivingCost = 0;
            for (int salaryRateIndex = 0; salaryRateIndex < processedSalaryRates.Count - 1; salaryRateIndex++) {
                SalaryRateInfo salaryRateInfo = processedSalaryRates[salaryRateIndex];
                SalaryRateInfo nextSalaryRateInfo = processedSalaryRates[salaryRateIndex + 1];
                int drivingTimeInRate = GetTimeInRange(drivingStartTime, drivingEndTime, salaryRateInfo.StartTime, nextSalaryRateInfo.StartTime);
                drivingCost += drivingTimeInRate * salaryRateInfo.SalaryRate;
            }
            SalaryRateInfo lastSalaryRate = processedSalaryRates[^1];
            int shiftLengthBeforeLast = Math.Max(0, lastSalaryRate.StartTime - drivingStartTime);
            int shiftLengthInLastRate = Math.Max(0, drivingTime - shiftLengthBeforeLast);
            drivingCost += shiftLengthInLastRate * lastSalaryRate.SalaryRate;

            return drivingCost;
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
            return carTravelTimes[trip1.EndStationIndex, trip2.StartStationIndex];
        }

        public int TravelTimeViaHotel(Trip trip1, Trip trip2) {
            return carTravelTimes[trip1.EndStationIndex, trip2.StartStationIndex] + Config.HotelExtraTravelTime;
        }

        public int HalfTravelTimeViaHotel(Trip trip1, Trip trip2) {
            return (carTravelTimes[trip1.EndStationIndex, trip2.StartStationIndex] + Config.HotelExtraTravelTime) / 2;
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
    }
}

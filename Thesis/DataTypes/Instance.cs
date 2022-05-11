using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class Instance {
        public readonly int TimeframeLength;
        readonly int[,] CarTravelTimes;
        public readonly Trip[] Trips;
        public readonly string[] StationCodes;
        readonly ShiftInfo[,] ShiftInfos;
        readonly bool[,] TripSuccession, TripsAreSameShift;
        public readonly InternalDriver[] InternalDrivers;
        public readonly ExternalDriver[][] ExternalDriversByType;
        public readonly Driver[] AllDrivers;

        public Instance(Trip[] rawTrips, string[] stationCodes, int[,] carTravelTimes, string[] internalDriverNames, int[][] internalDriversHomeTravelTimes, bool[][,] internalDriversTrackProficiencies, int internalDriverContractTime, int[] externalDriverCounts, int[][] externalDriversHomeTravelTimes) {
            CarTravelTimes = carTravelTimes;
            (Trips, TripSuccession, TripsAreSameShift, TimeframeLength) = PrepareTrips(rawTrips, carTravelTimes);
            StationCodes = stationCodes;
            ShiftInfos = GetShiftInfos(Trips, TimeframeLength);
            InternalDrivers = CreateInternalDrivers(Trips, internalDriverNames, internalDriversHomeTravelTimes, internalDriversTrackProficiencies, internalDriverContractTime, TimeframeLength);
            ExternalDriversByType = GenerateExternalDrivers(Trips, externalDriverCounts, externalDriversHomeTravelTimes, InternalDrivers.Length, TimeframeLength);

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

        (Trip[], bool[,], bool[,], int) PrepareTrips(Trip[] rawTrips, int[,] carTravelTimes) {
            // Sort trips by start time
            Trip[] trips = rawTrips.OrderBy(trip => trip.StartTime).ToArray();

            // Add trip indices
            for (int tripIndex = 0; tripIndex < trips.Length; tripIndex++) {
                trips[tripIndex].Index = tripIndex;
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

            // Create 2D bool array for precedance relations
            bool[,] tripSuccession = new bool[trips.Length, trips.Length];
            for (int tripIndex = 0; tripIndex < trips.Length; tripIndex++) {
                Trip trip = trips[tripIndex];
                for (int successorIndex = 0; successorIndex < trip.Successors.Count; successorIndex++) {
                    Trip successor = trip.Successors[successorIndex];
                    tripSuccession[tripIndex, successor.Index] = true;
                }
            }

            // Preprocess whether trips belong to the same shift
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

            return (trips, tripSuccession, tripsAreSameShift, timeframeLength);
        }

        static InternalDriver[] CreateInternalDrivers(Trip[] trips, string[] internalDriverNames, int[][] internalDriversHomeTravelTimes, bool[][,] internalDriversTrackProficiencies, int internalDriverContractTime, int timeframeLength) {
            InternalDriver[] internalDrivers = new InternalDriver[internalDriverNames.Length];
            for (int internalDriverIndex = 0; internalDriverIndex < internalDriverNames.Length; internalDriverIndex++) {
                string driverName = internalDriverNames[internalDriverIndex];
                int[] homeTravelTimes = internalDriversHomeTravelTimes[internalDriverIndex];
                bool[,] trackProficiencies = internalDriversTrackProficiencies[internalDriverIndex];

                // Contract time
                int minWorkedTime = (int)Math.Ceiling(internalDriverContractTime * Config.MinContractTimeFraction);
                int maxWorkedTime = (int)Math.Floor(internalDriverContractTime * Config.MaxContractTimeFraction);

                internalDrivers[internalDriverIndex] = new InternalDriver(internalDriverIndex, internalDriverIndex, driverName, homeTravelTimes, minWorkedTime, maxWorkedTime, trackProficiencies);
            }
            return internalDrivers;
        }

        static ExternalDriver[][] GenerateExternalDrivers(Trip[] trips, int[] externalDriverCounts, int[][] externalDriversHomeTravelTimes, int indexOffset, int timeframeLength) {
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
            return ShiftInfos[trip1.Index, trip2.Index];
        }

        public bool IsValidPrecedence(Trip trip1, Trip trip2) {
            return TripSuccession[trip1.Index, trip2.Index];
        }

        public int CarTravelTime(Trip trip1, Trip trip2) {
            return CarTravelTimes[trip1.EndStationIndex, trip2.StartStationIndex];
        }

        public int TravelTimeViaHotel(Trip trip1, Trip trip2) {
            return CarTravelTimes[trip1.EndStationIndex, trip2.StartStationIndex] + Config.HotelExtraTravelTime;
        }

        public int HalfTravelTimeViaHotel(Trip trip1, Trip trip2) {
            return (CarTravelTimes[trip1.EndStationIndex, trip2.StartStationIndex] + Config.HotelExtraTravelTime) / 2;
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
            return TripsAreSameShift[trip1.Index, trip2.Index];
        }
    }
}

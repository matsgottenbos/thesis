using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class Instance {
        readonly int[,] CarTravelTimes;
        public readonly Trip[] Trips;
        readonly bool[,] TripSuccession, TripsAreSameShift;
        public readonly InternalDriver[] InternalDrivers;
        public readonly ExternalDriver[][] ExternalDriversByType;
        public readonly Driver[] AllDrivers;

        public Instance(Trip[] rawTrips, int[,] carTravelTimes, string[] internalDriverNames, int[][] internalDriversHomeTravelTimes, bool[][,] internalDriversTrackProficiencies, int internalDriverContractTime, int[] externalDriverCounts, int[][] externalDriversHomeTravelTimes) {
            CarTravelTimes = carTravelTimes;
            (Trips, TripSuccession, TripsAreSameShift) = PrepareTrips(rawTrips, carTravelTimes);
            InternalDrivers = CreateInternalDrivers(Trips, carTravelTimes, internalDriverNames, internalDriversHomeTravelTimes, internalDriversTrackProficiencies, internalDriverContractTime);
            ExternalDriversByType = GenerateExternalDrivers(Trips, carTravelTimes, externalDriverCounts, externalDriversHomeTravelTimes, InternalDrivers.Length);

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

        (Trip[], bool[,], bool[,]) PrepareTrips(Trip[] rawTrips, int[,] carTravelTimes) {
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

            return (trips, tripSuccession, tripsAreSameShift);
        }

        InternalDriver[] CreateInternalDrivers(Trip[] trips, int[,] carTravelTimes, string[] internalDriverNames, int[][] internalDriversHomeTravelTimes, bool[][,] internalDriversTrackProficiencies, int internalDriverContractTime) {
            InternalDriver[] internalDrivers = new InternalDriver[internalDriverNames.Length];
            for (int internalDriverIndex = 0; internalDriverIndex < internalDriverNames.Length; internalDriverIndex++) {
                string driverName = internalDriverNames[internalDriverIndex];
                int[] homeTravelTimes = internalDriversHomeTravelTimes[internalDriverIndex];
                bool[,] trackProficiencies = internalDriversTrackProficiencies[internalDriverIndex];

                // Contract time
                int minWorkedTime = (int)Math.Ceiling(internalDriverContractTime * Config.MinContractTimeFraction);
                int maxWorkedTime = (int)Math.Floor(internalDriverContractTime * Config.MaxContractTimeFraction);

                // Preprocess shift lengths and costs
                (int[,] drivingTimes, float[,] drivingCosts, int[,] shiftLengthsWithPickup, float[,] shiftCostsWithPickup) = GetDriverShiftLengthsAndCosts(Config.InternalDriverUnpaidTravelTimePerShift, trips, homeTravelTimes, carTravelTimes, Config.InternalDriverDailySalaryRates, Config.InternalDriverTravelSalaryRate);

                internalDrivers[internalDriverIndex] = new InternalDriver(internalDriverIndex, internalDriverIndex, driverName, homeTravelTimes, drivingTimes, drivingCosts, shiftLengthsWithPickup, shiftCostsWithPickup, minWorkedTime, maxWorkedTime, trackProficiencies);
            }
            return internalDrivers;
        }

        ExternalDriver[][] GenerateExternalDrivers(Trip[] trips, int[,] carTravelTimes, int[] externalDriverCounts, int[][] externalDriversHomeTravelTimes, int indexOffset) {
            ExternalDriver[][] externalDriversByType = new ExternalDriver[externalDriverCounts.Length][];
            int allDriverIndex = indexOffset;
            for (int externalDriverTypeIndex = 0; externalDriverTypeIndex < externalDriverCounts.Length; externalDriverTypeIndex++) {
                int count = externalDriverCounts[externalDriverTypeIndex];
                int[] homeTravelTimes = externalDriversHomeTravelTimes[externalDriverTypeIndex];

                // Preprocess shift lengths and costs
                (int[,] drivingTimes, float[,] drivingCosts, int[,] shiftLengthsWithPickup, float[,] shiftCostsWithPickup) = GetDriverShiftLengthsAndCosts(0, trips, homeTravelTimes, carTravelTimes, Config.ExternalDriverDailySalaryRates, Config.ExternalDriverTravelSalaryRate);

                ExternalDriver[] currentTypeDrivers = new ExternalDriver[count];
                externalDriversByType[externalDriverTypeIndex] = currentTypeDrivers;
                for (int indexInType = 0; indexInType < count; indexInType++) {
                    ExternalDriver newExternalDriver = new ExternalDriver(allDriverIndex, externalDriverTypeIndex, indexInType, homeTravelTimes, drivingTimes, drivingCosts, shiftLengthsWithPickup, shiftCostsWithPickup);
                    currentTypeDrivers[indexInType] = newExternalDriver;
                    allDriverIndex++;
                }
            }
            return externalDriversByType;
        }

        /** Preprocess an internal driver's shift lengths and costs */
        (int[,], float[,], int[,], float[,]) GetDriverShiftLengthsAndCosts(int unpaidTravelTimePerShift, Trip[] trips, int[] oneWayTravelTimes, int[,] carTravelTimes, SalaryRateInfo[] salaryRates, float travelSalaryRate) {
            int[,] drivingTimes = new int[trips.Length, trips.Length];
            float[,] drivingCosts = new float[trips.Length, trips.Length];
            int[,] shiftLengthsWithPickup = new int[trips.Length, trips.Length];
            float[,] shiftCostsWithPickup = new float[trips.Length, trips.Length];
            for (int firstTripIndex = 0; firstTripIndex < trips.Length; firstTripIndex++) {
                for (int lastTripIndex = 0; lastTripIndex < trips.Length; lastTripIndex++) {
                    Trip firstTripInternal = trips[firstTripIndex];
                    Trip lastTripInternal = trips[lastTripIndex];

                    // Determine driving cost from the different salary rates
                    (int drivingTime, float drivingCost) = GetDrivingTimeAndCost(firstTripInternal, lastTripInternal, salaryRates);

                    // Determine driving and travel time
                    int travelTimeWithPickup = carTravelTimes[lastTripInternal.EndStationIndex, firstTripInternal.StartStationIndex] + 2 * oneWayTravelTimes[firstTripInternal.StartStationIndex];
                    int paidTravelTimeWithPickup = Math.Max(0, travelTimeWithPickup - unpaidTravelTimePerShift);
                    int shiftLengthWithPickup = drivingTime + travelTimeWithPickup;

                    // Determine full shift costs
                    float travelCostWithPickup = paidTravelTimeWithPickup * travelSalaryRate;
                    float shiftCostWithPickup = drivingCost + travelCostWithPickup;

                    // Store determine values
                    drivingTimes[firstTripIndex, lastTripIndex] = drivingTime;
                    drivingCosts[firstTripIndex, lastTripIndex] = drivingCost;
                    shiftLengthsWithPickup[firstTripIndex, lastTripIndex] = shiftLengthWithPickup;
                    shiftCostsWithPickup[firstTripIndex, lastTripIndex] = shiftCostWithPickup;
                }
            }

            return (drivingTimes, drivingCosts, shiftLengthsWithPickup, shiftCostsWithPickup);
        }

        (int, float) GetDrivingTimeAndCost(Trip firstTripInternal, Trip lastTripInternal, SalaryRateInfo[] salaryRates) {
            // Process salary rate to cover two days
            SalaryRateInfo[] processedSalaryRates = new SalaryRateInfo[2 * salaryRates.Length];
            for (int i = 0; i < salaryRates.Length; i++) {
                processedSalaryRates[i] = salaryRates[i];
                processedSalaryRates[salaryRates.Length + i] = new SalaryRateInfo(salaryRates[i].StartTime + Config.DayLength, salaryRates[i].SalaryRate);
            }

            // Determine driving time
            int drivingTime = Math.Max(0, lastTripInternal.EndTime - firstTripInternal.StartTime);

            // Determine driving start and end time in the day
            int shiftDayNum = (int)Math.Floor((float)firstTripInternal.StartTime / Config.DayLength); // NB: floor so it works with negative values too
            int drivingStartTimeInDay = firstTripInternal.StartTime - shiftDayNum * Config.DayLength;
            int drivingEndTimeInDay = lastTripInternal.EndTime - shiftDayNum * Config.DayLength;

            // Determine driving cost from the different salary rates
            float drivingCost = 0;
            for (int salaryRateIndex = 0; salaryRateIndex < processedSalaryRates.Length - 1; salaryRateIndex++) {
                SalaryRateInfo salaryRateInfo = processedSalaryRates[salaryRateIndex];
                SalaryRateInfo nextSalaryRateInfo = processedSalaryRates[salaryRateIndex + 1];
                int shiftLengthBefore = Math.Max(0, salaryRateInfo.StartTime - drivingStartTimeInDay);
                int shiftLengthAfter = Math.Max(0, drivingEndTimeInDay - nextSalaryRateInfo.StartTime);
                int drivingTimeInRate = Math.Max(0, drivingTime - shiftLengthBefore - shiftLengthAfter);
                drivingCost += drivingTimeInRate * salaryRateInfo.SalaryRate;
            }
            SalaryRateInfo lastSalaryRate = processedSalaryRates[^1];
            int shiftLengthBeforeLast = Math.Max(0, lastSalaryRate.StartTime - drivingStartTimeInDay);
            int shiftLengthInLastRate = Math.Max(0, drivingTime - shiftLengthBeforeLast);
            drivingCost += shiftLengthInLastRate * lastSalaryRate.SalaryRate;

            return (drivingTime, drivingCost);
        }


        /* Helper methods */

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

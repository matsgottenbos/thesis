using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class Generator {
        readonly Random rand;

        public Generator(Random rand) {
            this.rand = rand;
        }

        public Instance GenerateInstance() {
            (int[,] trainTravelTimes, int[,] carTravelTimes) = GenerateTravelTimes();
            (Trip[] trips, bool[,] tripSuccession) = GenerateAllTrips(trainTravelTimes);
            InternalDriver[] internalDrivers = GenerateInternalDrivers(trips, carTravelTimes);
            ExternalDriver[][] externalDriversByType = GenerateExternalDrivers(trips, carTravelTimes, internalDrivers.Length);

            List<Driver> allDriversList = new List<Driver>();
            allDriversList.AddRange(internalDrivers);
            for (int i = 0; i < externalDriversByType.Length; i++) {
                allDriversList.AddRange(externalDriversByType[i]);
            }
            Driver[] allDrivers = allDriversList.ToArray();

            Instance instance = new Instance(trainTravelTimes, carTravelTimes, trips, tripSuccession, internalDrivers, externalDriversByType, allDrivers);

            // Pass instance object to drivers
            for (int driverIndex = 0; driverIndex < allDrivers.Length; driverIndex++) {
                allDrivers[driverIndex].SetInstance(instance);
            }

            return instance;
        }

        (int[,], int[,]) GenerateTravelTimes() {
            int[,] trainTravelTimes = new int[Config.GenStationCount, Config.GenStationCount];
            int[,] carTravelTimes = new int[Config.GenStationCount, Config.GenStationCount];
            for (int i = 0; i < Config.GenStationCount; i++) {
                for (int j = i; j < Config.GenStationCount; j++) {
                    if (i == j) continue;

                    // Train travel times are randomly generated within [minDist, maxDist]
                    int trainTravelTime = (int)(rand.NextDouble() * (Config.GenMaxStationTravelTime - Config.GenMinStationTravelTime) + Config.GenMinStationTravelTime);
                    trainTravelTimes[i, j] = trainTravelTime;
                    trainTravelTimes[j, i] = trainTravelTime;

                    // Car travel times are randomly generated within [0.5, 1.5] times the train travel times
                    int carTravelTime = (int)(trainTravelTime * (rand.NextDouble() + 0.5f));
                    carTravelTimes[i, j] = carTravelTime;
                    carTravelTimes[j, i] = carTravelTime;
                }
            }
            return (trainTravelTimes, carTravelTimes);
        }

        (Trip[], bool[,]) GenerateAllTrips(int[,] trainTravelTimes) {
            // Generate trips
            Trip[] trips = new Trip[Config.GenTripCount];
            for (int tripIndex = 0; tripIndex < Config.GenTripCount; tripIndex++) {
                // Stations
                List<int> tripStations = new List<int>();
                int tripStationCount = rand.Next(2, Config.GenMaxStationCountPerTrip + 1);
                while (tripStations.Count < tripStationCount) {
                    int randomStation = rand.Next(Config.GenStationCount);
                    if (!tripStations.Contains(randomStation)) tripStations.Add(randomStation);
                }

                // Start and end time
                int tripDuration = 0;
                for (int j = 0; j < tripStations.Count - 1; j++) {
                    int station1Index = tripStations[j];
                    int station2Index = tripStations[j + 1];
                    tripDuration += trainTravelTimes[station1Index, station2Index];
                }
                int startTime = (int)(rand.NextDouble() * (Config.GenTimeframeLength - tripDuration));
                int endTime = startTime + tripDuration;

                Trip trip = new Trip(-1, tripStations, startTime, endTime, tripDuration);
                trips[tripIndex] = trip;
            }

            // Sort trips by start time
            trips = trips.OrderBy(trip => trip.StartTime).ToArray();

            // Add trip indices
            for (int tripIndex = 0; tripIndex < trips.Length; tripIndex++) {
                trips[tripIndex].Index = tripIndex;
            }

            // Generate precedence constraints
            for (int trip1Index = 0; trip1Index < Config.GenTripCount; trip1Index++) {
                for (int trip2Index = trip1Index; trip2Index < Config.GenTripCount; trip2Index++) {
                    Trip trip1 = trips[trip1Index];
                    Trip trip2 = trips[trip2Index];
                    float travelTimeBetween = trainTravelTimes[trip1.Stations[trip1.Stations.Count - 1], trip2.Stations[0]];
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

            return (trips, tripSuccession);
        }

        InternalDriver[] GenerateInternalDrivers(Trip[] trips, int[,] carTravelTimes) {
            InternalDriver[] internalDrivers = new InternalDriver[Config.GenInternalDriverCount];
            for (int internalDriverIndex = 0; internalDriverIndex < Config.GenInternalDriverCount; internalDriverIndex++) {
                // Track proficiencies
                bool[,] trackProficiencies = new bool[Config.GenStationCount, Config.GenStationCount];
                for (int i = 0; i < Config.GenStationCount; i++) {
                    for (int j = i; j < Config.GenStationCount; j++) {
                        bool isProficient;
                        if (i == j) {
                            isProficient = true;
                        } else {
                            isProficient = rand.NextDouble() < Config.GenTrackProficiencyProb;
                        }

                        trackProficiencies[i, j] = isProficient;
                        trackProficiencies[j, i] = isProficient;
                    }
                }

                // Travel times
                int[] oneWayTravelTimes = new int[Config.GenStationCount];
                int[] twoWayPayedTravelTimes = new int[Config.GenStationCount];
                for (int i = 0; i < Config.GenStationCount; i++) {
                    int oneWayTravelTime = (int)(rand.NextDouble() * Config.GenMaxStationTravelTime);
                    int twoWayPayedTravelTime = Math.Max(0, 2 * oneWayTravelTime - Config.InternalDriverUnpaidTravelTimePerShift);
                    oneWayTravelTimes[i] = oneWayTravelTime;
                    twoWayPayedTravelTimes[i] = twoWayPayedTravelTime;
                }

                // Contract time
                int contactTime = (int)(rand.NextDouble() * (Config.GenMaxContractTime - Config.GenMinContractTime) + Config.GenMinContractTime);
                int minWorkedTime = (int)Math.Ceiling(contactTime * Config.MinContractTimeFraction);
                int maxWorkedTime = (int)Math.Floor(contactTime * Config.MaxContractTimeFraction);

                // Preprocess shift lengths and costs
                (int[,] drivingTimes, float[,] drivingCosts, int[,] shiftLengthsWithoutPickup, int[,] shiftLengthsWithPickup, float[,] shiftCostsWithPickup) = GetDriverShiftLengthsAndCosts(Config.InternalDriverUnpaidTravelTimePerShift, trips, oneWayTravelTimes, carTravelTimes, Config.InternalDriverDailySalaryRates);

                internalDrivers[internalDriverIndex] = new InternalDriver(internalDriverIndex, internalDriverIndex, oneWayTravelTimes, twoWayPayedTravelTimes, drivingTimes, drivingCosts, shiftLengthsWithoutPickup, shiftLengthsWithPickup, shiftCostsWithPickup, minWorkedTime, maxWorkedTime, trackProficiencies);
            }
            return internalDrivers;
        }

        ExternalDriver[][] GenerateExternalDrivers(Trip[] trips, int[,] carTravelTimes, int indexOffset) {
            ExternalDriver[][] externalDriversByType = new ExternalDriver[Config.GenExternaDriverTypeCount][];
            int allDriverIndex = indexOffset;
            for (int externalDriverTypeIndex = 0; externalDriverTypeIndex < Config.GenExternaDriverTypeCount; externalDriverTypeIndex++) {
                // Travel times
                int[] oneWayTravelTimes = new int[Config.GenStationCount];
                int[] twoWayPayedTravelTimes = new int[Config.GenStationCount];
                for (int i = 0; i < Config.GenStationCount; i++) {
                    int oneWayTravelTime = rand.Next(Config.GenMaxStationTravelTime + 1);
                    int twoWayPayedTravelTime = 2 * oneWayTravelTime;
                    oneWayTravelTimes[i] = oneWayTravelTime;
                    twoWayPayedTravelTimes[i] = twoWayPayedTravelTime;
                }

                // Preprocess shift lengths and costs
                (int[,] drivingTimes, float[,] drivingCosts, int[,] shiftLengthsWithoutPickup, int[,] shiftLengthsWithPickup, float[,] shiftCostsWithPickup) = GetDriverShiftLengthsAndCosts(0, trips, oneWayTravelTimes, carTravelTimes, Config.ExternalDriverDailySalaryRates);

                // Number of external drivers of this type
                int count = rand.Next(Config.GenExternalDriverMinCountPerType, Config.GenExternalDriverMaxCountPerType + 1);

                ExternalDriver[] currentTypeDrivers = new ExternalDriver[count];
                externalDriversByType[externalDriverTypeIndex] = currentTypeDrivers;
                for (int indexInType = 0; indexInType < count; indexInType++) {
                    ExternalDriver newExternalDriver = new ExternalDriver(allDriverIndex, externalDriverTypeIndex, indexInType, oneWayTravelTimes, twoWayPayedTravelTimes, drivingTimes, drivingCosts, shiftLengthsWithoutPickup, shiftLengthsWithPickup, shiftCostsWithPickup);
                    currentTypeDrivers[indexInType] = newExternalDriver;
                    allDriverIndex++;
                }
            }
            return externalDriversByType;
        }

        /** Preprocess an internal driver's shift lengths and costs */
        (int[,], float[,], int[,], int[,], float[,]) GetDriverShiftLengthsAndCosts(int unpaidTravelTimePerShift, Trip[] trips, int[] oneWayTravelTimes, int[,] carTravelTimes, SalaryRateInfo[] salaryRates) {
            int[,] drivingTimes = new int[trips.Length, trips.Length];
            float[,] drivingCosts = new float[trips.Length, trips.Length];
            int[,] shiftLengthsWithoutPickup = new int[trips.Length, trips.Length];
            int[,] shiftLengthsWithPickup = new int[trips.Length, trips.Length];
            float[,] shiftCostsWithPickup = new float[trips.Length, trips.Length];
            for (int firstTripIndex = 0; firstTripIndex < trips.Length; firstTripIndex++) {
                for (int lastTripIndex = 0; lastTripIndex < trips.Length; lastTripIndex++) {
                    Trip firstTripInternal = trips[firstTripIndex];
                    Trip lastTripInternal = trips[lastTripIndex];

                    // Determine driving cost from the different salary rates
                    (int drivingTime, float drivingCost) = GetDrivingTimeAndCost(firstTripInternal, lastTripInternal, salaryRates);

                    // Determine driving and travel time
                    int travelTimeWithoutPickup = oneWayTravelTimes[firstTripInternal.FirstStation] + oneWayTravelTimes[lastTripInternal.LastStation];
                    int shiftLengthWithoutPickup = drivingTime + travelTimeWithoutPickup;
                    int travelTimeWithPickup = carTravelTimes[lastTripInternal.LastStation, firstTripInternal.FirstStation] + 2 * oneWayTravelTimes[firstTripInternal.FirstStation];
                    int payedTravelTimeWithPickup = Math.Max(0, travelTimeWithPickup - unpaidTravelTimePerShift);
                    int shiftLengthWithPickup = drivingTime + travelTimeWithPickup;

                    // Determine full shift costs
                    float travelCostWithPickup = payedTravelTimeWithPickup * Config.InternalDriverTravelSalaryRate;
                    float shiftCostWithPickup = drivingCost + travelCostWithPickup;

                    // Store determine values
                    drivingTimes[firstTripIndex, lastTripIndex] = drivingTime;
                    drivingCosts[firstTripIndex, lastTripIndex] = drivingCost;
                    shiftLengthsWithoutPickup[firstTripIndex, lastTripIndex] = shiftLengthWithoutPickup;
                    shiftLengthsWithPickup[firstTripIndex, lastTripIndex] = shiftLengthWithPickup;
                    shiftCostsWithPickup[firstTripIndex, lastTripIndex] = shiftCostWithPickup;
                }
            }

            return (drivingTimes, drivingCosts, shiftLengthsWithoutPickup, shiftLengthsWithPickup, shiftCostsWithPickup);
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
    }
}

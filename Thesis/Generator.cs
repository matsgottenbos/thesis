﻿using System;
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
            (float[,] trainTravelTimes, float[,] carTravelTimes) = GenerateTravelTimes();
            (Trip[] trips, Trip[][] tripsPerDay, bool[,] tripSuccession) = GenerateAllTrips(trainTravelTimes);
            Driver[] drivers = GenerateDrivers();
            return new Instance(trainTravelTimes, carTravelTimes, trips, tripSuccession, tripsPerDay, drivers);
        }

        (float[,], float[,]) GenerateTravelTimes() {
            float[,] trainTravelTimes = new float[Config.GenStationCount, Config.GenStationCount];
            float[,] carTravelTimes = new float[Config.GenStationCount, Config.GenStationCount];
            for (int i = 0; i < Config.GenStationCount; i++) {
                for (int j = i; j < Config.GenStationCount; j++) {
                    if (i == j) continue;

                    // Train travel times are randomly generated within [1, maxDist]
                    float trainTravelTime = (float)rand.NextDouble() * (Config.GenMaxDist - 1) + 1;
                    trainTravelTimes[i, j] = trainTravelTime;
                    trainTravelTimes[j, i] = trainTravelTime;

                    // Car travel times are randomly generated within [0.5, 1.5] times the train travel times
                    float carTravelTime = trainTravelTime * ((float)rand.NextDouble() + 0.5f);
                    carTravelTimes[i, j] = carTravelTime;
                    carTravelTimes[j, i] = carTravelTime;
                }
            }
            return (trainTravelTimes, carTravelTimes);
        }

        Trip[] GenerateTripsOneDay(float[,] trainTravelTimes, int dayIndex) {
            // Generate trips
            Trip[] dayTrips = new Trip[Config.GenTripCountPerDay];
            for (int dayTripIndex = 0; dayTripIndex < Config.GenTripCountPerDay; dayTripIndex++) {
                // Stations
                List<int> tripStations = new List<int>();
                int tripStationCount = rand.Next(2, Config.GenMaxStationCountPerTrip + 1);
                while (tripStations.Count < tripStationCount) {
                    int randomStation = rand.Next(Config.GenStationCount);
                    if (!tripStations.Contains(randomStation)) tripStations.Add(randomStation);
                }

                // Start and end time
                float tripDuration = 0;
                for (int j = 0; j < tripStations.Count - 1; j++) {
                    int station1Index = tripStations[j];
                    int station2Index = tripStations[j + 1];
                    tripDuration += trainTravelTimes[station1Index, station2Index];
                }
                float startTime = (float)rand.NextDouble() * (Config.DayLength - tripDuration);
                float endTime = startTime + tripDuration;

                // Driving cost
                float drivingCost = tripDuration * Config.HourlyRate;

                Trip trip = new Trip(-1, tripStations, dayIndex, startTime, endTime, tripDuration, drivingCost);
                dayTrips[dayTripIndex] = trip;
            }

            // Sort trips by start time
            dayTrips = dayTrips.OrderBy(trip => trip.StartTime).ToArray();

            // Generate precedence constraints within day
            for (int dayTrip1Index = 0; dayTrip1Index < Config.GenTripCountPerDay; dayTrip1Index++) {
                for (int dayTrip2Index = dayTrip1Index; dayTrip2Index < Config.GenTripCountPerDay; dayTrip2Index++) {
                    Trip trip1 = dayTrips[dayTrip1Index];
                    Trip trip2 = dayTrips[dayTrip2Index];
                    float travelTimeBetween = trainTravelTimes[trip1.Stations[trip1.Stations.Count - 1], trip2.Stations[0]];
                    if (trip1.EndTime + travelTimeBetween <= trip2.StartTime) {
                        trip1.AddSuccessor(trip2, dayTrip2Index);
                    }
                }
            }

            return dayTrips;
        }

        (Trip[], Trip[][], bool[,]) GenerateAllTrips(float[,] stationTravelTimes) {
            // Generate days
            Trip[][] tripsPerDay = new Trip[Config.GenDayCount][];
            List<Trip> tripsList = new List<Trip>();
            for (int dayIndex = 0; dayIndex < Config.GenDayCount; dayIndex++) {
                Trip[] dayTrips = GenerateTripsOneDay(stationTravelTimes, dayIndex);
                tripsPerDay[dayIndex] = dayTrips;
                tripsList.AddRange(dayTrips);
            }
            Trip[] trips = tripsList.ToArray();

            // Add trip indices
            for (int tripIndex = 0; tripIndex < trips.Length; tripIndex++) {
                trips[tripIndex].Index = tripIndex;
            }

            // Generate precedence constraints between days
            for (int dayIndex = 0; dayIndex < Config.GenDayCount - 1; dayIndex++) {
                Trip[] day1Trips = tripsPerDay[dayIndex];
                Trip[] day2Trips = tripsPerDay[dayIndex + 1];

                for (int day1TripIndex = 0; day1TripIndex < day1Trips.Length; day1TripIndex++) {
                    for (int day2TripIndex = 0; day2TripIndex < day2Trips.Length; day2TripIndex++) {
                        day1Trips[day1TripIndex].AddSuccessor(day2Trips[day2TripIndex], null);
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

            return (trips, tripsPerDay, tripSuccession);
        }

        Driver[] GenerateDrivers() {
            Driver[] drivers = new Driver[Config.GenDriverCount];
            for (int driverIndex = 0; driverIndex < Config.GenDriverCount; driverIndex++) {
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
                float[] twoWayPayedTravelTimes = new float[Config.GenStationCount];
                for (int i = 0; i < Config.GenStationCount; i++) {
                    float oneWayTravelTime = (float)rand.NextDouble() * Config.GenMaxDist;
                    float twoWayPayedTravelTime = Math.Max(0, 2 * oneWayTravelTime - 1); // First hour of travel is unpaid
                    twoWayPayedTravelTimes[i] = twoWayPayedTravelTime;
                }

                // Contract hours
                int contactHours = (int)(rand.NextDouble() * (Config.GenMaxContractHours - Config.GenMinContractHours) + Config.GenMinContractHours);
                float minWorkedHours = contactHours * Config.MinContractHoursFraction;
                float maxWorkedHours = contactHours * Config.MaxContractHoursFraction;

                drivers[driverIndex] = new Driver(-1, minWorkedHours, maxWorkedHours, trackProficiencies, twoWayPayedTravelTimes);
            }

            // Add driver indices
            for (int driverIndex = 0; driverIndex < drivers.Length; driverIndex++) {
                drivers[driverIndex].Index = driverIndex;
            }

            return drivers;
        }
    }

    class Trip {
        public int Index;
        public readonly List<int> Stations;
        public readonly int FirstStation, LastStation;
        public readonly int DayIndex;
        public readonly float StartTime, EndTime, Duration, DrivingCost;
        public readonly List<Trip> Successors;
        public readonly List<Trip> SameDaySuccessors;
        public readonly List<int> SameDaySuccessorsIndices;

        public Trip(int index, List<int> stations, int dayIndex, float startTime, float endTime, float duration, float drivingCost) {
            Index = index;
            Stations = stations;
            FirstStation = stations[0];
            LastStation = stations[stations.Count - 1];
            Successors = new List<Trip>();
            SameDaySuccessors = new List<Trip>();
            SameDaySuccessorsIndices = new List<int>();
            DayIndex = dayIndex;
            StartTime = startTime;
            EndTime = endTime;
            Duration = duration;
            DrivingCost = drivingCost;
        }

        public void AddSuccessor(Trip trip, int? dayIndex) {
            Successors.Add(trip);
            if (trip.DayIndex == DayIndex) {
                SameDaySuccessors.Add(trip);
                SameDaySuccessorsIndices.Add(dayIndex.Value);
            }
        }
    }

    class Driver {
        public int Index;
        public float MinWorkedHours, MaxWorkedHours;
        public readonly bool[,] TrackProficiencies;
        public readonly float[] TwoWayPayedTravelTimes;

        public Driver(int index, float minWorkedHours, float maxWorkedHours, bool[,] trackProficiencies, float[] twoWayPayedTravelTimes) {
            Index = index;
            MinWorkedHours = minWorkedHours;
            MaxWorkedHours = maxWorkedHours;
            TrackProficiencies = trackProficiencies;
            TwoWayPayedTravelTimes = twoWayPayedTravelTimes;
        }
    }

    class Instance {
        public readonly float[,] TrainTravelTimes, CarTravelTimes;
        public readonly Trip[] Trips;
        public readonly bool[,] TripSuccession;
        public readonly Trip[][] TripsPerDay;
        public readonly Driver[] Drivers;

        public Instance(float[,] trainTravelTimes, float[,] carTravelTimes, Trip[] trips, bool[,] tripSuccession, Trip[][] tripsPerDay, Driver[] drivers) {
            TrainTravelTimes = trainTravelTimes;
            CarTravelTimes = carTravelTimes;
            Trips = trips;
            TripSuccession = tripSuccession;
            TripsPerDay = tripsPerDay;
            Drivers = drivers;
        }
    }
}

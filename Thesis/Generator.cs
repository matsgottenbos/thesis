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
            float[,] stationTravelTimes = GenerateStationTravelTimes();
            (Trip[] trips, Trip[][] tripsPerDay) = GenerateAllTrips(stationTravelTimes);
            Driver[] drivers = GenerateDrivers();
            return new Instance(stationTravelTimes, trips, tripsPerDay, drivers);
        }

        float[,] GenerateStationTravelTimes() {
            float[,] stationTravelTimes = new float[Config.StationCount, Config.StationCount];
            for (int i = 0; i < Config.StationCount; i++) {
                for (int j = i; j < Config.StationCount; j++) {
                    if (i == j) continue;
                    float dist = (float)rand.NextDouble() * Config.MaxDist;
                    stationTravelTimes[i, j] = dist;
                    stationTravelTimes[j, i] = dist;
                }
            }
            return stationTravelTimes;
        }

        Trip[] GenerateTripsOneDay(float[,] stationTravelTimes, int dayIndex) {
            // Generate trips
            Trip[] dayTrips = new Trip[Config.TripCountPerDay];
            for (int dayTripIndex = 0; dayTripIndex < Config.TripCountPerDay; dayTripIndex++) {
                // Stations
                List<int> tripStations = new List<int>();
                int tripStationCount = rand.Next(2, Config.MaxStationCountPerTrip + 1);
                while (tripStations.Count < tripStationCount) {
                    int randomStation = rand.Next(Config.StationCount);
                    if (!tripStations.Contains(randomStation)) tripStations.Add(randomStation);
                }

                // Start and end time
                float tripDuration = 0;
                for (int j = 0; j < tripStations.Count - 1; j++) {
                    int station1Index = tripStations[j];
                    int station2Index = tripStations[j + 1];
                    tripDuration += stationTravelTimes[station1Index, station2Index];
                }
                float startTime = (float)rand.NextDouble() * (Config.DayLength - tripDuration);
                float endTime = startTime + tripDuration;

                Trip trip = new Trip(-1, tripStations, dayIndex, startTime, endTime, tripDuration);
                dayTrips[dayTripIndex] = trip;
            }

            // Sort trips by start time
            dayTrips = dayTrips.OrderBy(trip => trip.StartTime).ToArray();

            // Generate precedence constraints
            for (int dayTrip1Index = 0; dayTrip1Index < Config.TripCountPerDay; dayTrip1Index++) {
                for (int dayTrip2Index = dayTrip1Index; dayTrip2Index < Config.TripCountPerDay; dayTrip2Index++) {
                    Trip trip1 = dayTrips[dayTrip1Index];
                    Trip trip2 = dayTrips[dayTrip2Index];
                    float travelTimeBetween = stationTravelTimes[trip1.Stations[trip1.Stations.Count - 1], trip2.Stations[0]];
                    if (trip1.EndTime + travelTimeBetween <= trip2.StartTime) {
                        trip1.AddSuccessor(trip2, dayTrip2Index);
                    }
                }
            }

            return dayTrips;
        }

        (Trip[], Trip[][]) GenerateAllTrips(float[,] stationTravelTimes) {
            // Generate days
            Trip[][] tripsPerDay = new Trip[Config.DayCount][];
            List<Trip> tripsList = new List<Trip>();
            for (int dayIndex = 0; dayIndex < Config.DayCount; dayIndex++) {
                Trip[] dayTrips = GenerateTripsOneDay(stationTravelTimes, dayIndex);
                tripsPerDay[dayIndex] = dayTrips;
                tripsList.AddRange(dayTrips);
            }
            Trip[] trips = tripsList.ToArray();

            // Add trip indices
            for (int tripIndex = 0; tripIndex < trips.Length; tripIndex++) {
                trips[tripIndex].Index = tripIndex;
            }

            // Generate precedence constraints
            for (int dayIndex = 0; dayIndex < Config.DayCount - 1; dayIndex++) {
                Trip[] day1Trips = tripsPerDay[dayIndex];
                Trip[] day2Trips = tripsPerDay[dayIndex + 1];

                for (int day1TripIndex = 0; day1TripIndex < day1Trips.Length; day1TripIndex++) {
                    for (int day2TripIndex = 0; day2TripIndex < day2Trips.Length; day2TripIndex++) {
                        day1Trips[day1TripIndex].AddSuccessor(day2Trips[day2TripIndex], null);
                    }
                }
            }

            return (trips, tripsPerDay);
        }

        Driver[] GenerateDrivers() {
            Driver[] drivers = new Driver[Config.DriverCount];
            for (int driverIndex = 0; driverIndex < Config.DriverCount; driverIndex++) {
                // Salary
                float hourlyRate = (float)rand.NextDouble() * (Config.MaxHourlyRate - Config.MinHourlyRate) + Config.MinHourlyRate;

                // Track proficiencies
                bool[,] trackProficiencies = new bool[Config.StationCount, Config.StationCount];
                for (int i = 0; i < Config.StationCount; i++) {
                    for (int j = i; j < Config.StationCount; j++) {
                        bool isProficient;
                        if (i == j) {
                            isProficient = true;
                        } else {
                            isProficient = rand.NextDouble() < Config.TrackProficiencyProb;
                        }

                        trackProficiencies[i, j] = isProficient;
                        trackProficiencies[j, i] = isProficient;
                    }
                }

                // Travel times
                float[] payedTravelTimes = new float[Config.StationCount];
                for (int i = 0; i < Config.StationCount; i++) {
                    float travelTime = (float)rand.NextDouble() * Config.MaxDist;
                    float payedTravelTime = Math.Max(0, travelTime - 1); // First hour of travel is unpaid
                    payedTravelTimes[i] = payedTravelTime;
                }

                drivers[driverIndex] = new Driver(driverIndex, hourlyRate, trackProficiencies, payedTravelTimes);
            }
            return drivers;
        }
    }

    class Trip {
        public int Index;
        public readonly List<int> Stations;
        public readonly int FirstStation, LastStation;
        public readonly int DayIndex;
        public readonly float StartTime, EndTime, Duration;
        public readonly List<Trip> Successors;
        public readonly List<Trip> SameDaySuccessors;
        public readonly List<int> SameDaySuccessorsIndices;

        public Trip(int index, List<int> stations, int dayIndex, float startTime, float endTime, float duration) {
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
        public readonly int Index;
        public readonly float HourlyRate;
        public readonly bool[,] TrackProficiencies;
        public readonly float[] PayedTravelTimes;

        public Driver(int index, float hourlyRate, bool[,] trackProficiencies, float[] payedTravelTimes) {
            Index = index;
            HourlyRate = hourlyRate;
            TrackProficiencies = trackProficiencies;
            PayedTravelTimes = payedTravelTimes;
        }
    }

    class Instance {
        public readonly float[,] StationTravelTimes;
        public readonly Trip[] Trips;
        public readonly Trip[][] TripsPerDay;
        public readonly Driver[] Drivers;

        public Instance(float[,] stationTravelTimes, Trip[] trips, Trip[][] tripsPerDay, Driver[] drivers) {
            StationTravelTimes = stationTravelTimes;
            Trips = trips;
            TripsPerDay = tripsPerDay;
            Drivers = drivers;
        }
    }
}

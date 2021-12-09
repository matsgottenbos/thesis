using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class LowerBoundCalculator {
        readonly Instance instance;

        public LowerBoundCalculator(Instance instance) {
            this.instance = instance;
        }

        public float CalculateLowerBound1() {
            float cost = 0;
            for (int driverIndex = 0; driverIndex < instance.Drivers.Length; driverIndex++) {
                Driver driver = instance.Drivers[driverIndex];
                float driverBestTripCost = int.MaxValue;
                for (int tripIndex = 0; tripIndex < instance.Trips.Length; tripIndex++) {
                    Trip trip = instance.Trips[tripIndex];

                    // Driving cost
                    float drivingCosts = CostHelper.DrivingCost(trip, driver);

                    // Costs before
                    float travelFromHomeCost = CostHelper.TravelCostFromHome(trip, driver);
                    float minTravelFromTripBeforeCost = int.MaxValue;
                    for (int tripBeforeIndex = 0; tripBeforeIndex < instance.Trips.Length; tripBeforeIndex++) {
                        Trip tripBefore = instance.Trips[tripBeforeIndex];
                        if (tripBefore.DayIndex != trip.DayIndex || !tripBefore.Successors.Contains(trip)) continue;

                        float travelFromTripCost = CostHelper.WaitingCostBetween(tripBefore, trip, driver);
                        minTravelFromTripBeforeCost = Math.Min(minTravelFromTripBeforeCost, travelFromTripCost);
                    }
                    float minCostsBefore = Math.Min(travelFromHomeCost, 0.5f * minTravelFromTripBeforeCost);

                    // Costs after
                    float travelToHomeCost = CostHelper.TravelCostToHome(trip, driver);
                    float minTravelToTripAfterCost = int.MaxValue;
                    for (int tripAfterIndex = 0; tripAfterIndex < instance.Trips.Length; tripAfterIndex++) {
                        Trip tripAfter = instance.Trips[tripAfterIndex];
                        if (tripAfter.DayIndex != trip.DayIndex || !trip.Successors.Contains(tripAfter)) continue;

                        float travelToTripCost = CostHelper.WaitingCostBetween(trip, tripAfter, driver);
                        minTravelFromTripBeforeCost = Math.Min(minTravelFromTripBeforeCost, travelToTripCost);
                    }
                    float minCostsAfter = Math.Min(travelToHomeCost, 0.5f * minTravelToTripAfterCost);

                    float tripCosts = drivingCosts + minCostsBefore + minCostsAfter;

                    driverBestTripCost = Math.Min(driverBestTripCost, tripCosts);
                }

                cost += driverBestTripCost;
            }

            return cost;
        }

        public float CalculateLowerBound2() {
            float cost = 0;
            for (int dayIndex = 0; dayIndex < Config.DayCount; dayIndex++) {
                Trip[] dayTrips = instance.TripsPerDay[dayIndex];
                List<(int, float)> pathOptions = new List<(int, float)>(); // For each driver, for each starting trip, for each trip length, the best cost

                for (int driverIndex = 0; driverIndex < Config.DriverCount; driverIndex++) {
                    Driver driver = instance.Drivers[driverIndex];
                    List<float>[] minCostsPerLengthFromTrips = new List<float>[dayTrips.Length]; // For each trip, for each successor path length, the minimum cost; includes travel after, but not travel before
                    List<float>[] minCostsPerLengthFromTripsWithBefore = new List<float>[dayTrips.Length]; // For each trip, for each successor path length, the minimum cost; includes travel before and after

                    for (int dayTripIndex = dayTrips.Length - 1; dayTripIndex >= 0; dayTripIndex--) {
                        minCostsPerLengthFromTrips[dayTripIndex] = new List<float>();
                        minCostsPerLengthFromTripsWithBefore[dayTripIndex] = new List<float>();

                        Trip trip = dayTrips[dayTripIndex];
                        float travelFromHomeCosts = CostHelper.TravelCostFromHome(trip, driver);
                        float drivingCosts = CostHelper.DrivingCost(trip, driver);
                        float travelToHomeCosts = CostHelper.TravelCostToHome(trip, driver);

                        // Add option where this is the only trip
                        float singleTripCost = drivingCosts + travelToHomeCosts;
                        float singleTripCostWithBefore = drivingCosts + travelToHomeCosts;
                        minCostsPerLengthFromTrips[dayTripIndex].Add(singleTripCost);
                        minCostsPerLengthFromTripsWithBefore[dayTripIndex].Add(travelFromHomeCosts + singleTripCostWithBefore);
                        pathOptions.Add((1, singleTripCostWithBefore));

                        for (int tripSuccessorIndex = 0; tripSuccessorIndex < trip.SameDaySuccessors.Count; tripSuccessorIndex++) {
                            Trip successorTrip = trip.SameDaySuccessors[tripSuccessorIndex];
                            int successorTripDayIndex = trip.SameDaySuccessorsIndices[tripSuccessorIndex];
                            float successorTravelCost = CostHelper.WaitingCostBetween(trip, successorTrip, driver);
                            List<float> minCostsPerLengthFromSuccessor = minCostsPerLengthFromTrips[successorTripDayIndex];

                            for (int pathLength = 0; pathLength < minCostsPerLengthFromSuccessor.Count; pathLength++) {
                                float successorPathCosts = minCostsPerLengthFromSuccessor[pathLength];
                                float fromSuccessorCost = successorTravelCost + successorPathCosts;
                                float fromSuccessorCostWithBefore = travelFromHomeCosts + fromSuccessorCost;
                                minCostsPerLengthFromTrips[dayTripIndex].Add(fromSuccessorCost);
                                minCostsPerLengthFromTripsWithBefore[dayTripIndex].Add(fromSuccessorCostWithBefore);
                                pathOptions.Add((pathLength + 2, fromSuccessorCostWithBefore));
                            }
                        }
                    }
                }

                pathOptions = pathOptions.OrderBy(option => option.Item2 / option.Item1).ToList();
                int optionIndex = 0;
                int assignedDayTrips = 0;
                while (assignedDayTrips < dayTrips.Length) {
                    (int optionTripCount, float optionCosts) = pathOptions[optionIndex];
                    assignedDayTrips += optionTripCount;
                    cost += optionCosts;
                    optionIndex++;
                }
            }

            return cost;
        }
    }
}

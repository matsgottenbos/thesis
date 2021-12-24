using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class CostHelper {
        /* Assignment cost without penalties */

        public static double AssignmentCostWithoutPenalties(List<Trip>[] driverPaths, Instance instance) {
            // Determine cost
            double cost = 0;
            for (int driverIndex = 0; driverIndex < Config.DriverCount; driverIndex++) {
                Driver driver = instance.Drivers[driverIndex];
                List<Trip> driverPath = driverPaths[driverIndex];

                if (driverPath.Count < 1) continue;

                // Count driving cost
                cost += DrivingCost(driverPath, driver);

                // Count first and last trip travel
                Trip firstTrip = driverPath[0];
                Trip lastTrip = driverPath[driverPath.Count - 1];
                cost += TravelCostFromHome(firstTrip, driver) + TravelCostToHome(lastTrip, driver);

                // TODO: option for hotel

                if (driverPath.Count < 2) continue;

                // Count travel cost
                for (int tripIndex = 0; tripIndex < driverPath.Count - 1; tripIndex++) {
                    Trip trip1 = driverPath[tripIndex];
                    Trip trip2 = driverPath[tripIndex + 1];
                    cost += BestCostBetween(trip1, trip2, driver);
                }
            }

            return cost;
        }
        public static double AssignmentCostWithoutPenalties(int[] assignmentIndices, Instance instance) {
            List<Trip>[] driverPaths = GetPathPerDriver(assignmentIndices, instance);
            return AssignmentCostWithoutPenalties(driverPaths, instance);
        }
        public static double AssignmentCostWithoutPenalties(Driver[] assignment, Instance instance) {
            List<Trip>[] driverPaths = GetPathPerDriver(assignment, instance);
            return AssignmentCostWithoutPenalties(driverPaths, instance);
        }


        /* Assignment penalties */

        public static double GetAssignmentBasePenalties(List<Trip>[] driverPaths, Instance instance) {
            int workDayLengthExceedanceCount = 0;
            double workDayLengthExceedance = 0;
            int precedenceViolationCount = 0;
            for (int driverIndex = 0; driverIndex < Config.DriverCount; driverIndex++) {
                Driver driver = instance.Drivers[driverIndex];
                List<Trip> driverPath = driverPaths[driverIndex];
                if (driverPath.Count > 0) {
                    Trip prevTrip = driverPath[0];
                    int currentDayIndex = prevTrip.DayIndex;
                    double workDayStartTime = prevTrip.StartTime - TravelTimeFromHome(prevTrip, driver);
                    for (int driverTripIndex = 1; driverTripIndex < driverPath.Count; driverTripIndex++) {
                        Trip trip = driverPath[driverTripIndex];

                        // Working day length
                        if (trip.DayIndex != currentDayIndex) {
                            // End previous day
                            double workDayLength = prevTrip.EndTime + TravelTimeToHome(prevTrip, driver) - workDayStartTime;
                            double currentWorkDayLengthExceedance = Math.Max(0, workDayLength - Config.MaxWorkDayLength);
                            if (currentWorkDayLengthExceedance > 0) {
                                workDayLengthExceedanceCount++;
                                workDayLengthExceedance += currentWorkDayLengthExceedance;
                            }

                            // Start new day
                            currentDayIndex = trip.DayIndex;
                            workDayStartTime = trip.StartTime - TravelTimeFromHome(trip, driver);
                        }

                        // Precedence
                        if (!instance.TripSuccession[prevTrip.Index, trip.Index]) {
                            precedenceViolationCount++;
                        }

                        prevTrip = trip;
                    }

                    // End last day
                    double lastWorkDayLength = prevTrip.EndTime + TravelTimeToHome(prevTrip, driver) - workDayStartTime;
                    double lastWorkDayLengthExceedance = Math.Max(0, lastWorkDayLength - Config.MaxWorkDayLength);
                    if (lastWorkDayLengthExceedance > 0) {
                        workDayLengthExceedanceCount++;
                        workDayLengthExceedance += lastWorkDayLengthExceedance;
                    }
                }
            }

            double workDayLengthPenaltyBase = workDayLengthExceedanceCount * Config.WorkDayLengthExceedancePenalty + workDayLengthExceedance * Config.WorkDayLengthExceedancePenaltyPerHour;
            double precendencePenaltyBase = precedenceViolationCount * Config.PrecendenceViolationPenalty;
            double penaltyBase = workDayLengthPenaltyBase + precendencePenaltyBase;
            return penaltyBase;
        }


        /* Assignment cost with penalties */

        public static (double, double, double) AssignmentCostWithPenalties(List<Trip>[] driverPaths, Instance instance, float penaltyFactor) {
            double costWithoutPenalty = AssignmentCostWithoutPenalties(driverPaths, instance);
            double penaltyBase = GetAssignmentBasePenalties(driverPaths, instance);
            double penalty = penaltyBase * penaltyFactor;
            double cost = costWithoutPenalty + penalty;
            return (cost, costWithoutPenalty, penaltyBase);
        }
        public static (double, double, double) AssignmentCostWithPenalties(int[] assignmentIndices, Instance instance, float penaltyFactor) {
            List<Trip>[] driverPaths = GetPathPerDriver(assignmentIndices, instance);
            return AssignmentCostWithPenalties(driverPaths, instance, penaltyFactor);
        }
        public static (double, double, double) AssignmentCostWithPenalties(Driver[] assignment, Instance instance, float penaltyFactor) {
            List<Trip>[] driverPaths = GetPathPerDriver(assignment, instance);
            return AssignmentCostWithPenalties(driverPaths, instance, penaltyFactor);
        }


        /* Assignment helpers */

        static List<Trip>[] GetPathPerDriver(int[] assignmentIndices, Instance instance) {
            List<Trip>[] driverPaths = new List<Trip>[Config.DriverCount];
            for (int driverIndex = 0; driverIndex < driverPaths.Length; driverIndex++) {
                driverPaths[driverIndex] = new List<Trip>();
            }

            for (int tripIndex = 0; tripIndex < assignmentIndices.Length; tripIndex++) {
                int driverIndex = assignmentIndices[tripIndex];
                Trip trip = instance.Trips[tripIndex];
                driverPaths[driverIndex].Add(trip);
            }
            return driverPaths;
        }
        static List<Trip>[] GetPathPerDriver(Driver[] assignment, Instance instance) {
            List<Trip>[] driverPaths = new List<Trip>[Config.DriverCount];
            for (int driverIndex = 0; driverIndex < driverPaths.Length; driverIndex++) {
                driverPaths[driverIndex] = new List<Trip>();
            }

            for (int tripIndex = 0; tripIndex < assignment.Length; tripIndex++) {
                Driver driver = assignment[tripIndex];
                Trip trip = instance.Trips[tripIndex];
                driverPaths[driver.Index].Add(trip);
            }
            return driverPaths;
        }


        /* Driving cost */

        public static float DrivingCost(Trip trip, Driver driver) {
            return trip.Duration * driver.HourlyRate;
        }
        public static float DrivingCost(List<Trip> trips, Driver driver) {
            float cost = 0;
            for (int tripIndex = 0; tripIndex < trips.Count; tripIndex++) {
                cost += DrivingCost(trips[tripIndex], driver);
            }
            return cost;
        }


        /* Travel cost */

        public static float TravelTimeFromHome(Trip trip, Driver driver) {
            return driver.PayedTravelTimes[trip.FirstStation];
        }
        public static float TravelCostFromHome(Trip trip, Driver driver) {
            return TravelTimeFromHome(trip, driver) * driver.HourlyRate;
        }

        public static float TravelTimeToHome(Trip trip, Driver driver) {
            return driver.PayedTravelTimes[trip.LastStation];
        }
        public static float TravelCostToHome(Trip trip, Driver driver) {
            return TravelTimeToHome(trip, driver) * driver.HourlyRate;
        }

        public static float WaitingTimeBetween(Trip trip1, Trip trip2) {
            if (trip1.DayIndex != trip2.DayIndex) return float.MaxValue; // Waiting is not possible between days
            return trip2.StartTime - trip1.EndTime;
        }
        public static float WaitingCostBetween(Trip trip1, Trip trip2, Driver driver) {
            if (trip1.DayIndex != trip2.DayIndex) return float.MaxValue; // Waiting is not possible between days
            return (trip2.StartTime - trip1.EndTime) * driver.HourlyRate; // Assumption: waiting time is paid
        }

        public static float BestWorkingTimeBetween(Trip trip1, Trip trip2, Driver driver) {
            // If same day, waiting time
            if (trip1.DayIndex == trip2.DayIndex) return WaitingTimeBetween(trip1, trip2);

            // If different days: travel time via home
            return TravelTimeToHome(trip1, driver) + TravelTimeFromHome(trip2, driver);
        }
        public static float BestCostBetween(Trip trip1, Trip trip2, Driver driver) {
            return BestWorkingTimeBetween(trip1, trip2, driver) * driver.HourlyRate;
        }


        /* Work day start and end times */

        public static float WorkDayStartTime(Trip firstDayTrip, Driver driver) {
            return firstDayTrip.StartTime - TravelTimeFromHome(firstDayTrip, driver);
        }

        public static float WorkDayEndTime(Trip lastDayTrip, Driver driver) {
            return lastDayTrip.EndTime + TravelTimeToHome(lastDayTrip, driver);
        }

        public static float WorkDayLength(Trip firstDayTrip, Trip lastDayTrip, Driver driver) {
            return WorkDayEndTime(lastDayTrip, driver) - WorkDayStartTime(firstDayTrip, driver);
        }


        /* Operation cost */

        public static (double, double, double) UnassignTripCostDiff(Trip oldTrip, Driver driver, Driver[] assignment, Instance instance, float penaltyFactor) {
            Trip tripBefore = GetDriverSameDayTripBefore(oldTrip.Index, driver, oldTrip.DayIndex, assignment, instance);
            Trip tripAfter = GetDriverSameDayTripAfter(oldTrip.Index, driver, oldTrip.DayIndex, assignment, instance);

            float workDayLengthDiff, workDayLengthPenaltyDiff;
            int precedenceViolationCountDiff = 0;
            if (tripBefore == null) {
                if (tripAfter == null) {
                    // No trips before or after
                    float oldWorkDayLength = WorkDayLength(oldTrip, oldTrip, driver);
                    float newWorkDayLength = 0;
                    workDayLengthDiff = -oldWorkDayLength;
                    workDayLengthPenaltyDiff = GetWorkDayPenaltyBaseDiff(oldWorkDayLength, newWorkDayLength);
                } else {
                    // Trip after, but not before
                    Trip lastDayTrip = GetLastDayTrip(tripAfter, driver, assignment, instance);
                    float workDayEndTime = WorkDayEndTime(lastDayTrip, driver);
                    float oldWorkDayLength = workDayEndTime - WorkDayStartTime(oldTrip, driver);
                    float newWorkDayLength = workDayEndTime - WorkDayStartTime(tripAfter, driver);
                    workDayLengthDiff = newWorkDayLength - oldWorkDayLength;
                    workDayLengthPenaltyDiff = GetWorkDayPenaltyBaseDiff(oldWorkDayLength, newWorkDayLength);

                    // Check if a precedence violation was removed after
                    if (!instance.TripSuccession[oldTrip.Index, tripAfter.Index]) precedenceViolationCountDiff--;
                }
            } else {
                if (tripAfter == null) {
                    // Trip before, but not after
                    Trip firstDayTrip = GetFirstDayTrip(tripBefore, driver, assignment, instance);
                    float workDayStartTime = WorkDayStartTime(firstDayTrip, driver);
                    float oldWorkDayLength = WorkDayEndTime(oldTrip, driver) - workDayStartTime;
                    float newWorkDayLength = WorkDayEndTime(tripBefore, driver) - workDayStartTime;
                    workDayLengthDiff = newWorkDayLength - oldWorkDayLength;
                    workDayLengthPenaltyDiff = GetWorkDayPenaltyBaseDiff(oldWorkDayLength, newWorkDayLength);
                } else {
                    // Trips before and after
                    workDayLengthDiff = 0;
                    workDayLengthPenaltyDiff = 0;

                    // Check if a precedence violation was removed after
                    if (!instance.TripSuccession[oldTrip.Index, tripAfter.Index]) precedenceViolationCountDiff--;

                    // Check if a precedence violation was added
                    if (!instance.TripSuccession[tripBefore.Index, tripAfter.Index]) precedenceViolationCountDiff++;
                }

                // Check if a precedence violation was removed before
                if (!instance.TripSuccession[tripBefore.Index, oldTrip.Index]) precedenceViolationCountDiff--;
            }

            double costWithoutPenaltyDiff = workDayLengthDiff * driver.HourlyRate;
            double precedencePenaltyBaseDiff = precedenceViolationCountDiff * Config.PrecendenceViolationPenalty; // TODO: include next-day successors
            double penaltyBaseDiff = workDayLengthPenaltyDiff + precedencePenaltyBaseDiff;
            double costDiff = costWithoutPenaltyDiff + penaltyBaseDiff * penaltyFactor;
            return (costDiff, costWithoutPenaltyDiff, penaltyBaseDiff);
        }

        public static (double, double, double) AssignTripCostDiff(Trip newTrip, Driver driver, Driver[] assignment, Instance instance, float penaltyFactor) {
            (double costDiff, double costWithoutPenaltyDiff, double penaltyBaseDiff) = UnassignTripCostDiff(newTrip, driver, assignment, instance, penaltyFactor);
            return (-costDiff, -costWithoutPenaltyDiff, -penaltyBaseDiff);
        }

        static float GetWorkDayPenaltyBaseDiff(float oldWorkDayLength, float newWorkDayLength) {
            float oldWorkDayLengthExceedance = Math.Max(0, oldWorkDayLength - Config.MaxWorkDayLength);
            float newWorkDayLengthExceedance = Math.Max(0, newWorkDayLength - Config.MaxWorkDayLength);
            float amountPenaltyBaseDiff = (newWorkDayLengthExceedance - oldWorkDayLengthExceedance) * Config.WorkDayLengthExceedancePenaltyPerHour;

            float countPenaltyBaseDiff = 0;
            if (oldWorkDayLengthExceedance > 0) {
                if (newWorkDayLengthExceedance == 0) countPenaltyBaseDiff = -Config.WorkDayLengthExceedancePenalty;
            } else {
                if (newWorkDayLengthExceedance > 0) countPenaltyBaseDiff = Config.WorkDayLengthExceedancePenalty;
            }

            return amountPenaltyBaseDiff + countPenaltyBaseDiff;
        }


        /* Getting trips */

        public static Trip GetDriverSameDayTripBefore(int tripIndex, Driver driver, int dayIndex, Driver[] assignment, Instance instance) {
            for (int searchTripIndex = tripIndex - 1; searchTripIndex >= 0; searchTripIndex--) {
                Trip trip = instance.Trips[searchTripIndex];
                if (trip.DayIndex != dayIndex) return null;

                if (assignment[searchTripIndex] == driver) {
                    return trip;
                }
            }
            return null;
        }
        public static Trip GetDriverSameDayTripAfter(int tripIndex, Driver driver, int dayIndex, Driver[] assignment, Instance instance) {
            for (int searchTripIndex = tripIndex + 1; searchTripIndex < instance.Trips.Length; searchTripIndex++) {
                Trip trip = instance.Trips[searchTripIndex];
                if (trip.DayIndex != dayIndex) return null;

                if (assignment[searchTripIndex] == driver) {
                    return trip;
                }
            }
            return null;
        }

        static Trip GetFirstDayTrip(Trip someTripOnDay, Driver driver, Driver[] assignment, Instance instance) {
            Trip firstDayTrip = someTripOnDay;
            for (int searchTripIndex = someTripOnDay.Index - 1; searchTripIndex >= 0; searchTripIndex--) {
                Trip trip = instance.Trips[searchTripIndex];
                if (trip.DayIndex != someTripOnDay.DayIndex) break;
                if (assignment[searchTripIndex] == driver) firstDayTrip = trip;
            }
            return firstDayTrip;
        }
        static Trip GetLastDayTrip(Trip someTripOnDay, Driver driver, Driver[] assignment, Instance instance) {
            Trip lastDayTrip = someTripOnDay;
            for (int searchTripIndex = someTripOnDay.Index + 1; searchTripIndex < instance.Trips.Length; searchTripIndex++) {
                Trip trip = instance.Trips[searchTripIndex];
                if (trip.DayIndex != someTripOnDay.DayIndex) break;
                if (assignment[searchTripIndex] == driver) lastDayTrip = trip;
            }
            return lastDayTrip;
        }

        static float GetDriverWorkDayLength(Trip someTripOnDay, Driver driver, Driver[] assignment, Instance instance) {
            Trip firstDayTrip = GetFirstDayTrip(someTripOnDay, driver, assignment, instance);
            Trip lastDayTrip = GetLastDayTrip(someTripOnDay, driver, assignment, instance);
            float workDayLength = WorkDayLength(firstDayTrip, lastDayTrip, driver);
            return workDayLength;
        }


        /* Objects for gettings trips */

        public static void AssignDriverDayTrip(Trip newTrip, Driver newDriver, int[,] driverDayTripCount, Trip[,] driverDayTrips, Driver[] assignment, Instance instance) {
            int count = ++driverDayTripCount[newDriver.Index, newTrip.DayIndex];
            if (count == 1) {
                driverDayTrips[newDriver.Index, newTrip.DayIndex] = newTrip;
            }
        }

        public static void UnassignDriverDayTrip(Trip oldTrip, Driver oldDriver, int[,] driverDayTripCount, Trip[,] driverDayTrips, Driver[] assignment, Instance instance) {
            int count = --driverDayTripCount[oldDriver.Index, oldTrip.DayIndex];
            if (count == 0) {
                driverDayTrips[oldDriver.Index, oldTrip.DayIndex] = null;
            } else if (count == 1) {
                driverDayTrips[oldDriver.Index, oldTrip.DayIndex] = GetDriverSameDayTripBefore(oldTrip.Index, oldDriver, oldTrip.DayIndex, assignment, instance) ?? GetDriverSameDayTripAfter(oldTrip.Index, oldDriver, oldTrip.DayIndex, assignment, instance);
            }
        }
    }
}

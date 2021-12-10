using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class CostHelper {
        /* Assignment cost without penalties*/

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
                        if (!prevTrip.Successors.Contains(trip)) {
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






        /* Debug */

        public static DebugCost[] DebugCost(List<Trip>[] driverPaths, Instance instance, float penaltyFactor) {
            DebugCost[] driverCosts = new DebugCost[Config.DriverCount];
            for (int driverIndex = 0; driverIndex < Config.DriverCount; driverIndex++) {
                double drivingTime = 0;
                double travelTime = 0;

                /* Determine cost */
                Driver driver = instance.Drivers[driverIndex];
                List<Trip> driverPath = driverPaths[driverIndex];

                if (driverPath.Count > 0) {
                    // Count driving cost
                    for (int tripIndex = 0; tripIndex < driverPath.Count; tripIndex++) {
                        Trip trip = driverPath[tripIndex];
                        drivingTime += trip.Duration;
                    }

                    // Count first and last trip travel
                    Trip firstTrip = driverPath[0];
                    Trip lastTrip = driverPath[driverPath.Count - 1];
                    travelTime += TravelTimeFromHome(firstTrip, driver) + TravelTimeToHome(lastTrip, driver);

                    // TODO: option for hotel

                    // Count travel cost
                    for (int tripIndex = 0; tripIndex < driverPath.Count - 1; tripIndex++) {
                        Trip trip1 = driverPath[tripIndex];
                        Trip trip2 = driverPath[tripIndex + 1];
                        travelTime += BestWorkingTimeBetween(trip1, trip2, driver);
                    }
                }

                double drivingCost = drivingTime * driver.HourlyRate;
                double travelCost = travelTime * driver.HourlyRate;
                double costWithoutPenalty = drivingCost + travelCost;

                /* Determine penalties */
                int workDayLengthExceedanceCount = 0;
                double workDayLengthExceedance = 0;
                int precedenceViolationCount = 0;
                double[] workDayLengths = new double[Config.DayCount];
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
                            workDayLengths[currentDayIndex] = workDayLength;
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
                        if (!prevTrip.Successors.Contains(trip)) precedenceViolationCount++;

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

                double workDayLengthPenaltyBase = workDayLengthExceedanceCount * Config.WorkDayLengthExceedancePenalty + workDayLengthExceedance * Config.WorkDayLengthExceedancePenaltyPerHour;
                double precendencePenaltyBase = precedenceViolationCount * Config.PrecendenceViolationPenalty;
                double penalties = (workDayLengthPenaltyBase + precendencePenaltyBase) * penaltyFactor;

                double totalCost = costWithoutPenalty + penalties;

                driverCosts[driverIndex] = new DebugCost(totalCost, drivingTime, drivingCost, travelTime, travelCost, workDayLengthExceedanceCount, workDayLengthExceedance, workDayLengthPenaltyBase, precedenceViolationCount, precendencePenaltyBase, workDayLengths);
            }

            return driverCosts;
        }
        public static DebugCost[] DebugCost(Driver[] assignment, Instance instance, float penaltyFactor) {
            List<Trip>[] driverPaths = GetPathPerDriver(assignment, instance);
            return DebugCost(driverPaths, instance, penaltyFactor);
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


        /* Operation cost */

        public static (double, double, double) UnassignTripCostDiff(int oldTripIndex, Trip oldTrip, Driver driver, Driver[] assignment, Instance instance, float penaltyFactor) {
            Trip tripBefore = GetDriverSameDayTripBefore(oldTripIndex, driver, oldTrip.DayIndex, assignment, instance);
            Trip tripAfter = GetDriverSameDayTripAfter(oldTripIndex, driver, oldTrip.DayIndex, assignment, instance);

            float drivingTimeDiff = -oldTrip.Duration;
            float drivingCostDiff = drivingTimeDiff * driver.HourlyRate;

            float oldTravelTimeBefore = tripBefore == null ? TravelTimeFromHome(oldTrip, driver) : WaitingTimeBetween(tripBefore, oldTrip);
            float oldTravelTimeAfter = tripAfter == null ? TravelTimeToHome(oldTrip, driver) : WaitingTimeBetween(oldTrip, tripAfter);

            float newTravelTime;
            int precedenceViolationCountDiff = 0;
            if (tripBefore == null) {
                if (tripAfter == null) {
                    // No trips before or after
                    newTravelTime = 0;
                } else {
                    // Trip after, but not before
                    newTravelTime = TravelTimeFromHome(tripAfter, driver);

                    // Check if a precedence violation was removed after
                    if (!oldTrip.SameDaySuccessors.Contains(tripAfter)) precedenceViolationCountDiff--;
                }
            } else {
                if (tripAfter == null) {
                    // Trip before, but not after
                    newTravelTime = TravelTimeToHome(tripBefore, driver);
                } else {
                    // Trips before and after
                    newTravelTime = WaitingTimeBetween(tripBefore, tripAfter);

                    // Check if a precedence violation was removed after
                    if (!oldTrip.SameDaySuccessors.Contains(tripAfter)) precedenceViolationCountDiff--;

                    // Check if a precedence violation was added
                    if (!tripBefore.SameDaySuccessors.Contains(tripAfter)) precedenceViolationCountDiff++;
                }

                // Check if a precedence violation was removed before
                if (!tripBefore.SameDaySuccessors.Contains(oldTrip)) precedenceViolationCountDiff--;
            }

            double travelTimeDiff = newTravelTime - oldTravelTimeBefore - oldTravelTimeAfter;
            double travelCostDiff = travelTimeDiff * driver.HourlyRate;
            double costWithoutPenaltyDiff = drivingCostDiff + travelCostDiff;

            // Work day penalty
            double oldWorkDayLength = GetDriverWorkDayLength(oldTripIndex, oldTrip, driver, assignment, instance);
            double newWorkDayLength = oldWorkDayLength + drivingTimeDiff + travelTimeDiff;
            double workDayLengthDiff = newWorkDayLength - oldWorkDayLength;

            double oldWorkDayLengthExceedance = Math.Max(0, oldWorkDayLength - Config.MaxWorkDayLength);
            double newWorkDayLengthExceedance = Math.Max(0, newWorkDayLength - Config.MaxWorkDayLength);
            double workDayLengthExceedanceDiff = newWorkDayLengthExceedance - oldWorkDayLengthExceedance;
            int workDayLengthExceedanceCountDiff = 0;
            if (oldWorkDayLengthExceedance > 0) workDayLengthExceedanceCountDiff--;
            if (newWorkDayLengthExceedance > 0) workDayLengthExceedanceCountDiff++;
            double workDayLengthPenaltyBaseDiff = workDayLengthExceedanceCountDiff * Config.WorkDayLengthExceedancePenalty + workDayLengthExceedanceDiff * Config.WorkDayLengthExceedancePenaltyPerHour;
            double workDayLengthPenaltyDiff = workDayLengthPenaltyBaseDiff * penaltyFactor;

            // Precedence penalty
            // TODO: include next-day successors
            double precedencePenaltyBaseDiff = precedenceViolationCountDiff * Config.PrecendenceViolationPenalty;
            double precedencePenaltyDiff = precedencePenaltyBaseDiff * penaltyFactor;

            double penaltyBaseDiff = workDayLengthPenaltyBaseDiff + precedencePenaltyBaseDiff;
            double penaltyDiff = workDayLengthPenaltyDiff + precedencePenaltyDiff;
            double costDiff = costWithoutPenaltyDiff + penaltyDiff;

            double[] workDayLengthDiffs = new double[Config.DayCount];
            workDayLengthDiffs[oldTrip.DayIndex] = workDayLengthDiff;

            return (costDiff, costWithoutPenaltyDiff, penaltyBaseDiff);
        }

        public static (double, double, double) AssignTripCostDiff(int newTripIndex, Trip newTrip, Driver driver, Driver[] assignment, Instance instance, float penaltyFactor) {
            (double costDiff, double costWithoutPenaltyDiff, double penaltyBaseDiff) = UnassignTripCostDiff(newTripIndex, newTrip, driver, assignment, instance, penaltyFactor);
            return (-costDiff, -costWithoutPenaltyDiff, -penaltyBaseDiff);
        }

        static Trip GetDriverSameDayTripBefore(int tripIndex, Driver driver, int dayIndex, Driver[] assignment, Instance instance) {
            for (int searchTripIndex = tripIndex - 1; searchTripIndex >= 0; searchTripIndex--) {
                Trip trip = instance.Trips[searchTripIndex];
                if (trip.DayIndex != dayIndex) return null;

                if (assignment[searchTripIndex] == driver) {
                    return trip;
                }
            }
            return null;
        }
        static Trip GetDriverSameDayTripAfter(int tripIndex, Driver driver, int dayIndex, Driver[] assignment, Instance instance) {
            for (int searchTripIndex = tripIndex + 1; searchTripIndex < instance.Trips.Length; searchTripIndex++) {
                Trip trip = instance.Trips[searchTripIndex];
                if (trip.DayIndex != dayIndex) return null;

                if (assignment[searchTripIndex] == driver) {
                    return trip;
                }
            }
            return null;
        }

        static (Trip, Trip) GetFirstLastDayTrip(int tripIndex, Trip someTripOnDay, Driver driver, Driver[] assignment, Instance instance) {
            // Find first trip
            Trip firstDayTrip = someTripOnDay;
            for (int searchTripIndex = tripIndex - 1; searchTripIndex >= 0; searchTripIndex--) {
                Trip trip = instance.Trips[searchTripIndex];
                if (trip.DayIndex != someTripOnDay.DayIndex) break;
                if (assignment[searchTripIndex] == driver) firstDayTrip = trip;
            }

            // Find last trip
            Trip lastDayTrip = someTripOnDay;
            for (int searchTripIndex = tripIndex + 1; searchTripIndex < instance.Trips.Length; searchTripIndex++) {
                Trip trip = instance.Trips[searchTripIndex];
                if (trip.DayIndex != someTripOnDay.DayIndex) break;
                if (assignment[searchTripIndex] == driver) lastDayTrip = trip;
            }

            return (firstDayTrip, lastDayTrip);
        }
        static float GetDriverWorkDayLength(int tripIndex, Trip someTripOnDay, Driver driver, Driver[] assignment, Instance instance) {
            (Trip firstDayTrip, Trip lastDayTrip) = GetFirstLastDayTrip(tripIndex, someTripOnDay, driver, assignment, instance);
            float workDayStartTime = firstDayTrip.StartTime - TravelTimeFromHome(firstDayTrip, driver);
            float workDayEndTime = lastDayTrip.EndTime + TravelTimeToHome(lastDayTrip, driver);
            float workDayLength = workDayEndTime - workDayStartTime;

            return workDayLength;
        }
    }

    class DebugCost {
        public readonly double TotalCost, DrivingTime, DrivingCost, TravelTime, TravelCost, WorkDayLengthExceedance, WorkDayLengthPenaltyBase, PrecedencePenaltyBase;
        public readonly int WorkDayLengthExceedanceCount, PrecedenceViolationCount;
        public readonly double[] WorkDayLengths;

        public DebugCost(double totalCost, double drivingTime, double drivingCost, double travelTime, double travelCost, int workDayLengthExceedanceCount, double workDayLengthExceedance, double workDayLengthPenaltyBase, int precedenceViolationCount, double precedencePenaltyBase, double[] workDayLengths) {
            TotalCost = totalCost;
            DrivingTime = drivingTime;
            DrivingCost = drivingCost;
            TravelTime = travelTime;
            TravelCost = travelCost;
            WorkDayLengthExceedanceCount = workDayLengthExceedanceCount;
            WorkDayLengthExceedance = workDayLengthExceedance;
            WorkDayLengthPenaltyBase = workDayLengthPenaltyBase;
            PrecedenceViolationCount = precedenceViolationCount;
            PrecedencePenaltyBase = precedencePenaltyBase;
            WorkDayLengths = workDayLengths;
        }

        public static DebugCost CreateBySum(DebugCost[] debugCosts) {
            DebugCost sum = new DebugCost(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, new double[Config.DayCount]);
            for (int i = 0; i < debugCosts.Length; i++) {
                sum += debugCosts[i];
            }
            return sum;
        }

        public static DebugCost operator +(DebugCost a, DebugCost b) {
            double[] workDayLengths = new double[Config.DayCount];
            for (int dayIndex = 0; dayIndex < Config.DayCount; dayIndex++) workDayLengths[dayIndex] = a.WorkDayLengths[dayIndex] + b.WorkDayLengths[dayIndex];

            return new DebugCost(
                a.TotalCost + b.TotalCost,
                a.DrivingTime + b.DrivingTime,
                a.DrivingCost + b.DrivingCost,
                a.TravelTime + b.TravelTime,
                a.TravelCost + b.TravelCost,
                a.WorkDayLengthExceedanceCount + b.WorkDayLengthExceedanceCount,
                a.WorkDayLengthExceedance + b.WorkDayLengthExceedance,
                a.WorkDayLengthPenaltyBase + b.WorkDayLengthPenaltyBase,
                a.PrecedenceViolationCount + b.PrecedenceViolationCount,
                a.PrecedencePenaltyBase + b.PrecedencePenaltyBase,
                workDayLengths
            );
        }

        public static DebugCost operator -(DebugCost a) {
            double[] workDayLengths = new double[Config.DayCount];
            for (int dayIndex = 0; dayIndex < Config.DayCount; dayIndex++) workDayLengths[dayIndex] = -a.WorkDayLengths[dayIndex];

            return new DebugCost(
                -a.TotalCost,
                -a.DrivingTime,
                -a.DrivingCost,
                -a.TravelTime,
                -a.TravelCost,
                -a.WorkDayLengthExceedanceCount,
                -a.WorkDayLengthExceedance,
                -a.WorkDayLengthPenaltyBase,
                -a.PrecedenceViolationCount,
                -a.PrecedencePenaltyBase,
                workDayLengths
            );
        }

        public static DebugCost operator -(DebugCost a, DebugCost b) => a + -b;
    }
}

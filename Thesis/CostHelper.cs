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
            double totalWorkTime = 0;
            for (int driverIndex = 0; driverIndex < Config.DriverCount; driverIndex++) {
                List<Trip> driverPath = driverPaths[driverIndex];
                if (driverPath.Count == 0) continue;
                Driver driver = instance.Drivers[driverIndex];

                Trip dayFirstTrip = driverPath[0];
                Trip prevTrip = driverPath[0];
                for (int driverTripIndex = 1; driverTripIndex < driverPath.Count; driverTripIndex++) {
                    Trip trip = driverPath[driverTripIndex];

                    // Working day length
                    if (trip.DayIndex != dayFirstTrip.DayIndex) {
                        // End previous day
                        totalWorkTime += WorkDayLength(dayFirstTrip, prevTrip, driver, instance);

                        // Start new day
                        dayFirstTrip = trip;
                    }

                    prevTrip = trip;
                }

                // End last day
                totalWorkTime += WorkDayLength(dayFirstTrip, prevTrip, driver, instance);
            }

            double cost = totalWorkTime * Config.HourlyRate;
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
            int totalWorkDayLengthExceedanceCount = 0;
            double totalWorkDayLengthExceedance = 0;
            int totalPrecedenceViolationCount = 0;
            for (int driverIndex = 0; driverIndex < Config.DriverCount; driverIndex++) {
                List<Trip> driverPath = driverPaths[driverIndex];
                if (driverPath.Count == 0) continue;
                Driver driver = instance.Drivers[driverIndex];

                Trip dayFirstTrip = driverPath[0];
                Trip prevTrip = driverPath[0];
                for (int driverTripIndex = 1; driverTripIndex < driverPath.Count; driverTripIndex++) {
                    Trip trip = driverPath[driverTripIndex];

                    // Check working day length
                    if (trip.DayIndex != dayFirstTrip.DayIndex) {
                        // End previous day
                        double workDayLength = WorkDayLength(dayFirstTrip, prevTrip, driver, instance);
                        double workDayLengthExceedance = Math.Max(0, workDayLength - Config.MaxWorkDayLength);
                        if (workDayLengthExceedance > 0) {
                            totalWorkDayLengthExceedanceCount++;
                            totalWorkDayLengthExceedance += workDayLengthExceedance;
                        }

                        // Start new day
                        dayFirstTrip = trip;
                    }

                    // Check precedence
                    if (!instance.TripSuccession[prevTrip.Index, trip.Index]) {
                        totalPrecedenceViolationCount++;
                    }

                    prevTrip = trip;
                }

                // End last day
                double lastWorkDayLength = WorkDayLength(dayFirstTrip, prevTrip, driver, instance);
                double lastWorkDayLengthExceedance = Math.Max(0, lastWorkDayLength - Config.MaxWorkDayLength);
                if (lastWorkDayLengthExceedance > 0) {
                    totalWorkDayLengthExceedanceCount++;
                    totalWorkDayLengthExceedance += lastWorkDayLengthExceedance;
                }
            }

            double workDayLengthPenaltyBase = totalWorkDayLengthExceedanceCount * Config.WorkDayLengthExceedancePenalty + totalWorkDayLengthExceedance * Config.WorkDayLengthExceedancePenaltyPerHour;
            double precendencePenaltyBase = totalPrecedenceViolationCount * Config.PrecendenceViolationPenalty;
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


        /* Travel cost */

        public static float TwoWayPayedTravelTime(Trip trip, Driver driver) {
            return driver.TwoWayPayedTravelTimes[trip.FirstStation];
        }

        public static float TwoWayPayedTravelCost(Trip trip, Driver driver) {
            return TwoWayPayedTravelTime(trip, driver) * Config.HourlyRate;
        }


        /* Work day start and end times */

        public static float WorkDayStartTimeWithTwoWayTravel(Trip firstDayTrip, Driver driver) {
            return firstDayTrip.StartTime - TwoWayPayedTravelTime(firstDayTrip, driver);
        }

        public static float WorkDayEndTimeWithoutTwoWayTravel(Trip firstDayTrip, Trip lastDayTrip, Instance instance) {
            return lastDayTrip.EndTime + instance.CarTravelTimes[lastDayTrip.LastStation, firstDayTrip.FirstStation];
        }

        public static float WorkDayLength(Trip firstDayTrip, Trip lastDayTrip, Driver driver, Instance instance) {
            return WorkDayEndTimeWithoutTwoWayTravel(firstDayTrip, lastDayTrip, instance) - WorkDayStartTimeWithTwoWayTravel(firstDayTrip, driver);
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
                    float oldWorkDayLength = WorkDayLength(oldTrip, oldTrip, driver, instance);
                    float newWorkDayLength = 0;
                    workDayLengthDiff = -oldWorkDayLength;
                    workDayLengthPenaltyDiff = GetWorkDayPenaltyBaseDiff(oldWorkDayLength, newWorkDayLength);
                } else {
                    // Trip after, but not before
                    Trip lastDayTrip = GetLastDayTrip(tripAfter, driver, assignment, instance);
                    float workDayEndTime = WorkDayEndTimeWithoutTwoWayTravel(oldTrip, lastDayTrip, instance);
                    float oldWorkDayLength = workDayEndTime - WorkDayStartTimeWithTwoWayTravel(oldTrip, driver);
                    float newWorkDayLength = workDayEndTime - WorkDayStartTimeWithTwoWayTravel(tripAfter, driver);
                    workDayLengthDiff = newWorkDayLength - oldWorkDayLength;
                    workDayLengthPenaltyDiff = GetWorkDayPenaltyBaseDiff(oldWorkDayLength, newWorkDayLength);

                    // Check if a precedence violation was removed after
                    if (!instance.TripSuccession[oldTrip.Index, tripAfter.Index]) precedenceViolationCountDiff--;
                }
            } else {
                if (tripAfter == null) {
                    // Trip before, but not after
                    Trip firstDayTrip = GetFirstDayTrip(tripBefore, driver, assignment, instance);
                    float workDayStartTime = WorkDayStartTimeWithTwoWayTravel(firstDayTrip, driver);
                    float oldWorkDayLength = WorkDayEndTimeWithoutTwoWayTravel(firstDayTrip, oldTrip, instance) - workDayStartTime;
                    float newWorkDayLength = WorkDayEndTimeWithoutTwoWayTravel(firstDayTrip, tripBefore, instance) - workDayStartTime;
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

            double costWithoutPenaltyDiff = workDayLengthDiff * Config.HourlyRate;
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
            float workDayLength = WorkDayLength(firstDayTrip, lastDayTrip, driver, instance);
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

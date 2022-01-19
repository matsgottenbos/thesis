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
            for (int driverIndex = 0; driverIndex < Config.GenDriverCount; driverIndex++) {
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

            double cost = totalWorkTime * Config.SalaryRate;
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

        public static (double, double[]) GetAssignmentBasePenalties(List<Trip>[] driverPaths, Instance instance) {
            int totalPrecedenceViolationCount = 0;
            int totalWorkDayLengthViolationCount = 0;
            double totalWorkDayLengthViolation = 0;
            int totalContractTimeViolationCount = 0;
            double totalContractTimeViolation = 0;
            double[] driverWorkedTime = new double[instance.Drivers.Length];
            for (int driverIndex = 0; driverIndex < Config.GenDriverCount; driverIndex++) {
                List<Trip> driverPath = driverPaths[driverIndex];
                Driver driver = instance.Drivers[driverIndex];
                if (driverPath.Count == 0) {
                    // Empty path, so we only need to check min contract time
                    if (driver.MinContractTime > 0) {
                        totalContractTimeViolationCount++;
                        totalContractTimeViolation += driver.MinContractTime;
                    }
                    continue;
                }
                double currentDriverWorkedTime = 0;

                Trip dayFirstTrip = driverPath[0];
                Trip prevTrip = driverPath[0];
                for (int driverTripIndex = 1; driverTripIndex < driverPath.Count; driverTripIndex++) {
                    Trip trip = driverPath[driverTripIndex];

                    // Check working day length
                    if (trip.DayIndex != dayFirstTrip.DayIndex) {
                        // End previous day
                        double workDayLength = WorkDayLength(dayFirstTrip, prevTrip, driver, instance);
                        //if (Config.DebugCheckAndLog) Console.WriteLine("Driver {0} work day {1} length: {2}", driverIndex, dayFirstTrip.DayIndex, workDayLength);
                        currentDriverWorkedTime += workDayLength;
                        double workDayLengthViolation = Math.Max(0, workDayLength - Config.MaxWorkDayLength);
                        if (workDayLengthViolation > 0) {
                            totalWorkDayLengthViolationCount++;
                            totalWorkDayLengthViolation += workDayLengthViolation;
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
                //if (Config.DebugCheckAndLog) Console.WriteLine("Driver {0} work day {1} length: {2}", driverIndex, dayFirstTrip.DayIndex, lastWorkDayLength);
                currentDriverWorkedTime += lastWorkDayLength;
                double lastWorkDayLengthViolation = Math.Max(0, lastWorkDayLength - Config.MaxWorkDayLength);
                if (lastWorkDayLengthViolation > 0) {
                    totalWorkDayLengthViolationCount++;
                    totalWorkDayLengthViolation += lastWorkDayLengthViolation;
                }

                // Check driver worked time
                if (currentDriverWorkedTime < driver.MinContractTime) {
                    totalContractTimeViolationCount++;
                    totalContractTimeViolation += driver.MinContractTime - currentDriverWorkedTime;
                } else if (currentDriverWorkedTime > driver.MaxContractTime) {
                    totalContractTimeViolationCount++;
                    totalContractTimeViolation += currentDriverWorkedTime - driver.MaxContractTime;
                }

                driverWorkedTime[driverIndex] = currentDriverWorkedTime;
            }

            double precendencePenaltyBase = totalPrecedenceViolationCount * Config.PrecendenceViolationPenalty;
            double workDayLengthPenaltyBase = totalWorkDayLengthViolationCount * Config.WorkDayLengthViolationPenalty + totalWorkDayLengthViolation * Config.WorkDayLengthViolationPenaltyPerMin;
            double contractTimePenaltyBase = totalContractTimeViolationCount * Config.ContractTimeViolationPenalty + totalContractTimeViolation * Config.ContractTimeViolationPenaltyPerMin;
            double penaltyBase = precendencePenaltyBase + workDayLengthPenaltyBase + contractTimePenaltyBase;

            // Debug
            if (Config.DebugCheckAndLogOperations) {
                Console.WriteLine("Precedence violation count: {0}", totalPrecedenceViolationCount);
                Console.WriteLine("WDL violation count: {0}", totalWorkDayLengthViolationCount);
                Console.WriteLine("WDL violation amount: {0}", totalWorkDayLengthViolation);
                Console.WriteLine("CT violation count: {0}", totalContractTimeViolationCount);
                Console.WriteLine("CT violation amount: {0}", totalContractTimeViolation);
            }

            return (penaltyBase, driverWorkedTime);
        }


        /* Assignment cost with penalties */

        public static (double, double, double, double[]) AssignmentCostWithPenalties(List<Trip>[] driverPaths, Instance instance, float penaltyFactor) {
            double costWithoutPenalty = AssignmentCostWithoutPenalties(driverPaths, instance);
            (double penaltyBase, double[] driverWorkedTime) = GetAssignmentBasePenalties(driverPaths, instance);
            double penalty = penaltyBase * penaltyFactor;
            double cost = costWithoutPenalty + penalty;
            return (cost, costWithoutPenalty, penaltyBase, driverWorkedTime);
        }
        public static (double, double, double, double[]) AssignmentCostWithPenalties(int[] assignmentIndices, Instance instance, float penaltyFactor) {
            List<Trip>[] driverPaths = GetPathPerDriver(assignmentIndices, instance);
            return AssignmentCostWithPenalties(driverPaths, instance, penaltyFactor);
        }
        public static (double, double, double, double[]) AssignmentCostWithPenalties(Driver[] assignment, Instance instance, float penaltyFactor) {
            List<Trip>[] driverPaths = GetPathPerDriver(assignment, instance);
            return AssignmentCostWithPenalties(driverPaths, instance, penaltyFactor);
        }


        /* Assignment helpers */

        static List<Trip>[] GetPathPerDriver(int[] assignmentIndices, Instance instance) {
            List<Trip>[] driverPaths = new List<Trip>[Config.GenDriverCount];
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
            List<Trip>[] driverPaths = new List<Trip>[Config.GenDriverCount];
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
            return TwoWayPayedTravelTime(trip, driver) * Config.SalaryRate;
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

        public static (double, double, double, float) UnassignTripCostDiffWithoutContractTime(Trip oldTrip, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance, float penaltyFactor) {
            Trip tripBefore = GetDriverSameDayTripBefore(oldTrip.Index, driver, oldTrip.DayIndex, tripToIgnore, assignment, instance);
            Trip tripAfter = GetDriverSameDayTripAfter(oldTrip.Index, driver, oldTrip.DayIndex, tripToIgnore, assignment, instance);

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
                    Trip lastDayTrip = GetLastDayTrip(tripAfter, driver, tripToIgnore, assignment, instance);
                    float oldWorkDayLength = WorkDayEndTimeWithoutTwoWayTravel(oldTrip, lastDayTrip, instance) - WorkDayStartTimeWithTwoWayTravel(oldTrip, driver);
                    float newWorkDayLength = WorkDayEndTimeWithoutTwoWayTravel(tripAfter, lastDayTrip, instance) - WorkDayStartTimeWithTwoWayTravel(tripAfter, driver);
                    workDayLengthDiff = newWorkDayLength - oldWorkDayLength;
                    workDayLengthPenaltyDiff = GetWorkDayPenaltyBaseDiff(oldWorkDayLength, newWorkDayLength);

                    // Check if a precedence violation was removed after
                    if (!instance.TripSuccession[oldTrip.Index, tripAfter.Index]) precedenceViolationCountDiff--;
                }
            } else {
                if (tripAfter == null) {
                    // Trip before, but not after
                    Trip firstDayTrip = GetFirstDayTrip(tripBefore, driver, tripToIgnore, assignment, instance);
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

            // Precedence penalty
            double precedencePenaltyBaseDiff = precedenceViolationCountDiff * Config.PrecendenceViolationPenalty; // TODO: include next-day successors

            // Debug
            if (Config.DebugCheckAndLogOperations) Console.WriteLine("Precedence violation count: {0}", precedenceViolationCountDiff);

            double costWithoutPenaltyDiff = workDayLengthDiff * Config.SalaryRate;
            double penaltyBaseDiff = workDayLengthPenaltyDiff + precedencePenaltyBaseDiff;
            double costDiff = costWithoutPenaltyDiff + penaltyBaseDiff * penaltyFactor;
            return (costDiff, costWithoutPenaltyDiff, penaltyBaseDiff, workDayLengthDiff);
        }

        public static (double, double, double, float) UnassignTripCostDiff(Trip oldTrip, Driver driver, Trip tripToIgnore, Driver[] assignment, float driverOldWorkedTime, Instance instance, float penaltyFactor) {
            if (Config.DebugCheckAndLogOperations) Console.WriteLine("Unassign trip {0} from driver {1}", oldTrip.Index, driver.Index);
            (double costDiff, double costWithoutPenaltyDiff, double penaltyBaseDiffWithoutContractTime, float workDayLengthDiff) = UnassignTripCostDiffWithoutContractTime(oldTrip, driver, tripToIgnore, assignment, instance, penaltyFactor);

            // Worked time penalty
            float contractTimePenaltyBaseDiff = GetContractTimePenaltyBaseDiff(driverOldWorkedTime, driverOldWorkedTime + workDayLengthDiff, driver);
            float contractTimePenaltyDiff = contractTimePenaltyBaseDiff * penaltyFactor;

            return (costDiff + contractTimePenaltyDiff, costWithoutPenaltyDiff, penaltyBaseDiffWithoutContractTime + contractTimePenaltyBaseDiff, workDayLengthDiff);
        }

        public static (double, double, double, float) AssignTripCostDiff(Trip newTrip, Driver driver, Trip tripToIgnore, Driver[] assignment, float driverOldWorkedTime, Instance instance, float penaltyFactor) {
            if (Config.DebugCheckAndLogOperations) Console.WriteLine("Assign trip {0} to driver {1}", newTrip.Index, driver.Index);
            (double costDiff, double costWithoutPenaltyDiff, double penaltyBaseDiff, float workDayLengthDiff) = UnassignTripCostDiffWithoutContractTime(newTrip, driver, tripToIgnore, assignment, instance, penaltyFactor);

            // Worked time penalty
            float contractTimePenaltyBaseDiff = GetContractTimePenaltyBaseDiff(driverOldWorkedTime, driverOldWorkedTime - workDayLengthDiff, driver);
            float contractTimePenaltyDiff = contractTimePenaltyBaseDiff * penaltyFactor;

            return (-costDiff + contractTimePenaltyDiff, -costWithoutPenaltyDiff, -penaltyBaseDiff + contractTimePenaltyBaseDiff, -workDayLengthDiff);
        }

        static float GetWorkDayPenaltyBaseDiff(float oldWorkDayLength, float newWorkDayLength) {
            float oldWorkDayLengthViolation = Math.Max(0, oldWorkDayLength - Config.MaxWorkDayLength);
            float newWorkDayLengthViolation = Math.Max(0, newWorkDayLength - Config.MaxWorkDayLength);
            float amountPenaltyBaseDiff = (newWorkDayLengthViolation - oldWorkDayLengthViolation) * Config.WorkDayLengthViolationPenaltyPerMin;

            float countPenaltyBaseDiff = 0;
            if (oldWorkDayLengthViolation > 0) {
                if (newWorkDayLengthViolation == 0) countPenaltyBaseDiff = -Config.WorkDayLengthViolationPenalty;
            } else {
                if (newWorkDayLengthViolation > 0) countPenaltyBaseDiff = Config.WorkDayLengthViolationPenalty;
            }

            // Debug
            if (Config.DebugCheckAndLogOperations) {
                Console.WriteLine("Work day length: {0} -> {1}", oldWorkDayLength, newWorkDayLength);
                Console.WriteLine("WDL violation count: {0}", countPenaltyBaseDiff / Config.WorkDayLengthViolationPenalty);
                Console.WriteLine("WDL violation amount: {0} ({1} -> {2})", newWorkDayLengthViolation - oldWorkDayLengthViolation, oldWorkDayLengthViolation, newWorkDayLengthViolation);
            }

            return amountPenaltyBaseDiff + countPenaltyBaseDiff;
        }

        static float GetContractTimePenaltyBaseDiff(float oldWorkedTime, float newWorkedTime, Driver driver) {
            float contractTimePenaltyBaseDiff = 0;

            float oldContractTimeViolation = 0;
            if (oldWorkedTime < driver.MinContractTime) {
                oldContractTimeViolation += driver.MinContractTime - oldWorkedTime;
                contractTimePenaltyBaseDiff -= Config.ContractTimeViolationPenalty;
            } else if (oldWorkedTime > driver.MaxContractTime) {
                oldContractTimeViolation += oldWorkedTime - driver.MaxContractTime;
                contractTimePenaltyBaseDiff -= Config.ContractTimeViolationPenalty;
            }

            float newContractTimeViolation = 0;
            if (newWorkedTime < driver.MinContractTime) {
                newContractTimeViolation += driver.MinContractTime - newWorkedTime;
                contractTimePenaltyBaseDiff += Config.ContractTimeViolationPenalty;
            } else if (newWorkedTime > driver.MaxContractTime) {
                newContractTimeViolation += newWorkedTime - driver.MaxContractTime;
                contractTimePenaltyBaseDiff += Config.ContractTimeViolationPenalty;
            }

            // Debug
            if (Config.DebugCheckAndLogOperations) {
                Console.WriteLine("Worked time: {0} -> {1}", oldWorkedTime, newWorkedTime);
                Console.WriteLine("CT violation count: {0}", contractTimePenaltyBaseDiff / Config.ContractTimeViolationPenalty);
                Console.WriteLine("CT violation amount: {0} ({1} -> {2})", newContractTimeViolation - oldContractTimeViolation, oldContractTimeViolation, newContractTimeViolation);
            }

            contractTimePenaltyBaseDiff += (newContractTimeViolation - oldContractTimeViolation) * Config.ContractTimeViolationPenaltyPerMin;

            return contractTimePenaltyBaseDiff;
        }


        /* Getting trips */

        public static Trip GetDriverSameDayTripBefore(int tripIndex, Driver driver, int dayIndex, Trip tripToIgnore, Driver[] assignment, Instance instance) {
            for (int searchTripIndex = tripIndex - 1; searchTripIndex >= 0; searchTripIndex--) {
                Trip searchTrip = instance.Trips[searchTripIndex];
                if (searchTrip.DayIndex != dayIndex) return null;

                if (assignment[searchTripIndex] == driver && searchTrip != tripToIgnore) {
                    return searchTrip;
                }
            }
            return null;
        }
        public static Trip GetDriverSameDayTripAfter(int tripIndex, Driver driver, int dayIndex, Trip tripToIgnore, Driver[] assignment, Instance instance) {
            for (int searchTripIndex = tripIndex + 1; searchTripIndex < instance.Trips.Length; searchTripIndex++) {
                Trip searchTrip = instance.Trips[searchTripIndex];
                if (searchTrip.DayIndex != dayIndex) return null;

                if (assignment[searchTripIndex] == driver && searchTrip != tripToIgnore) {
                    return searchTrip;
                }
            }
            return null;
        }

        static Trip GetFirstDayTrip(Trip someTripOnDay, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance) {
            Trip firstDayTrip = someTripOnDay;
            for (int searchTripIndex = someTripOnDay.Index - 1; searchTripIndex >= 0; searchTripIndex--) {
                Trip searchTrip = instance.Trips[searchTripIndex];
                if (searchTrip.DayIndex != someTripOnDay.DayIndex) break;
                if (assignment[searchTripIndex] == driver && searchTrip != tripToIgnore) firstDayTrip = searchTrip;
            }
            return firstDayTrip;
        }
        static Trip GetLastDayTrip(Trip someTripOnDay, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance) {
            Trip lastDayTrip = someTripOnDay;
            for (int searchTripIndex = someTripOnDay.Index + 1; searchTripIndex < instance.Trips.Length; searchTripIndex++) {
                Trip searchTrip = instance.Trips[searchTripIndex];
                if (searchTrip.DayIndex != someTripOnDay.DayIndex) break;
                if (assignment[searchTripIndex] == driver && searchTrip != tripToIgnore) lastDayTrip = searchTrip;
            }
            return lastDayTrip;
        }

        static float GetDriverWorkDayLength(Trip someTripOnDay, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance) {
            Trip firstDayTrip = GetFirstDayTrip(someTripOnDay, driver, tripToIgnore, assignment, instance);
            Trip lastDayTrip = GetLastDayTrip(someTripOnDay, driver, tripToIgnore, assignment, instance);
            float workDayLength = WorkDayLength(firstDayTrip, lastDayTrip, driver, instance);
            return workDayLength;
        }
    }
}

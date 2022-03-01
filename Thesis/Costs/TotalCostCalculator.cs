using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class TotalCostCalculator {
        /** Get assignment cost without penalties */

        public static double AssignmentCostWithoutPenalties(List<Trip>[] driverPaths, Instance instance) {
            // Determine cost
            int totalWorkTime = 0;
            for (int driverIndex = 0; driverIndex < Config.GenDriverCount; driverIndex++) {
                List<Trip> driverPath = driverPaths[driverIndex];
                if (driverPath.Count == 0) continue;
                Driver driver = instance.Drivers[driverIndex];

                Trip dayFirstTrip = driverPath[0];
                Trip prevTrip = driverPath[0];
                for (int driverTripIndex = 1; driverTripIndex < driverPath.Count; driverTripIndex++) {
                    Trip trip = driverPath[driverTripIndex];

                    // Working day length
                    if (!CostHelper.AreSameShift(prevTrip, trip, instance)) {
                        // End previous day
                        totalWorkTime += CostHelper.ShiftLength(dayFirstTrip, prevTrip, driver, instance);

                        // Start new day
                        dayFirstTrip = trip;
                    }

                    prevTrip = trip;
                }

                // End last day
                totalWorkTime += CostHelper.ShiftLength(dayFirstTrip, prevTrip, driver, instance);
            }

            double cost = totalWorkTime * Config.SalaryRate;
            return cost;
        }


        /** Get assignment base penalties */

        public static (double, int[]) GetAssignmentBasePenalties(List<Trip>[] driverPaths, Instance instance, int debugIterationNum) {
            int totalPrecedenceViolationCount = 0;
            int totalWorkDayLengthViolationCount = 0;
            int totalWorkDayLengthViolation = 0;
            int totalRestTimeViolationCount = 0;
            int totalRestTimeViolation = 0;
            int totalContractTimeViolationCount = 0;
            int totalContractTimeViolation = 0;
            int[] driverWorkedTime = new int[instance.Drivers.Length];
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

                int currentDriverWorkedTime = 0;
                Trip dayFirstTrip = driverPath[0];
                Trip prevTrip = driverPath[0];
                for (int driverTripIndex = 1; driverTripIndex < driverPath.Count; driverTripIndex++) {
                    Trip trip = driverPath[driverTripIndex];

                    // Check working day length
                    if (!CostHelper.AreSameShift(prevTrip, trip, instance)) {
                        /* End previous day */
                        int workDayLength = CostHelper.ShiftLength(dayFirstTrip, prevTrip, driver, instance);
                        currentDriverWorkedTime += workDayLength;
                        int workDayLengthViolation = Math.Max(0, workDayLength - Config.MaxWorkDayLength);
                        if (workDayLengthViolation > 0) {
                            totalWorkDayLengthViolationCount++;
                            totalWorkDayLengthViolation += workDayLengthViolation;
                        }

                        int restTime = CostHelper.RestTime(dayFirstTrip, prevTrip, trip, driver, instance);
                        int restTimeViolation = Math.Max(0, Config.MinRestTime - restTime);
                        if (restTimeViolation > 0) {
                            totalRestTimeViolationCount++;
                            totalRestTimeViolation += restTimeViolation;
                        }

                        // Start new day
                        dayFirstTrip = trip;

                        if (Config.DebugCheckAndLogOperations) {
                            SaDebugger.CurrentChecked.DriverPathStrings[driverIndex] += prevTrip.Index + "|";
                        }
                    } else if (Config.DebugCheckAndLogOperations) {
                        SaDebugger.CurrentChecked.DriverPathStrings[driverIndex] += prevTrip.Index + "-";
                    }

                    // Check precedence
                    if (!instance.TripSuccession[prevTrip.Index, trip.Index]) {
                        totalPrecedenceViolationCount++;
                    }

                    prevTrip = trip;
                }

                // End last day
                int lastWorkDayLength = CostHelper.ShiftLength(dayFirstTrip, prevTrip, driver, instance);
                currentDriverWorkedTime += lastWorkDayLength;
                int lastWorkDayLengthViolation = Math.Max(0, lastWorkDayLength - Config.MaxWorkDayLength);
                if (lastWorkDayLengthViolation > 0) {
                    totalWorkDayLengthViolationCount++;
                    totalWorkDayLengthViolation += lastWorkDayLengthViolation;
                }
                if (Config.DebugCheckAndLogOperations) {
                    SaDebugger.CurrentChecked.DriverPathStrings[driverIndex] += prevTrip.Index;
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
            double workDayLengthPenaltyBase = totalWorkDayLengthViolationCount * Config.ShiftLengthViolationPenalty + totalWorkDayLengthViolation * Config.ShiftLengthViolationPenaltyPerMin;
            double restTimePenaltyBase = totalRestTimeViolationCount * Config.RestTimeViolationPenalty + totalRestTimeViolation * Config.RestTimeViolationPenaltyPerMin;
            double contractTimePenaltyBase = totalContractTimeViolationCount * Config.ContractTimeViolationPenalty + totalContractTimeViolation * Config.ContractTimeViolationPenaltyPerMin;
            double penaltyBase = precendencePenaltyBase + workDayLengthPenaltyBase + restTimePenaltyBase + contractTimePenaltyBase;

            // Debug
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.CurrentChecked.Info.PrecedenceViolationCount = totalPrecedenceViolationCount;
                SaDebugger.CurrentChecked.Info.SlViolationCount = totalWorkDayLengthViolationCount;
                SaDebugger.CurrentChecked.Info.SlViolationAmount = totalWorkDayLengthViolation;
                SaDebugger.CurrentChecked.Info.RtViolationCount = totalRestTimeViolationCount;
                SaDebugger.CurrentChecked.Info.RtViolationAmount = totalRestTimeViolation;
                SaDebugger.CurrentChecked.Info.CtViolationCount = totalContractTimeViolationCount;
                SaDebugger.CurrentChecked.Info.CtViolationAmount = totalContractTimeViolation;
                SaDebugger.CurrentChecked.Info.DriversWorkedTime = driverWorkedTime;
            }

            return (penaltyBase, driverWorkedTime);
        }


        /** Get assignment cost with penalties */

        public static (double, double, double, int[]) AssignmentCostWithPenalties(Driver[] assignment, Instance instance, float penaltyFactor, int debugIterationNum) {
            List<Trip>[] driverPaths = GetPathPerDriver(assignment, instance);
            double costWithoutPenalty = AssignmentCostWithoutPenalties(driverPaths, instance);
            (double penaltyBase, int[] driverWorkedTime) = GetAssignmentBasePenalties(driverPaths, instance, debugIterationNum);
            double penalty = penaltyBase * penaltyFactor;
            double cost = costWithoutPenalty + penalty;

            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.CurrentChecked.Info.Cost = cost;
                SaDebugger.CurrentChecked.Info.CostWithoutPenalty = costWithoutPenalty;
                SaDebugger.CurrentChecked.Info.PenaltyBase = penaltyBase;
            }

            return (cost, costWithoutPenalty, penaltyBase, driverWorkedTime);
        }


        /** Helper: get list of trips that each driver is assigned to */

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
    }
}

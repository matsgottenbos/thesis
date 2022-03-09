using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class TotalCostCalculator {
        /** Get assignment cost */
        public static (double, double, double, int[]) GetAssignmentCost(Driver[] assignment, Instance instance, float penaltyFactor) {
            List<Trip>[] driverPaths = GetPathPerDriver(assignment, instance);

            double cost = 0;
            double costWithoutPenalty = 0;
            double penaltyBase = 0;
            int[] driversWorkedTime = new int[instance.Drivers.Length];
            for (int driverIndex = 0; driverIndex < instance.Drivers.Length; driverIndex++) {
                List<Trip> driverPath = driverPaths[driverIndex];
                Driver driver = instance.Drivers[driverIndex];

                (double driverCost, double driverCostWithoutPenalty, double driverPenaltyBase, int driverWorkedTime) = GetDriverPathCost(driverPath, driver, instance, penaltyFactor, false);
                 
                cost += driverCost;
                costWithoutPenalty += driverCostWithoutPenalty;
                penaltyBase += driverPenaltyBase;
                driversWorkedTime[driverIndex] = driverWorkedTime;
            }

            return (cost, costWithoutPenalty, penaltyBase, driversWorkedTime);
        }

        public static (double, double, double, int) GetDriverPathCost(List<Trip> driverPath, Driver driver, Instance instance, float penaltyFactor, bool shouldDebug = true) {
            int totalPrecedenceViolationCount = 0;
            int totalShiftLengthViolationCount = 0;
            int totalShiftLengthViolation = 0;
            int totalRestTimeViolationCount = 0;
            int totalRestTimeViolation = 0;
            int totalContractTimeViolationCount = 0;
            int totalContractTimeViolation = 0;
            int totalWorkedTime = 0;

            if (driverPath.Count == 0) {
                // Empty path, so we only need to check min contract time
                if (driver.MinContractTime > 0) {
                    totalContractTimeViolationCount++;
                    totalContractTimeViolation += driver.MinContractTime;
                }
            } else {
                Trip dayFirstTrip = driverPath[0];
                Trip prevTrip = driverPath[0];
                for (int driverTripIndex = 1; driverTripIndex < driverPath.Count; driverTripIndex++) {
                    Trip trip = driverPath[driverTripIndex];

                    // Check working day length
                    if (CostHelper.AreSameShift(prevTrip, trip, instance)) {
                        // Check precedence
                        if (!instance.TripSuccession[prevTrip.Index, trip.Index]) {
                            totalPrecedenceViolationCount++;
                        }

                        if (Config.DebugCheckAndLogOperations && shouldDebug) {
                            SaDebugger.GetCurrentCheckedTotal().DriverPathString += prevTrip.Index + "-";
                        }
                    } else {
                        /* End previous day */
                        int shiftLength = CostHelper.ShiftLength(dayFirstTrip, prevTrip, driver, instance);
                        totalWorkedTime += shiftLength;
                        int shiftLengthViolation = Math.Max(0, shiftLength - Config.MaxWorkDayLength);
                        if (shiftLengthViolation > 0) {
                            totalShiftLengthViolationCount++;
                            totalShiftLengthViolation += shiftLengthViolation;
                        }

                        int restTime = CostHelper.RestTime(dayFirstTrip, prevTrip, trip, driver, instance);
                        int restTimeViolation = Math.Max(0, Config.MinRestTime - restTime);
                        if (restTimeViolation > 0) {
                            totalRestTimeViolationCount++;
                            totalRestTimeViolation += restTimeViolation;
                        }

                        // Start new day
                        dayFirstTrip = trip;

                        if (Config.DebugCheckAndLogOperations && shouldDebug) {
                            SaDebugger.GetCurrentCheckedTotal().DriverPathString += prevTrip.Index + "|";
                            SaDebugger.GetCurrentCheckedTotal().ShiftLengths.Add(shiftLength);
                            SaDebugger.GetCurrentCheckedTotal().RestTimes.Add(restTime);
                        }
                    }

                    prevTrip = trip;
                }

                // End last day
                int lastShiftLength = CostHelper.ShiftLength(dayFirstTrip, prevTrip, driver, instance);
                totalWorkedTime += lastShiftLength;
                int lastShiftLengthViolation = Math.Max(0, lastShiftLength - Config.MaxWorkDayLength);
                if (lastShiftLengthViolation > 0) {
                    totalShiftLengthViolationCount++;
                    totalShiftLengthViolation += lastShiftLengthViolation;
                }
                if (Config.DebugCheckAndLogOperations && shouldDebug) {
                    SaDebugger.GetCurrentCheckedTotal().DriverPathString += prevTrip.Index;
                    SaDebugger.GetCurrentCheckedTotal().ShiftLengths.Add(lastShiftLength);
                }

                // Check driver worked time
                if (totalWorkedTime < driver.MinContractTime) {
                    totalContractTimeViolationCount++;
                    totalContractTimeViolation += driver.MinContractTime - totalWorkedTime;
                } else if (totalWorkedTime > driver.MaxContractTime) {
                    totalContractTimeViolationCount++;
                    totalContractTimeViolation += totalWorkedTime - driver.MaxContractTime;
                }
            }
            

            double costWithoutPenalty = totalWorkedTime * Config.SalaryRate;

            double precendencePenaltyBase = totalPrecedenceViolationCount * Config.PrecendenceViolationPenalty;
            double workDayLengthPenaltyBase = totalShiftLengthViolationCount * Config.ShiftLengthViolationPenalty + totalShiftLengthViolation * Config.ShiftLengthViolationPenaltyPerMin;
            double restTimePenaltyBase = totalRestTimeViolationCount * Config.RestTimeViolationPenalty + totalRestTimeViolation * Config.RestTimeViolationPenaltyPerMin;
            double contractTimePenaltyBase = totalContractTimeViolationCount * Config.ContractTimeViolationPenalty + totalContractTimeViolation * Config.ContractTimeViolationPenaltyPerMin;
            double penaltyBase = precendencePenaltyBase + workDayLengthPenaltyBase + restTimePenaltyBase + contractTimePenaltyBase;

            double cost = costWithoutPenalty + penaltyBase * penaltyFactor;

            if (Config.DebugCheckAndLogOperations && shouldDebug) {
                SaDebugger.GetCurrentCheckedTotal().Total.PrecedenceViolationCount = totalPrecedenceViolationCount;
                SaDebugger.GetCurrentCheckedTotal().Total.SlViolationCount = totalShiftLengthViolationCount;
                SaDebugger.GetCurrentCheckedTotal().Total.SlViolationAmount = totalShiftLengthViolation;
                SaDebugger.GetCurrentCheckedTotal().Total.RtViolationCount = totalRestTimeViolationCount;
                SaDebugger.GetCurrentCheckedTotal().Total.RtViolationAmount = totalRestTimeViolation;
                SaDebugger.GetCurrentCheckedTotal().Total.CtViolationCount = totalContractTimeViolationCount;
                SaDebugger.GetCurrentCheckedTotal().Total.CtViolationAmount = totalContractTimeViolation;

                SaDebugger.GetCurrentCheckedTotal().Total.Cost = cost;
                SaDebugger.GetCurrentCheckedTotal().Total.CostWithoutPenalty = costWithoutPenalty;
                SaDebugger.GetCurrentCheckedTotal().Total.PenaltyBase = penaltyBase;
            }

            return (cost, costWithoutPenalty, penaltyBase, totalWorkedTime);
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

        public static List<Trip> GetSingleDriverPath(Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance) {
            List<Trip> driverPath = new List<Trip>();

            for (int tripIndex = 0; tripIndex < assignment.Length; tripIndex++) {
                Driver tripDriver = assignment[tripIndex];
                if (tripDriver != driver) continue;
                Trip trip = instance.Trips[tripIndex];
                if (trip == tripToIgnore) continue;
                driverPath.Add(trip);
            }
            return driverPath;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class TotalCostCalculator {
        /** Get assignment cost */
        public static (double, double, double, int[]) GetAssignmentCost(SaInfo info) {
            List<Trip>[] driverPaths = GetPathPerDriver(info);

            double cost = 0;
            double costWithoutPenalty = 0;
            double basePenalty = 0;
            int[] driversWorkedTime = new int[info.Instance.AllDrivers.Length];
            for (int driverIndex = 0; driverIndex < info.Instance.AllDrivers.Length; driverIndex++) {
                List<Trip> driverPath = driverPaths[driverIndex];
                Driver driver = info.Instance.AllDrivers[driverIndex];

                (double driverCost, double driverCostWithoutPenalty, double driverBasePenalty, int driverWorkedTime) = GetDriverPathCost(driverPath, driver, info, false);
                 
                cost += driverCost;
                costWithoutPenalty += driverCostWithoutPenalty;
                basePenalty += driverBasePenalty;
                driversWorkedTime[driverIndex] = driverWorkedTime;
            }

            return (cost, costWithoutPenalty, basePenalty, driversWorkedTime);
        }

        public static (double, double, double, int) GetDriverPathCost(List<Trip> driverPath, Driver driver, SaInfo info, bool shouldDebug = true) {
            int totalPrecedenceViolationCount = 0;
            int totalShiftLengthViolationCount = 0;
            int totalShiftLengthViolation = 0;
            int totalRestTimeViolationCount = 0;
            int totalRestTimeViolation = 0;
            int totalContractTimeViolation = 0;
            int totalContractTimeViolationCount = 0;
            int totalWorkedTime = 0;
            double totalCostWithoutPenalty = 0;

            if (driverPath.Count == 0) {
                // Empty path
                totalContractTimeViolation = driver.GetMinContractTimeViolation(0);
                if (totalContractTimeViolation > 0) totalContractTimeViolationCount = 1;
            } else {
                Trip shiftFirstTrip = driverPath[0];
                Trip prevTrip = driverPath[0];
                for (int driverTripIndex = 1; driverTripIndex < driverPath.Count; driverTripIndex++) {
                    Trip trip = driverPath[driverTripIndex];

                    // Check shift length
                    if (info.Instance.AreSameShift(prevTrip, trip)) {
                        // Check precedence
                        if (!info.Instance.TripSuccession[prevTrip.Index, trip.Index]) {
                            totalPrecedenceViolationCount++;
                        }

                        #if DEBUG
                        if (Config.DebugCheckAndLogOperations && shouldDebug) {
                            SaDebugger.GetCurrentCheckedTotal().DriverPathString += prevTrip.Index + "-";
                        }
                        #endif
                    } else {
                        /* End previous shift */
                        int shiftLength = driver.ShiftLength(shiftFirstTrip, prevTrip);
                        totalWorkedTime += shiftLength;
                        totalCostWithoutPenalty += driver.ShiftCost(shiftFirstTrip, prevTrip);
                        int shiftLengthViolation = Math.Max(0, shiftLength - Config.MaxShiftLength);
                        if (shiftLengthViolation > 0) {
                            totalShiftLengthViolationCount++;
                            totalShiftLengthViolation += shiftLengthViolation;
                        }

                        int restTime = driver.RestTime(shiftFirstTrip, prevTrip, trip);
                        int restTimeViolation = Math.Max(0, Config.MinRestTime - restTime);
                        if (restTimeViolation > 0) {
                            totalRestTimeViolationCount++;
                            totalRestTimeViolation += restTimeViolation;
                        }

                        // Start new shift
                        shiftFirstTrip = trip;

                        #if DEBUG
                        if (Config.DebugCheckAndLogOperations && shouldDebug) {
                            SaDebugger.GetCurrentCheckedTotal().DriverPathString += prevTrip.Index + "|";
                            SaDebugger.GetCurrentCheckedTotal().ShiftLengths.Add(shiftLength);
                            SaDebugger.GetCurrentCheckedTotal().RestTimes.Add(restTime);
                        }
                        #endif
                    }

                    prevTrip = trip;
                }

                // End last shift
                int lastShiftLength = driver.ShiftLength(shiftFirstTrip, prevTrip);
                totalWorkedTime += lastShiftLength;
                totalCostWithoutPenalty += driver.ShiftCost(shiftFirstTrip, prevTrip);
                int lastShiftLengthViolation = Math.Max(0, lastShiftLength - Config.MaxShiftLength);
                if (lastShiftLengthViolation > 0) {
                    totalShiftLengthViolationCount++;
                    totalShiftLengthViolation += lastShiftLengthViolation;
                }

                #if DEBUG
                if (Config.DebugCheckAndLogOperations && shouldDebug) {
                    SaDebugger.GetCurrentCheckedTotal().DriverPathString += prevTrip.Index;
                    SaDebugger.GetCurrentCheckedTotal().ShiftLengths.Add(lastShiftLength);
                }
                #endif

                // Check driver worked time
                totalContractTimeViolation = driver.GetTotalContractTimeViolation(totalWorkedTime);
                totalContractTimeViolationCount = totalContractTimeViolation > 0 ? 1 : 0;
            }

            double precendenceBasePenalty = totalPrecedenceViolationCount * Config.PrecendenceViolationPenalty;
            double shiftLengthBasePenalty = totalShiftLengthViolationCount * Config.ShiftLengthViolationPenalty + totalShiftLengthViolation * Config.ShiftLengthViolationPenaltyPerMin;
            double restTimeBasePenalty = totalRestTimeViolationCount * Config.RestTimeViolationPenalty + totalRestTimeViolation * Config.RestTimeViolationPenaltyPerMin;
            double contractTimeBasePenalty = totalContractTimeViolationCount * Config.ContractTimeViolationPenalty + totalContractTimeViolation * Config.ContractTimeViolationPenaltyPerMin;
            double basePenalty = precendenceBasePenalty + shiftLengthBasePenalty + restTimeBasePenalty + contractTimeBasePenalty;

            double cost = totalCostWithoutPenalty + basePenalty * info.PenaltyFactor;

            #if DEBUG
            if (Config.DebugCheckAndLogOperations && shouldDebug) {
                SaDebugger.GetCurrentCheckedTotal().Total.PrecedenceViolationCount = totalPrecedenceViolationCount;
                SaDebugger.GetCurrentCheckedTotal().Total.SlViolationCount = totalShiftLengthViolationCount;
                SaDebugger.GetCurrentCheckedTotal().Total.SlViolationAmount = totalShiftLengthViolation;
                SaDebugger.GetCurrentCheckedTotal().Total.RtViolationCount = totalRestTimeViolationCount;
                SaDebugger.GetCurrentCheckedTotal().Total.RtViolationAmount = totalRestTimeViolation;
                SaDebugger.GetCurrentCheckedTotal().Total.CtViolationCount = totalContractTimeViolationCount;
                SaDebugger.GetCurrentCheckedTotal().Total.CtViolationAmount = totalContractTimeViolation;

                SaDebugger.GetCurrentCheckedTotal().Total.Cost = cost;
                SaDebugger.GetCurrentCheckedTotal().Total.CostWithoutPenalty = totalCostWithoutPenalty;
                SaDebugger.GetCurrentCheckedTotal().Total.BasePenalty = basePenalty;
            }
            #endif

            return (cost, totalCostWithoutPenalty, basePenalty, totalWorkedTime);
        }

        /** Helper: get list of trips that each driver is assigned to */
        static List<Trip>[] GetPathPerDriver(SaInfo info) {
            List<Trip>[] driverPaths = new List<Trip>[info.Instance.AllDrivers.Length];
            for (int driverIndex = 0; driverIndex < driverPaths.Length; driverIndex++) {
                driverPaths[driverIndex] = new List<Trip>();
            }

            for (int tripIndex = 0; tripIndex < info.Assignment.Length; tripIndex++) {
                Driver driver = info.Assignment[tripIndex];
                Trip trip = info.Instance.Trips[tripIndex];
                driverPaths[driver.AllDriversIndex].Add(trip);
            }
            return driverPaths;
        }

        public static List<Trip> GetSingleDriverPath(Driver driver, Trip tripToIgnore, SaInfo info) {
            List<Trip> driverPath = new List<Trip>();

            for (int tripIndex = 0; tripIndex < info.Assignment.Length; tripIndex++) {
                Driver tripDriver = info.Assignment[tripIndex];
                if (tripDriver != driver) continue;
                Trip trip = info.Instance.Trips[tripIndex];
                if (trip == tripToIgnore) continue;
                driverPath.Add(trip);
            }
            return driverPath;
        }
    }
}

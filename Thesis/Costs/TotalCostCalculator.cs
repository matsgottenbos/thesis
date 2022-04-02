using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class TotalCostCalculator {
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
            int totalInvalidHotelCount = 0;
            int totalWorkedTime = 0;
            double totalCostWithoutPenalty = 0;

            if (driverPath.Count == 0) {
                // Empty path
                totalContractTimeViolation = driver.GetMinContractTimeViolation(0);
                if (totalContractTimeViolation > 0) totalContractTimeViolationCount = 1;
            } else {
                Trip shiftFirstTrip = driverPath[0];
                Trip parkingTrip = driverPath[0];
                Trip prevTrip = driverPath[0];
                Trip beforeHotelTrip = null;
                for (int driverTripIndex = 1; driverTripIndex < driverPath.Count; driverTripIndex++) {
                    Trip searchTrip = driverPath[driverTripIndex];

                    if (info.Instance.AreSameShift(prevTrip, searchTrip)) {
                        /* Same shift */
                        // Check precedence
                        if (!info.Instance.TripSuccession[prevTrip.Index, searchTrip.Index]) {
                            totalPrecedenceViolationCount++;
                        }

                        // Check for invalid hotel stay
                        if (info.IsHotelStayAfterTrip[prevTrip.Index]) {
                            totalInvalidHotelCount++;
                        }

                        #if DEBUG
                        if (Config.DebugCheckAndLogOperations && shouldDebug) {
                            SaDebugger.GetCurrentCheckedTotal().DriverPathString += prevTrip.Index + "-";
                        }
                        #endif
                    } else {
                        /* Start of new shift */
                        // Get travel time before
                        int travelTimeBefore;
                        if (beforeHotelTrip == null) {
                            // No hotel stay before
                            travelTimeBefore = driver.HomeTravelTimeToStart(shiftFirstTrip);
                        } else {
                            // Hotel stay before
                            travelTimeBefore = info.Instance.HalfTravelTimeViaHotel(beforeHotelTrip, shiftFirstTrip);
                        }

                        // Get driving time
                        int drivingTime = driver.DrivingTime(shiftFirstTrip, prevTrip);
                        float drivingCost = driver.DrivingCost(shiftFirstTrip, prevTrip);

                        // Get travel time after and rest time
                        int travelTimeAfter, restTime;
                        if (info.IsHotelStayAfterTrip[prevTrip.Index]) {
                            // Hotel stay after
                            travelTimeAfter = info.Instance.HalfTravelTimeViaHotel(prevTrip, searchTrip);
                            restTime = searchTrip.StartTime - prevTrip.EndTime - info.Instance.TravelTimeViaHotel(prevTrip, searchTrip);

                            beforeHotelTrip = prevTrip;
                        } else {
                            // No hotel stay after
                            travelTimeAfter = info.Instance.CarTravelTime(prevTrip, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);
                            restTime = searchTrip.StartTime - prevTrip.EndTime - travelTimeAfter - driver.HomeTravelTimeToStart(searchTrip);

                            // Set new parking trip
                            parkingTrip = searchTrip;
                            beforeHotelTrip = null;
                        }

                        // Get shift length
                        int travelTime = travelTimeBefore + travelTimeAfter;
                        int shiftLength = drivingTime + travelTime;
                        totalWorkedTime += shiftLength;

                        // Get shift cost
                        float travelCost = driver.GetPayedTravelCost(travelTime);
                        float shiftCost = drivingCost + travelCost;
                        totalCostWithoutPenalty += shiftCost;

                        // Check shift length
                        int shiftLengthViolation = Math.Max(0, shiftLength - Config.MaxShiftLength);
                        if (shiftLengthViolation > 0) {
                            totalShiftLengthViolationCount++;
                            totalShiftLengthViolation += shiftLengthViolation;
                        }

                        // Check rest time
                        int restTimeViolation = Math.Max(0, Config.MinRestTime - restTime);
                        if (restTimeViolation > 0) {
                            totalRestTimeViolationCount++;
                            totalRestTimeViolation += restTimeViolation;
                        }

                        // Start new shift
                        shiftFirstTrip = searchTrip;

                        #if DEBUG
                        if (Config.DebugCheckAndLogOperations && shouldDebug) {
                            SaDebugger.GetCurrentCheckedTotal().DriverPathString += prevTrip.Index + "|";
                            SaDebugger.GetCurrentCheckedTotal().ShiftLengths.Add(shiftLength);
                            SaDebugger.GetCurrentCheckedTotal().RestTimes.Add(restTime);
                        }
                        #endif
                    }

                    prevTrip = searchTrip;
                }

                // End final shift
                int lastShiftLength = driver.ShiftLengthWithCustomPickup(shiftFirstTrip, prevTrip, parkingTrip);
                totalWorkedTime += lastShiftLength;
                totalCostWithoutPenalty += driver.ShiftCostWithCustomPickup(shiftFirstTrip, prevTrip, parkingTrip);
                int lastShiftLengthViolation = Math.Max(0, lastShiftLength - Config.MaxShiftLength);
                if (lastShiftLengthViolation > 0) {
                    totalShiftLengthViolationCount++;
                    totalShiftLengthViolation += lastShiftLengthViolation;
                }

                // Check for invalid final hotel stay
                if (info.IsHotelStayAfterTrip[prevTrip.Index]) {
                    totalInvalidHotelCount++;
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

            // Determine penalties
            double precendenceBasePenalty = totalPrecedenceViolationCount * Config.PrecendenceViolationPenalty;
            double shiftLengthBasePenalty = totalShiftLengthViolationCount * Config.ShiftLengthViolationPenalty + totalShiftLengthViolation * Config.ShiftLengthViolationPenaltyPerMin;
            double restTimeBasePenalty = totalRestTimeViolationCount * Config.RestTimeViolationPenalty + totalRestTimeViolation * Config.RestTimeViolationPenaltyPerMin;
            double contractTimeBasePenalty = totalContractTimeViolationCount * Config.ContractTimeViolationPenalty + totalContractTimeViolation * Config.ContractTimeViolationPenaltyPerMin;
            double hotelBasePenalty = totalInvalidHotelCount * Config.InvalidHotelPenalty;
            double basePenalty = precendenceBasePenalty + shiftLengthBasePenalty + restTimeBasePenalty + contractTimeBasePenalty;

            // Determine cost
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
                SaDebugger.GetCurrentCheckedTotal().Total.InvalidHotelCount = totalInvalidHotelCount;

                SaDebugger.GetCurrentCheckedTotal().Total.Cost = cost;
                SaDebugger.GetCurrentCheckedTotal().Total.CostWithoutPenalty = totalCostWithoutPenalty;
                SaDebugger.GetCurrentCheckedTotal().Total.BasePenalty = basePenalty;
                SaDebugger.GetCurrentCheckedTotal().Total.WorkedTime = totalWorkedTime;
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

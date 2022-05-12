using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class TotalCostCalculator {
        /** Get assignment cost */
        public static (double, double, double, double, DriverInfo[], PenaltyInfo) GetAssignmentCost(SaInfo info) {
            List<Trip>[] driverPaths = GetPathPerDriver(info);

            double cost = 0;
            double costWithoutPenalty = 0;
            double penalty = 0;
            double satisfaction = 0;
            DriverInfo[] driverInfos = new DriverInfo[info.Instance.AllDrivers.Length];
            PenaltyInfo penaltyInfo = new PenaltyInfo();
            for (int driverIndex = 0; driverIndex < info.Instance.AllDrivers.Length; driverIndex++) {
                List<Trip> driverPath = driverPaths[driverIndex];
                Driver driver = info.Instance.AllDrivers[driverIndex];

                (double driverCost, double driverCostWithoutPenalty, double driverPenalty, double driverSatisfaction, DriverInfo driverInfo) = GetDriverPathCost(driverPath, info.IsHotelStayAfterTrip, driver, penaltyInfo, info, false);

                cost += driverCost;
                costWithoutPenalty += driverCostWithoutPenalty;
                penalty += driverPenalty;
                satisfaction += driverSatisfaction;
                driverInfos[driverIndex] = driverInfo;
            }

            return (cost, costWithoutPenalty, penalty, satisfaction, driverInfos, penaltyInfo);
        }

        public static (double, double, double, double, DriverInfo) GetDriverPathCost(List<Trip> driverPath, bool[] isHotelStayAfterTrip, Driver driver, PenaltyInfo penaltyInfo, SaInfo info, bool shouldDebug = true) {
            DriverInfo driverInfo = new DriverInfo();
            double costWithoutPenalty = 0;

            if (driverPath.Count == 0) {
                // Empty path
                penaltyInfo.ContractTimeViolation = driver.GetMinContractTimeViolation(0);
                if (penaltyInfo.ContractTimeViolation > 0) penaltyInfo.ContractTimeViolationCount = 1;
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
                        if (!info.Instance.IsValidPrecedence(prevTrip, searchTrip)) {
                            penaltyInfo.PrecedenceViolationCount++;
                        }

                        // Check for invalid hotel stay
                        if (isHotelStayAfterTrip[prevTrip.Index]) {
                            penaltyInfo.InvalidHotelCount++;
                        }

                        #if DEBUG
                        if (Config.DebugCheckAndLogOperations && shouldDebug) {
                            SaDebugger.GetCurrentCheckedTotal().DriverPathString += prevTrip.Index + "-";
                            if (isHotelStayAfterTrip[prevTrip.Index]) SaDebugger.GetCurrentCheckedTotal().DriverPathString += "H-";
                        }
                        #endif
                    } else {
                        /* Start of new shift */
                        ShiftInfo shiftInfo = info.Instance.ShiftInfo(shiftFirstTrip, prevTrip);
                        driverInfo.ShiftCount++;

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
                        int shiftLengthWithoutTravel = shiftInfo.DrivingTime;
                        float drivingCost = driver.DrivingCost(shiftFirstTrip, prevTrip);
                        driverInfo.WorkedTime += shiftLengthWithoutTravel;

                        // Get travel time after and rest time
                        int travelTimeAfter, restTime;
                        if (isHotelStayAfterTrip[prevTrip.Index]) {
                            // Hotel stay after
                            driverInfo.HotelCount++;
                            travelTimeAfter = info.Instance.HalfTravelTimeViaHotel(prevTrip, searchTrip);
                            restTime = info.Instance.RestTimeViaHotel(prevTrip, searchTrip);
                            costWithoutPenalty += Config.HotelCosts;

                            // Check if the hotel stay isn't too long
                            if (restTime > Config.HotelMaxRestTime) {
                                penaltyInfo.InvalidHotelCount++;
                            }

                            beforeHotelTrip = prevTrip;
                        } else {
                            // No hotel stay after
                            travelTimeAfter = info.Instance.CarTravelTime(prevTrip, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);
                            restTime = info.Instance.RestTimeWithTravelTime(prevTrip, searchTrip, travelTimeAfter + driver.HomeTravelTimeToStart(searchTrip));

                            // Set new parking trip
                            parkingTrip = searchTrip;
                            beforeHotelTrip = null;
                        }

                        // Get travel time and shift length
                        int travelTime = travelTimeBefore + travelTimeAfter;
                        driverInfo.TravelTime += travelTime;
                        int shiftLengthWithTravel = shiftLengthWithoutTravel + travelTime;

                        // Get shift cost
                        float travelCost = driver.GetPaidTravelCost(travelTime);
                        float shiftCost = drivingCost + travelCost;
                        costWithoutPenalty += shiftCost;

                        // Check shift length
                        int shiftLengthViolation = Math.Max(0, shiftLengthWithoutTravel - Config.MaxShiftLengthWithoutTravel) + Math.Max(0, shiftLengthWithTravel - Config.MaxShiftLengthWithTravel);
                        if (shiftLengthViolation > 0) {
                            penaltyInfo.ShiftLengthViolationCount++;
                            penaltyInfo.ShiftLengthViolation += shiftLengthViolation;
                        }

                        // Check rest time
                        int restTimeViolation = Math.Max(0, Config.MinRestTime - restTime);
                        if (restTimeViolation > 0) {
                            penaltyInfo.RestTimeViolationCount++;
                            penaltyInfo.RestTimeViolation += restTimeViolation;
                        }

                        // Update night and weekend counts
                        if (shiftInfo.IsNightShift) driverInfo.NightShiftCount++;
                        if (shiftInfo.IsWeekendShift) driverInfo.WeekendShiftCount++;

                        // Start new shift
                        shiftFirstTrip = searchTrip;

                        #if DEBUG
                        if (Config.DebugCheckAndLogOperations && shouldDebug) {
                            SaDebugger.GetCurrentCheckedTotal().DriverPathString += prevTrip.Index + "|";
                            if (isHotelStayAfterTrip[prevTrip.Index]) SaDebugger.GetCurrentCheckedTotal().DriverPathString += "H|";
                            SaDebugger.GetCurrentCheckedTotal().ShiftLengths.Add((shiftLengthWithoutTravel, shiftLengthWithTravel));
                            SaDebugger.GetCurrentCheckedTotal().RestTimes.Add(restTime);
                        }
                        #endif
                    }

                    prevTrip = searchTrip;
                }

                /* End final shift */
                ShiftInfo lastShiftInfo = info.Instance.ShiftInfo(shiftFirstTrip, prevTrip);
                driverInfo.ShiftCount++;

                // Get travel time before
                int lastShiftTravelTimeBefore;
                if (beforeHotelTrip == null) {
                    // No hotel stay before
                    lastShiftTravelTimeBefore = driver.HomeTravelTimeToStart(shiftFirstTrip);
                } else {
                    // Hotel stay before
                    lastShiftTravelTimeBefore = info.Instance.HalfTravelTimeViaHotel(beforeHotelTrip, shiftFirstTrip);
                }

                // Get driving time
                int lastShiftLengthWithoutTravel = lastShiftInfo.DrivingTime;
                float lastShiftDrivingCost = driver.DrivingCost(shiftFirstTrip, prevTrip);
                driverInfo.WorkedTime += lastShiftLengthWithoutTravel;

                // Get travel time after and rest time
                int lastShiftTravelTimeAfter = info.Instance.CarTravelTime(prevTrip, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);

                // Get travel time and shift length
                int lastShiftTravelTime = lastShiftTravelTimeBefore + lastShiftTravelTimeAfter;
                driverInfo.TravelTime += lastShiftTravelTime;
                int lastShiftLengthWithTravel = lastShiftLengthWithoutTravel + lastShiftTravelTime;

                // Get shift cost
                float lastShiftTravelCost = driver.GetPaidTravelCost(lastShiftTravelTime);
                float lastShiftShiftCost = lastShiftDrivingCost + lastShiftTravelCost;
                costWithoutPenalty += lastShiftShiftCost;

                // Check shift length
                int lastShiftLengthViolation = Math.Max(0, lastShiftLengthWithoutTravel - Config.MaxShiftLengthWithoutTravel) + Math.Max(0, lastShiftLengthWithTravel - Config.MaxShiftLengthWithTravel);
                if (lastShiftLengthViolation > 0) {
                    penaltyInfo.ShiftLengthViolationCount++;
                    penaltyInfo.ShiftLengthViolation += lastShiftLengthViolation;
                }

                // Check for invalid final hotel stay
                if (isHotelStayAfterTrip[prevTrip.Index]) {
                    penaltyInfo.InvalidHotelCount++;
                }

                // Update night and weekend counts
                if (lastShiftInfo.IsNightShift) driverInfo.NightShiftCount++;
                if (lastShiftInfo.IsWeekendShift) driverInfo.WeekendShiftCount++;

                #if DEBUG
                if (Config.DebugCheckAndLogOperations && shouldDebug) {
                    SaDebugger.GetCurrentCheckedTotal().DriverPathString += prevTrip.Index;
                    if (isHotelStayAfterTrip[prevTrip.Index]) SaDebugger.GetCurrentCheckedTotal().DriverPathString += "-H";
                    SaDebugger.GetCurrentCheckedTotal().ShiftLengths.Add((lastShiftLengthWithoutTravel, lastShiftLengthWithTravel));
                }
                #endif

                /* Full-path checks */
                // Check driver worked time
                penaltyInfo.ContractTimeViolation = driver.GetTotalContractTimeViolation(driverInfo.WorkedTime);
                penaltyInfo.ContractTimeViolationCount = penaltyInfo.ContractTimeViolation > 0 ? 1 : 0;

                // Check driver shift count
                penaltyInfo.ShiftCountViolationAmount = Math.Max(0, driverInfo.ShiftCount - Config.DriverMaxShiftCount);
            }

            // Determine penalties
            double precendencePenalty = penaltyInfo.PrecedenceViolationCount * Config.PrecendenceViolationPenalty;
            double shiftLengthPenalty = penaltyInfo.ShiftLengthViolationCount * Config.ShiftLengthViolationPenalty + penaltyInfo.ShiftLengthViolation * Config.ShiftLengthViolationPenaltyPerMin;
            double restTimePenalty = penaltyInfo.RestTimeViolationCount * Config.RestTimeViolationPenalty + penaltyInfo.RestTimeViolation * Config.RestTimeViolationPenaltyPerMin;
            double contractTimePenalty = penaltyInfo.ContractTimeViolationCount * Config.ContractTimeViolationPenalty + penaltyInfo.ContractTimeViolation * Config.ContractTimeViolationPenaltyPerMin;
            double shiftCountPenalty = penaltyInfo.ShiftCountViolationAmount * Config.ShiftCountViolationPenaltyPerShift;
            double hotelPenalty = penaltyInfo.InvalidHotelCount * Config.InvalidHotelPenalty;
            double penalty = precendencePenalty + shiftLengthPenalty + restTimePenalty + contractTimePenalty + shiftCountPenalty + hotelPenalty;

            // Determine cost
            double cost = costWithoutPenalty + penalty;

            // Determine satisfaction
            double driverSatisfaction = driver.GetSatisfaction(driverInfo);
            double satisfaction = driverSatisfaction / info.Instance.InternalDrivers.Length;

            #if DEBUG
            if (Config.DebugCheckAndLogOperations && shouldDebug) {
                SaDebugger.GetCurrentCheckedTotal().Total.Cost = cost;
                SaDebugger.GetCurrentCheckedTotal().Total.CostWithoutPenalty = costWithoutPenalty;
                SaDebugger.GetCurrentCheckedTotal().Total.Penalty = penalty;
                SaDebugger.GetCurrentCheckedTotal().Total.DriverSatisfaction = driverSatisfaction;
                SaDebugger.GetCurrentCheckedTotal().Total.Satisfaction = satisfaction;
                SaDebugger.GetCurrentCheckedTotal().Total.DriverInfo = driverInfo;
                SaDebugger.GetCurrentCheckedTotal().Total.PenaltyInfo = penaltyInfo;
            }
            #endif

            return (cost, costWithoutPenalty, penalty, satisfaction, driverInfo);
        }

        /** Helper: get list of trips that each driver is assigned to */
        public static List<Trip>[] GetPathPerDriver(SaInfo info) {
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

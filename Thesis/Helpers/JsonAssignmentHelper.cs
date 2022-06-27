using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class JsonAssignmentHelper {
        public static void ExportAssignmentInfoJson(string folderPath, SaInfo info) {
            JObject jsonObj = new JObject {
                ["cost"] = info.TotalInfo.Stats.Cost,
                ["rawCost"] = info.TotalInfo.Stats.RawCost,
                ["robustness"] = info.TotalInfo.Stats.Robustness,
                ["penalty"] = info.TotalInfo.Stats.Penalty,
                ["drivers"] = CreateDriversJArray(info.DriverPaths, info),
                ["satisfaction"] = info.TotalInfo.Stats.SatisfactionScore.Value,
            };

            string jsonString = SortJTokenAlphabetically(jsonObj).ToString();
            string fileName = string.Format("{0}k-{1}p.json", Math.Round(info.TotalInfo.Stats.Cost / 1000), Math.Round(info.TotalInfo.Stats.SatisfactionScore.Value * 100));
            string filePath = Path.Combine(folderPath, fileName);
            File.WriteAllText(filePath, jsonString);
        }

        static JArray CreateDriversJArray(List<Trip>[] driverPaths, SaInfo info) {
            JArray driversJArray = new JArray();
            for (int internalDriverIndex = 0; internalDriverIndex < info.Instance.InternalDrivers.Length; internalDriverIndex++) {
                InternalDriver internalDriver = info.Instance.InternalDrivers[internalDriverIndex];
                List<Trip> driverPath = driverPaths[internalDriver.AllDriversIndex];
                driversJArray.Add(CreateDriverJObject(driverPath, internalDriver, internalDriver.GetInternalDriverName(false), internalDriver.GetInternalDriverName(true), info));
            }
            for (int externalDriverTypeIndex = 0; externalDriverTypeIndex < info.Instance.ExternalDriverTypes.Length; externalDriverTypeIndex++) {
                ExternalDriver[] externalDriversInType = info.Instance.ExternalDriversByType[externalDriverTypeIndex];
                int usedExternalDriverInTypeIndex = 0;
                for (int externalDriverInTypeIndex = 0; externalDriverInTypeIndex < externalDriversInType.Length; externalDriverInTypeIndex++) {
                    ExternalDriver externalDriver = externalDriversInType[externalDriverInTypeIndex];
                    List<Trip> driverPath = driverPaths[externalDriver.AllDriversIndex];
                    if (driverPath.Count == 0) continue;

                    string externalDriverName = externalDriver.GetExternalDriverName(usedExternalDriverInTypeIndex);
                    driversJArray.Add(CreateDriverJObject(driverPath, externalDriver, externalDriverName, externalDriverName, info));
                    usedExternalDriverInTypeIndex++;
                }
            }
            return driversJArray;
        }

        static JObject CreateDriverJObject(List<Trip> driverPath, Driver driver, string driverName, string driverRealName, SaInfo info) {
            SaDriverInfo driverInfo = info.DriverInfos[driver.AllDriversIndex];

            JObject driverJObject = new JObject {
                ["driverName"] = driverName,
                ["realDriverName"] = driverRealName,
                ["isInternal"] = driver is InternalDriver,
                ["isInternational"] = driver.IsInternational,
                ["shifts"] = CreateDriverShiftsJArray(driver, driverPath, info),
                ["stats"] = CreateDriverStatsJObject(driver, driverInfo),
                ["info"] = CreateDriverInfoJObject(driverInfo),
            };
            if (driver is InternalDriver internalDriver) {
                driverJObject["contractTime"] = internalDriver.ContractTime;
            }
            return driverJObject;
        }

        static JObject CreateDriverStatsJObject(Driver driver, SaDriverInfo driverInfo) {
            JObject driverStatsJObject = new JObject {
                ["cost"] = driverInfo.Stats.Cost,
                ["rawCost"] = driverInfo.Stats.RawCost,
                ["robustness"] = driverInfo.Stats.Robustness,
                ["penalty"] = driverInfo.Stats.Penalty,
                ["driverSatisfaction"] = driverInfo.Stats.DriverSatisfaction,
            };
            if (driver is InternalDriver internalDriver) {
                driverStatsJObject["driverSatisfactionCriteria"] = CreateDriverSatisfactionCriteriaJObject(internalDriver, driverInfo);
            }
            return driverStatsJObject;
        }

        static JObject CreateDriverInfoJObject(SaDriverInfo driverInfo) {
            return new JObject {
                ["workedTime"] = driverInfo.WorkedTime,
                ["shiftCount"] = driverInfo.ShiftCount,
                ["hotelCount"] = driverInfo.HotelCount,
                ["nightShiftCountByCompanyRules"] = driverInfo.NightShiftCountByCompanyRules,
                ["weekendShiftCountByCompanyRules"] = driverInfo.WeekendShiftCountByCompanyRules,
                ["travelTime"] = driverInfo.TravelTime,
                ["singleFreeDayCount"] = driverInfo.SingleFreeDayCount,
                ["doubleFreeDayCount"] = driverInfo.DoubleFreeDayCount,
                ["duplicateRouteCount"] = driverInfo.SharedRouteCounts.Sum(),
            };
        }

        static JObject CreateDriverSatisfactionCriteriaJObject(InternalDriver internalDriver, SaDriverInfo driverInfo) {
            Dictionary<string, double> satisfactionPerCriterion = SatisfactionCalculator.GetDriverSatisfactionPerCriterion(internalDriver, driverInfo);

            JObject driverSatisfactionCriteriaJObject = new JObject();
            foreach (KeyValuePair<string, double> criterionKvp in satisfactionPerCriterion) {
                driverSatisfactionCriteriaJObject[criterionKvp.Key] = criterionKvp.Value;
            }
            return driverSatisfactionCriteriaJObject;
        }

        static JArray CreateDriverShiftsJArray(Driver driver, List<Trip> driverPath, SaInfo info) {
            JArray shiftsJArray = new JArray();
            if (driverPath.Count == 0) return shiftsJArray;

            Func<Trip, bool> isHotelAfterTripFunc = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];

            JArray shiftPathJArray = new JArray();
            Trip shiftFirstTrip = null;
            Trip prevTrip = null;
            Trip parkingTrip = driverPath[0];
            Trip tripBeforeHotelBeforeShift = null;
            float shiftRobustness = 0;
            for (int i = 0; i < driverPath.Count; i++) {
                Trip searchTrip = driverPath[i];

                if (prevTrip == null) {
                    // First activity
                    shiftFirstTrip = searchTrip;
                    AddTravelFromHomeToPath(searchTrip, driver, shiftPathJArray);
                } else {
                    if (info.Instance.AreSameShift(prevTrip, searchTrip)) {
                        // Activity in same shift
                        shiftRobustness += AddTravelAndWaitBetweenTripsToPath(prevTrip, searchTrip, shiftPathJArray, info);
                    } else {
                        // Activity in new shift
                        if (info.IsHotelStayAfterTrip[prevTrip.Index]) {
                            AddHotelStayAfterToPath(prevTrip, searchTrip, shiftPathJArray, info);
                            AddShiftToDriverShifts(shiftFirstTrip, prevTrip, parkingTrip, tripBeforeHotelBeforeShift, searchTrip, shiftRobustness, shiftPathJArray, shiftsJArray, driver, info);
                            tripBeforeHotelBeforeShift = prevTrip;
                        } else {
                            AddTravelToHomeToPath(prevTrip, parkingTrip, driver, shiftPathJArray, info);
                            AddRestToPath(prevTrip, searchTrip, parkingTrip, driver, shiftPathJArray, info);
                            AddShiftToDriverShifts(shiftFirstTrip, prevTrip, parkingTrip, tripBeforeHotelBeforeShift, null, shiftRobustness, shiftPathJArray, shiftsJArray, driver, info);
                            tripBeforeHotelBeforeShift = null;
                        }

                        // Start new shift
                        shiftPathJArray = new JArray();
                        shiftFirstTrip = searchTrip;
                        parkingTrip = searchTrip;

                        if (info.IsHotelStayAfterTrip[prevTrip.Index]) {
                            AddHotelStayBeforeToPath(prevTrip, searchTrip, shiftPathJArray, info);
                        } else {
                            AddTravelFromHomeToPath(searchTrip, driver, shiftPathJArray);
                        }
                    }
                }

                AddTripToPath(searchTrip, shiftPathJArray, info);

                prevTrip = searchTrip;
            }
            AddTravelToHomeToPath(prevTrip, parkingTrip, driver, shiftPathJArray, info);
            AddShiftToDriverShifts(shiftFirstTrip, prevTrip, parkingTrip, tripBeforeHotelBeforeShift, null, shiftRobustness, shiftPathJArray, shiftsJArray, driver, info);

            return shiftsJArray;
        }

        static void AddShiftToDriverShifts(Trip shiftFirstTrip, Trip shiftLastTrip, Trip parkingTrip, Trip tripBeforeHotel, Trip tripAfterHotel, float shiftRobustness, JArray shiftPathJArray, JArray shiftsJArray, Driver driver, SaInfo info) {
            ShiftInfo shiftInfo = info.Instance.ShiftInfo(shiftFirstTrip, shiftLastTrip);

            int administrativeDrivingTime = shiftInfo.AdministrativeDrivingTimeByDriverType[driver.SalarySettings.DriverTypeIndex];
            int startTime = shiftFirstTrip.StartTime;
            int administrativeEndTime = startTime + administrativeDrivingTime;

            float drivingCost = driver.DrivingCost(shiftFirstTrip, shiftLastTrip);

            (int travelTimeBefore, int travelDistanceBefore) = RangeCostTripProcessor.GetTravelInfoBefore(tripBeforeHotel, shiftFirstTrip, driver, info.Instance);
            (int travelTimeAfter, int travelDistanceAfter) = RangeCostTripProcessor.GetTravelInfoAfter(shiftLastTrip, tripAfterHotel, parkingTrip, tripAfterHotel != null, driver, info.Instance);

            int travelTime = travelTimeBefore + travelTimeAfter;
            int travelDistance = travelDistanceBefore + travelDistanceAfter;
            float travelCost = driver.GetPaidTravelCost(travelTime, travelDistance);
            float hotelCost = tripAfterHotel == null ? 0 : SalaryConfig.HotelCosts;
            float cost = drivingCost + travelCost + hotelCost + shiftRobustness;

            // Salary rates breakdown
            JArray salaryRates = new JArray();
            List<ComputedSalaryRateBlock> computeSalaryRateBlocks = shiftInfo.ComputeSalaryRateBlocksByType[driver.SalarySettings.DriverTypeIndex];
            for (int salaryRateIndex = 0; salaryRateIndex < computeSalaryRateBlocks.Count; salaryRateIndex++) {
                ComputedSalaryRateBlock computeSalaryRateBlock = computeSalaryRateBlocks[salaryRateIndex];
                JObject salaryRateJObject = new JObject() {
                    ["rateStartTime"] = computeSalaryRateBlock.RateStartTime,
                    ["rateEndTime"] = computeSalaryRateBlock.RateEndTime,
                    ["salaryStartTime"] = computeSalaryRateBlock.SalaryStartTime,
                    ["salaryEndTime"] = computeSalaryRateBlock.SalaryEndTime,
                    ["salaryDuration"] = computeSalaryRateBlock.SalaryDuration,
                    ["hourlySalaryRate"] = computeSalaryRateBlock.SalaryRate * MiscConfig.HourLength,
                    ["usesContinuingRate"] = computeSalaryRateBlock.UsesContinuingRate,
                    ["drivingCostInRange"] = computeSalaryRateBlock.DrivingCostInRate,
                };
                salaryRates.Add(salaryRateJObject);
            }

            JObject shiftJObject = new JObject() {
                ["tripPath"] = shiftPathJArray,
                ["drivingTime"] = shiftInfo.DrivingTime,
                ["administrativeDrivingTime"] = administrativeDrivingTime,
                ["startTime"] = startTime,
                ["administrativeEndTime"] = administrativeEndTime,
                ["drivingCost"] = drivingCost,
                ["travelTimeBefore"] = travelTimeBefore,
                ["travelDistanceBefore"] = travelDistanceBefore,
                ["travelTimeAfter"] = travelTimeAfter,
                ["travelDistanceAfter"] = travelDistanceAfter,
                ["travelTime"] = travelDistanceAfter,
                ["travelDistance"] = travelDistanceAfter,
                ["cost"] = cost,
                ["travelCost"] = travelCost,
                ["hotelCost"] = hotelCost,
                ["robustness"] = shiftRobustness,
                ["salaryRates"] = salaryRates,
            };
            shiftsJArray.Add(shiftJObject);
        }

        static void AddTripToPath(Trip trip, JArray driverPathJArray, SaInfo info) {
            JObject tripPathItem = new JObject {
                ["type"] = "trip",
                ["tripIndex"] = trip.Index,
                ["startTime"] = trip.StartTime,
                ["endTime"] = trip.EndTime,
                ["dutyName"] = trip.DutyName,
                ["activityName"] = trip.ActivityName,
                ["startStationName"] = info.Instance.StationNames[trip.StartStationAddressIndex],
                ["endStationName"] = info.Instance.StationNames[trip.EndStationAddressIndex],
            };
            driverPathJArray.Add(tripPathItem);
        }

        static float AddTravelAndWaitBetweenTripsToPath(Trip trip1, Trip trip2, JArray driverPathJArray, SaInfo info) {
            int carTravelTime = info.Instance.PlannedCarTravelTime(trip1, trip2);
            if (carTravelTime > 0) {
                JObject travelBetweenPathItem = new JObject {
                    ["type"] = "travelBetween",
                    ["startTime"] = trip1.EndTime,
                    ["endTime"] = trip1.EndTime + carTravelTime,
                };
                driverPathJArray.Add(travelBetweenPathItem);
            }

            int waitingTime = trip2.StartTime - trip1.EndTime - carTravelTime;
            float robustness = info.Instance.TripSuccessionRobustness(trip1, trip2);
            string typeStr = waitingTime >= 0 ? "wait" : "overlapError";
            JObject waitBetweenPathItem = new JObject {
                ["type"] = typeStr,
                ["startTime"] = trip1.EndTime + carTravelTime,
                ["endTime"] = trip2.StartTime,
                ["robustness"] = robustness,
            };
            driverPathJArray.Add(waitBetweenPathItem);

            return robustness;
        }

        static void AddRestToPath(Trip tripBeforeRest, Trip tripAfterRest, Trip parkingTrip, Driver driver, JArray driverPathJArray, SaInfo info) {
            int travelTimeAfterShift = info.Instance.PlannedCarTravelTime(tripBeforeRest, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);
            int travelTimeBeforeShift = driver.HomeTravelTimeToStart(tripAfterRest);

            JObject restPathItem = new JObject {
                ["type"] = "rest",
                ["startTime"] = tripBeforeRest.EndTime + travelTimeAfterShift,
                ["endTime"] = tripAfterRest.StartTime - travelTimeBeforeShift,
            };
            driverPathJArray.Add(restPathItem);
        }

        static void AddTravelToHomeToPath(Trip tripBeforeHome, Trip parkingTrip, Driver driver, JArray driverPathJArray, SaInfo info) {
            int travelTimeAfterShift = info.Instance.PlannedCarTravelTime(tripBeforeHome, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);

            JObject travelAfterPathItem = new JObject {
                ["type"] = "travelAfter",
                ["startTime"] = tripBeforeHome.EndTime,
                ["endTime"] = tripBeforeHome.EndTime + travelTimeAfterShift,
            };
            driverPathJArray.Add(travelAfterPathItem);
        }

        static void AddTravelFromHomeToPath(Trip tripAfterHome, Driver driver, JArray driverPathJArray) {
            int travelTimeBeforeShift = driver.HomeTravelTimeToStart(tripAfterHome);

            JObject travelBeforePathItem = new JObject {
                ["type"] = "travelBefore",
                ["startTime"] = tripAfterHome.StartTime - travelTimeBeforeShift,
                ["endTime"] = tripAfterHome.StartTime,
            };
            driverPathJArray.Add(travelBeforePathItem);
        }

        static void AddHotelStayAfterToPath(Trip tripBeforeHotel, Trip tripAfterHotel, JArray driverPathJArray, SaInfo info) {
            int halfTravelTimeViaHotel = info.Instance.PlannedHalfTravelTimeViaHotel(tripBeforeHotel, tripAfterHotel);

            JObject travelBeforeHotelPathItem = new JObject {
                ["type"] = "travelBeforeHotel",
                ["startTime"] = tripBeforeHotel.EndTime,
                ["endTime"] = tripBeforeHotel.EndTime + halfTravelTimeViaHotel,
            };
            driverPathJArray.Add(travelBeforeHotelPathItem);

            JObject hotelPathItem = new JObject {
                ["type"] = "hotel",
                ["startTime"] = tripBeforeHotel.EndTime + halfTravelTimeViaHotel,
                ["endTime"] = tripAfterHotel.StartTime - halfTravelTimeViaHotel,
            };
            driverPathJArray.Add(hotelPathItem);
        }

        static void AddHotelStayBeforeToPath(Trip tripBeforeHotel, Trip tripAfterHotel, JArray driverPathJArray, SaInfo info) {
            int halfTravelTimeViaHotel = info.Instance.PlannedHalfTravelTimeViaHotel(tripBeforeHotel, tripAfterHotel);
            JObject travelAfterHotelPathItem = new JObject {
                ["type"] = "travelAfterHotel",
                ["startTime"] = tripAfterHotel.StartTime - halfTravelTimeViaHotel,
                ["endTime"] = tripAfterHotel.StartTime,
            };
            driverPathJArray.Add(travelAfterHotelPathItem);
        }

        static JToken SortJTokenAlphabetically(JToken token) {
            if (token is JObject jObject) {
                JObject processedJObject = new JObject();
                foreach (JProperty property in jObject.Properties().ToList().OrderBy(p => p.Name)) {
                    processedJObject.Add(property.Name, SortJTokenAlphabetically(property.Value));
                }
                return processedJObject;
            }
            if (token is JArray jArray) {
                JArray processedJArray = new JArray();
                foreach (var element in jArray) {
                    processedJArray.Add(SortJTokenAlphabetically(element));
                }
                return processedJArray;
            }
            return token;
        }
    }
}

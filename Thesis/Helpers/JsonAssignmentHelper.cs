﻿using Newtonsoft.Json.Linq;
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

        static JArray CreateDriversJArray(List<Activity>[] driverPaths, SaInfo info) {
            JArray driversJArray = new JArray();
            for (int internalDriverIndex = 0; internalDriverIndex < info.Instance.InternalDrivers.Length; internalDriverIndex++) {
                InternalDriver internalDriver = info.Instance.InternalDrivers[internalDriverIndex];
                List<Activity> driverPath = driverPaths[internalDriver.AllDriversIndex];
                driversJArray.Add(CreateDriverJObject(driverPath, internalDriver, internalDriver.GetInternalDriverName(false), internalDriver.GetInternalDriverName(true), info));
            }
            for (int externalDriverTypeIndex = 0; externalDriverTypeIndex < info.Instance.ExternalDriverTypes.Length; externalDriverTypeIndex++) {
                ExternalDriver[] externalDriversInType = info.Instance.ExternalDriversByType[externalDriverTypeIndex];
                int usedExternalDriverInTypeIndex = 0;
                for (int externalDriverInTypeIndex = 0; externalDriverInTypeIndex < externalDriversInType.Length; externalDriverInTypeIndex++) {
                    ExternalDriver externalDriver = externalDriversInType[externalDriverInTypeIndex];
                    List<Activity> driverPath = driverPaths[externalDriver.AllDriversIndex];
                    if (driverPath.Count == 0) continue;

                    string externalDriverName = externalDriver.GetExternalDriverName(usedExternalDriverInTypeIndex);
                    driversJArray.Add(CreateDriverJObject(driverPath, externalDriver, externalDriverName, externalDriverName, info));
                    usedExternalDriverInTypeIndex++;
                }
            }
            return driversJArray;
        }

        static JObject CreateDriverJObject(List<Activity> driverPath, Driver driver, string driverName, string driverRealName, SaInfo info) {
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

        static JArray CreateDriverShiftsJArray(Driver driver, List<Activity> driverPath, SaInfo info) {
            JArray shiftsJArray = new JArray();
            if (driverPath.Count == 0) return shiftsJArray;

            JArray shiftPathJArray = new JArray();
            Activity shiftFirstActivity = null;
            Activity prevActivity = null;
            Activity parkingActivity = driverPath[0];
            Activity activityBeforeHotelBeforeShift = null;
            float shiftRobustness = 0;
            for (int i = 0; i < driverPath.Count; i++) {
                Activity searchActivity = driverPath[i];

                if (prevActivity == null) {
                    // First activity
                    shiftFirstActivity = searchActivity;
                    AddTravelFromHomeToPath(searchActivity, driver, shiftPathJArray, info);
                } else {
                    if (info.Instance.AreSameShift(prevActivity, searchActivity)) {
                        // Activity in same shift
                        shiftRobustness += AddTravelAndWaitBetweenActivitiesToPath(prevActivity, searchActivity, shiftPathJArray, info);
                    } else {
                        // Activity in new shift
                        if (info.IsHotelStayAfterActivity[prevActivity.Index]) {
                            AddHotelStayAfterToPath(prevActivity, searchActivity, shiftPathJArray, info);
                            AddShiftToDriverShifts(shiftFirstActivity, prevActivity, parkingActivity, activityBeforeHotelBeforeShift, searchActivity, shiftRobustness, shiftPathJArray, shiftsJArray, driver, info);
                            activityBeforeHotelBeforeShift = prevActivity;
                        } else {
                            AddTravelToHomeToPath(prevActivity, parkingActivity, driver, shiftPathJArray, info);
                            AddRestToPath(prevActivity, searchActivity, parkingActivity, driver, shiftPathJArray, info);
                            AddShiftToDriverShifts(shiftFirstActivity, prevActivity, parkingActivity, activityBeforeHotelBeforeShift, null, shiftRobustness, shiftPathJArray, shiftsJArray, driver, info);
                            activityBeforeHotelBeforeShift = null;
                            parkingActivity = searchActivity;
                        }

                        // Start new shift
                        shiftPathJArray = new JArray();
                        shiftFirstActivity = searchActivity;

                        if (info.IsHotelStayAfterActivity[prevActivity.Index]) {
                            AddHotelStayBeforeToPath(prevActivity, searchActivity, shiftPathJArray, info);
                        } else {
                            AddTravelFromHomeToPath(searchActivity, driver, shiftPathJArray, info);
                        }
                    }
                }

                AddActivityToPath(searchActivity, shiftPathJArray, info);

                prevActivity = searchActivity;
            }
            AddTravelToHomeToPath(prevActivity, parkingActivity, driver, shiftPathJArray, info);
            AddShiftToDriverShifts(shiftFirstActivity, prevActivity, parkingActivity, activityBeforeHotelBeforeShift, null, shiftRobustness, shiftPathJArray, shiftsJArray, driver, info);

            return shiftsJArray;
        }

        static void AddShiftToDriverShifts(Activity shiftFirstActivity, Activity shiftLastActivity, Activity parkingActivity, Activity activityBeforeHotel, Activity activityAfterHotel, float shiftRobustness, JArray shiftPathJArray, JArray shiftsJArray, Driver driver, SaInfo info) {
            ShiftInfo shiftInfo = info.Instance.ShiftInfo(shiftFirstActivity, shiftLastActivity);

            int administrativeDrivingTime = shiftInfo.AdministrativeDrivingTimeByDriverType[driver.SalarySettings.DriverTypeIndex];
            int startTime = shiftFirstActivity.StartTime;
            int administrativeEndTime = startTime + administrativeDrivingTime;

            float drivingCost = driver.DrivingCost(shiftFirstActivity, shiftLastActivity);

            (int travelTimeBefore, int travelDistanceBefore) = RangeCostActivityProcessor.GetTravelInfoBefore(activityBeforeHotel, shiftFirstActivity, driver, info.Instance);
            (int travelTimeAfter, int travelDistanceAfter) = RangeCostActivityProcessor.GetTravelInfoAfter(shiftLastActivity, activityAfterHotel, parkingActivity, activityAfterHotel != null, driver, info.Instance);

            int travelTime = travelTimeBefore + travelTimeAfter;
            int travelDistance = travelDistanceBefore + travelDistanceAfter;
            float travelCost = driver.GetPaidTravelCost(travelTime, travelDistance);
            float hotelCost = activityAfterHotel == null ? 0 : SalaryConfig.HotelCosts;
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
                ["activityPath"] = shiftPathJArray,
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

        static void AddActivityToPath(Activity activity, JArray driverPathJArray, SaInfo info) {
            JObject activityPathItem = new JObject {
                ["type"] = "activity",
                ["activityIndex"] = activity.Index,
                ["startTime"] = activity.StartTime,
                ["endTime"] = activity.EndTime,
                ["dutyName"] = activity.DutyName,
                ["activityName"] = activity.ActivityName,
                ["projectName"] = activity.ProjectName,
                ["trainNumber"] = activity.TrainNumber,
                ["startStationName"] = info.Instance.StationNames[activity.StartStationAddressIndex],
                ["endStationName"] = info.Instance.StationNames[activity.EndStationAddressIndex],
            };
            driverPathJArray.Add(activityPathItem);
        }

        static float AddTravelAndWaitBetweenActivitiesToPath(Activity activity1, Activity activity2, JArray driverPathJArray, SaInfo info) {
            int carTravelTime = info.Instance.PlannedCarTravelTime(activity1, activity2);
            if (carTravelTime > 0) {
                JObject travelBetweenPathItem = new JObject {
                    ["type"] = "travelBetween",
                    ["startTime"] = activity1.EndTime,
                    ["endTime"] = activity1.EndTime + carTravelTime,
                    ["startStationName"] = info.Instance.StationNames[activity1.EndStationAddressIndex],
                    ["endStationName"] = info.Instance.StationNames[activity2.StartStationAddressIndex],
                };
                driverPathJArray.Add(travelBetweenPathItem);
            }

            int waitingTime = activity2.StartTime - activity1.EndTime - carTravelTime;
            float robustness = info.Instance.ActivitySuccessionRobustness(activity1, activity2);
            string typeStr = waitingTime >= 0 ? "wait" : "overlapError";
            JObject waitBetweenPathItem = new JObject {
                ["type"] = typeStr,
                ["startTime"] = activity1.EndTime + carTravelTime,
                ["endTime"] = activity2.StartTime,
                ["robustness"] = robustness,
            };
            driverPathJArray.Add(waitBetweenPathItem);

            return robustness;
        }

        static void AddRestToPath(Activity activityBeforeRest, Activity activityAfterRest, Activity parkingActivity, Driver driver, JArray driverPathJArray, SaInfo info) {
            int travelTimeAfterShift = info.Instance.PlannedCarTravelTime(activityBeforeRest, parkingActivity) + driver.HomeTravelTimeToStart(parkingActivity);
            int travelTimeBeforeShift = driver.HomeTravelTimeToStart(activityAfterRest);

            JObject restPathItem = new JObject {
                ["type"] = "rest",
                ["startTime"] = activityBeforeRest.EndTime + travelTimeAfterShift,
                ["endTime"] = activityAfterRest.StartTime - travelTimeBeforeShift,
            };
            driverPathJArray.Add(restPathItem);
        }

        static void AddTravelToHomeToPath(Activity activityBeforeHome, Activity parkingActivity, Driver driver, JArray driverPathJArray, SaInfo info) {
            int travelTimeToCar = info.Instance.PlannedCarTravelTime(activityBeforeHome, parkingActivity);
            int travelTimeFromCarToHome = driver.HomeTravelTimeToStart(parkingActivity);

            JObject travelToCarPathItem = new JObject {
                ["type"] = "travelToCar",
                ["startTime"] = activityBeforeHome.EndTime,
                ["endTime"] = activityBeforeHome.EndTime + travelTimeToCar,
                ["startStationName"] = info.Instance.StationNames[activityBeforeHome.EndStationAddressIndex],
                ["endStationName"] = info.Instance.StationNames[parkingActivity.StartStationAddressIndex],
            };
            driverPathJArray.Add(travelToCarPathItem);

            JObject travelToHomePathItem = new JObject {
                ["type"] = "travelToHome",
                ["startTime"] = activityBeforeHome.EndTime + travelTimeToCar,
                ["endTime"] = activityBeforeHome.EndTime + travelTimeToCar + travelTimeFromCarToHome,
                ["startStationName"] = info.Instance.StationNames[parkingActivity.StartStationAddressIndex],
            };
            driverPathJArray.Add(travelToHomePathItem);
        }

        static void AddTravelFromHomeToPath(Activity activityAfterHome, Driver driver, JArray driverPathJArray, SaInfo info) {
            int travelTimeBeforeShift = driver.HomeTravelTimeToStart(activityAfterHome);

            JObject travelBeforePathItem = new JObject {
                ["type"] = "travelFromHome",
                ["startTime"] = activityAfterHome.StartTime - travelTimeBeforeShift,
                ["endTime"] = activityAfterHome.StartTime,
                ["endStationName"] = info.Instance.StationNames[activityAfterHome.StartStationAddressIndex],
            };
            driverPathJArray.Add(travelBeforePathItem);
        }

        static void AddHotelStayAfterToPath(Activity activityBeforeHotel, Activity activityAfterHotel, JArray driverPathJArray, SaInfo info) {
            int halfTravelTimeViaHotel = info.Instance.PlannedHalfTravelTimeViaHotel(activityBeforeHotel, activityAfterHotel);

            JObject travelBeforeHotelPathItem = new JObject {
                ["type"] = "travelToHotel",
                ["startTime"] = activityBeforeHotel.EndTime,
                ["endTime"] = activityBeforeHotel.EndTime + halfTravelTimeViaHotel,
                ["startStationName"] = info.Instance.StationNames[activityBeforeHotel.EndStationAddressIndex],
            };
            driverPathJArray.Add(travelBeforeHotelPathItem);

            JObject hotelPathItem = new JObject {
                ["type"] = "hotel",
                ["startTime"] = activityBeforeHotel.EndTime + halfTravelTimeViaHotel,
                ["endTime"] = activityAfterHotel.StartTime - halfTravelTimeViaHotel,
            };
            driverPathJArray.Add(hotelPathItem);
        }

        static void AddHotelStayBeforeToPath(Activity activityBeforeHotel, Activity activityAfterHotel, JArray driverPathJArray, SaInfo info) {
            int halfTravelTimeViaHotel = info.Instance.PlannedHalfTravelTimeViaHotel(activityBeforeHotel, activityAfterHotel);
            JObject travelAfterHotelPathItem = new JObject {
                ["type"] = "travelFromHotel",
                ["startTime"] = activityAfterHotel.StartTime - halfTravelTimeViaHotel,
                ["endTime"] = activityAfterHotel.StartTime,
                ["endStationName"] = info.Instance.StationNames[activityAfterHotel.StartStationAddressIndex],
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

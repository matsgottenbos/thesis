using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class JsonOutputHelper {
        public static void ExportRunListJsonFile() {
            string[] runFolderPaths = Directory.GetDirectories(DevConfig.OutputFolder);
            JArray runsByStartDateJArray = new JArray();
            for (int i = 0; i < runFolderPaths.Length; i++) {
                string runFolderPath = runFolderPaths[i];
                string runJsonPath = Path.Combine(runFolderPath, "run.json");
                if (!File.Exists(runJsonPath)) continue;

                string runJsonStr = File.ReadAllText(runJsonPath);
                JObject runJObject = JObject.Parse(runJsonStr);
                runJObject["folderName"] = new DirectoryInfo(runFolderPath).Name;

                string runStartDate = runJObject["dataStartDate"].ToString();
                string runEndDate = runJObject["dataEndDate"].ToString();
                JObject dateRunListJObject = runsByStartDateJArray.FirstOrDefault(searchDateRunList => searchDateRunList["dataStartDate"].ToString() == runStartDate && searchDateRunList["dataEndDate"].ToString() == runEndDate) as JObject;
                if (dateRunListJObject == null) {
                    runsByStartDateJArray.Add(new JObject() {
                        ["dataStartDate"] = runStartDate,
                        ["dataEndDate"] = runEndDate,
                        ["runs"] = new JArray() {
                            runJObject
                        },
                    });
                } else {
                    (dateRunListJObject["runs"] as JArray).Add(runJObject);
                }
            }

            // Sort list and nested lists
            runsByStartDateJArray = new JArray(runsByStartDateJArray.OrderByDescending(dateRunList => dateRunList["dataStartDate"]));
            for (int i = 0; i < runsByStartDateJArray.Count; i++) {
                runsByStartDateJArray[i]["runs"] = new JArray(runsByStartDateJArray[i]["runs"].OrderByDescending(dateRunList => dateRunList["runCompletionDate"]));
            }

            JObject runsListJObject = new JObject() {
                ["runsByStartDate"] = runsByStartDateJArray,
            };

            string filePath = Path.Combine(DevConfig.OutputFolder, "runList.json");
            ExportJsonFile(runsListJObject, filePath);
        }

        public static void ExportRunJsonFiles(string folderPath, List<SaInfo> paretoFront) {
            // Log pareto front solutions to separate JSON files
            JArray schedulesJArray = new JArray();
            for (int i = 0; i < paretoFront.Count; i++) {
                SaInfo paretoPoint = paretoFront[i];
                paretoPoint.ProcessDriverPaths();
                TotalCostCalculator.ProcessAssignmentCost(paretoPoint);

                // Add schedule to array
                JObject scheduleInfoJObject = GetBasicAssignmentInfoJson(paretoPoint);
                scheduleInfoJObject["fileName"] = GetScheduleJsonFilename(paretoPoint);
                schedulesJArray.Add(scheduleInfoJObject);

                // Export separate JSON file for schedule
                ExportAssignmentInfoJson(folderPath, paretoPoint);
            }

            // Export JSON file with basic run info
            JObject runJObject = new JObject {
                ["iterationCount"] = AppConfig.SaIterationCount,
                ["dataStartDate"] = AppConfig.PlanningStartDate.ToString("yyyy/MM/dd HH:mm"),
                ["dataEndDate"] = AppConfig.PlanningNextDate.ToString("yyyy/MM/dd HH:mm"),
                ["runCompletionDate"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm"),
                ["schedules"] = schedulesJArray,
            };
            string filePath = Path.Combine(folderPath, "run.json");
            ExportJsonFile(runJObject, filePath);
        }

        public static void ExportAssignmentInfoJson(string folderPath, SaInfo info) {
            JObject assignmentJObject = GetBasicAssignmentInfoJson(info);
            assignmentJObject["drivers"] = CreateDriversJArray(info.DriverPaths, info);

            string fileName = GetScheduleJsonFilename(info) + ".json";
            string filePath = Path.Combine(folderPath, fileName);
            ExportJsonFile(assignmentJObject, filePath);
        }

        static string GetScheduleJsonFilename(SaInfo info) {
            return string.Format("{0}k-{1}p", Math.Round(info.TotalInfo.Stats.Cost / 1000), Math.Round(info.TotalInfo.Stats.SatisfactionScore.Value * 100));
        }

        static JObject GetBasicAssignmentInfoJson(SaInfo info) {
            return new JObject {
                ["cost"] = info.TotalInfo.Stats.Cost,
                ["rawCost"] = info.TotalInfo.Stats.RawCost,
                ["robustness"] = info.TotalInfo.Stats.Robustness,
                ["penalty"] = info.TotalInfo.Stats.Penalty,
                ["satisfaction"] = info.TotalInfo.Stats.SatisfactionScore.Value,
            };
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
                driverJObject["isOptional"] = internalDriver.IsOptional;
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
            Dictionary<string, double> satisfactionPerCriterion = internalDriver.GetSatisfactionPerCriterion(driverInfo);

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
            Activity beforeHotelActivity = null;
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
                            AddShiftToDriverShifts(shiftFirstActivity, prevActivity, parkingActivity, beforeHotelActivity, searchActivity, shiftRobustness, shiftPathJArray, shiftsJArray, driver, info);
                            beforeHotelActivity = prevActivity;
                        } else {
                            AddTravelToHomeToPath(prevActivity, parkingActivity, driver, shiftPathJArray, info);
                            AddRestToPath(prevActivity, searchActivity, parkingActivity, driver, shiftPathJArray, info);
                            AddShiftToDriverShifts(shiftFirstActivity, prevActivity, parkingActivity, beforeHotelActivity, null, shiftRobustness, shiftPathJArray, shiftsJArray, driver, info);
                            beforeHotelActivity = null;
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
            AddShiftToDriverShifts(shiftFirstActivity, prevActivity, parkingActivity, beforeHotelActivity, null, shiftRobustness, shiftPathJArray, shiftsJArray, driver, info);

            return shiftsJArray;
        }

        static void AddShiftToDriverShifts(Activity shiftFirstActivity, Activity shiftLastActivity, Activity parkingActivity, Activity beforeHotelActivity, Activity afterHotelActivity, float shiftRobustness, JArray shiftPathJArray, JArray shiftsJArray, Driver driver, SaInfo info) {
            (_, DriverTypeMainShiftInfo driverTypeMainShiftInfo, int realMainShiftLength, int fullShiftLength, _, _, _, float fullShiftCost) = RangeCostActivityProcessor.GetShiftDetails(shiftFirstActivity, shiftLastActivity, parkingActivity, beforeHotelActivity, afterHotelActivity, driver, info, info.Instance);

            (int sharedCarTravelTimeBefore, int sharedCarTravelDistanceBefore, int ownCarTravelTimeBefore, int ownCarTravelDistanceBefore) = RangeCostActivityProcessor.GetTravelInfoBefore(beforeHotelActivity, shiftFirstActivity, driver, info.Instance);
            (int sharedCarTravelTimeAfter, int sharedCarTravelDistanceAfter, int ownCarTravelTimeAfter, int ownCarTravelDistanceAfter) = RangeCostActivityProcessor.GetTravelInfoAfter(shiftLastActivity, afterHotelActivity, parkingActivity, driver, info.Instance);

            var temp = info.Instance.ExpectedCarTravelTime(shiftLastActivity, parkingActivity);

            int mainShiftStartTime = shiftFirstActivity.StartTime - sharedCarTravelTimeBefore;
            int realMainShiftEndTime = shiftLastActivity.EndTime + sharedCarTravelTimeAfter;

            int sharedCarTravelDistance = sharedCarTravelDistanceBefore + sharedCarTravelDistanceAfter;
            int ownCarTravelTime = ownCarTravelTimeBefore + ownCarTravelTimeAfter;
            int ownCarTravelDistance = ownCarTravelDistanceBefore + ownCarTravelDistanceAfter;

            float travelCost = driver.GetPaidTravelCost(ownCarTravelTime, ownCarTravelDistance);
            float sharedCarTravelCost = sharedCarTravelDistance * RulesConfig.SharedCarCostsPerKilometer;
            float hotelCost = afterHotelActivity == null ? 0 : RulesConfig.HotelCosts;
            float cost = fullShiftCost + hotelCost + shiftRobustness;

            // Salary rates breakdown
            JArray salaryBlocksJArray = new JArray();
            List<ComputedSalaryRateBlock> mainShiftSalaryBlocks = driverTypeMainShiftInfo.MainShiftSalaryBlocks;
            for (int salaryRateIndex = 0; salaryRateIndex < mainShiftSalaryBlocks.Count; salaryRateIndex++) {
                ComputedSalaryRateBlock computeSalaryRateBlock = mainShiftSalaryBlocks[salaryRateIndex];
                JObject salaryRateJObject = new JObject() {
                    ["rateStartTime"] = computeSalaryRateBlock.RateStartTime,
                    ["rateEndTime"] = computeSalaryRateBlock.RateEndTime,
                    ["salaryStartTime"] = computeSalaryRateBlock.SalaryStartTime,
                    ["salaryEndTime"] = computeSalaryRateBlock.SalaryEndTime,
                    ["salaryDuration"] = computeSalaryRateBlock.SalaryDuration,
                    ["hourlySalaryRate"] = computeSalaryRateBlock.SalaryRate * DevConfig.HourLength,
                    ["usesContinuingRate"] = computeSalaryRateBlock.UsesContinuingRate,
                    ["shiftCostInRange"] = computeSalaryRateBlock.CostInRate,
                };
                salaryBlocksJArray.Add(salaryRateJObject);
            }

            JObject shiftJObject = new JObject() {
                ["activityPath"] = shiftPathJArray,
                ["realMainShiftLength"] = realMainShiftLength,
                ["paidMainShiftLength"] = driverTypeMainShiftInfo.PaidMainShiftLength,
                ["mainShiftStartTime"] = mainShiftStartTime,
                ["realMainShiftEndTime"] = realMainShiftEndTime,
                ["fullShiftLength"] = fullShiftLength,
                ["sharedCarTravelTimeBefore"] = sharedCarTravelTimeBefore,
                ["sharedCarTravelDistanceBefore"] = sharedCarTravelDistanceBefore,
                ["ownCarTravelTimeBefore"] = ownCarTravelTimeBefore,
                ["ownCarTravelDistanceBefore"] = ownCarTravelDistanceBefore,
                ["sharedCarTravelTimeAfter"] = sharedCarTravelTimeAfter,
                ["sharedCarTravelDistanceAfter"] = sharedCarTravelDistanceAfter,
                ["ownCarTravelTimeAfter"] = ownCarTravelTimeAfter,
                ["ownCarTravelDistanceAfter"] = ownCarTravelDistanceAfter,
                ["cost"] = cost,
                ["mainShiftCost"] = driverTypeMainShiftInfo.MainShiftCost,
                ["travelCost"] = travelCost,
                ["sharedCarTravelCost"] = sharedCarTravelCost,
                ["hotelCost"] = hotelCost,
                ["robustness"] = shiftRobustness,
                ["salaryRates"] = salaryBlocksJArray,
            };
            shiftsJArray.Add(shiftJObject);
        }

        static void AddActivityToPath(Activity activity, JArray driverPathJArray, SaInfo info) {
            if (activity.OriginalRawActivities == null) {
                // This is an original activity, add it
                AddRawActivityToPath(activity.Index, activity, driverPathJArray, info);
            } else {
                // This is a combined activity, add each original activity
                for (int i = 0; i < activity.OriginalRawActivities.Length; i++) {
                    AddRawActivityToPath(activity.Index, activity.OriginalRawActivities[i], driverPathJArray, info);
                }
            }
        }

        static void AddRawActivityToPath(int activityIndex, RawActivity rawActivity, JArray driverPathJArray, SaInfo info) {
            JObject activityPathItem = new JObject {
                ["type"] = "activity",
                ["activityIndex"] = activityIndex,
                ["startTime"] = rawActivity.StartTime,
                ["endTime"] = rawActivity.EndTime,
                ["dutyName"] = rawActivity.DutyName,
                ["activityName"] = rawActivity.ActivityName,
                ["projectName"] = rawActivity.ProjectName,
                ["trainNumber"] = rawActivity.TrainNumber,
                ["startStationName"] = rawActivity.StartStationName,
                ["endStationName"] = rawActivity.EndStationName,
            };
            driverPathJArray.Add(activityPathItem);
        }

        static float AddTravelAndWaitBetweenActivitiesToPath(Activity activity1, Activity activity2, JArray driverPathJArray, SaInfo info) {
            int carTravelTime = info.Instance.ExpectedCarTravelTime(activity1, activity2);
            if (carTravelTime > 0) {
                JObject travelBetweenPathItem = new JObject {
                    ["type"] = "travelBetween",
                    ["startTime"] = activity1.EndTime,
                    ["endTime"] = activity1.EndTime + carTravelTime,
                    ["startStationName"] = activity1.EndStationName,
                    ["endStationName"] = activity2.StartStationName,
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
            int travelTimeAfterShift = info.Instance.ExpectedCarTravelTime(activityBeforeRest, parkingActivity) + driver.HomeTravelTimeToStart(parkingActivity);
            int travelTimeBeforeShift = driver.HomeTravelTimeToStart(activityAfterRest);

            JObject restPathItem = new JObject {
                ["type"] = "rest",
                ["startTime"] = activityBeforeRest.EndTime + travelTimeAfterShift,
                ["endTime"] = activityAfterRest.StartTime - travelTimeBeforeShift,
            };
            driverPathJArray.Add(restPathItem);
        }

        static void AddTravelToHomeToPath(Activity activityBeforeHome, Activity parkingActivity, Driver driver, JArray driverPathJArray, SaInfo info) {
            int travelTimeToCar = info.Instance.ExpectedCarTravelTime(activityBeforeHome, parkingActivity);
            int travelTimeFromCarToHome = driver.HomeTravelTimeToStart(parkingActivity);

            JObject travelToCarPathItem = new JObject {
                ["type"] = "travelToCar",
                ["startTime"] = activityBeforeHome.EndTime,
                ["endTime"] = activityBeforeHome.EndTime + travelTimeToCar,
                ["startStationName"] = activityBeforeHome.EndStationName,
                ["endStationName"] = parkingActivity.StartStationName,
            };
            driverPathJArray.Add(travelToCarPathItem);

            JObject travelToHomePathItem = new JObject {
                ["type"] = "travelToHome",
                ["startTime"] = activityBeforeHome.EndTime + travelTimeToCar,
                ["endTime"] = activityBeforeHome.EndTime + travelTimeToCar + travelTimeFromCarToHome,
                ["startStationName"] = parkingActivity.StartStationName,
            };
            driverPathJArray.Add(travelToHomePathItem);
        }

        static void AddTravelFromHomeToPath(Activity activityAfterHome, Driver driver, JArray driverPathJArray, SaInfo info) {
            int travelTimeBeforeShift = driver.HomeTravelTimeToStart(activityAfterHome);

            JObject travelBeforePathItem = new JObject {
                ["type"] = "travelFromHome",
                ["startTime"] = activityAfterHome.StartTime - travelTimeBeforeShift,
                ["endTime"] = activityAfterHome.StartTime,
                ["endStationName"] = activityAfterHome.StartStationName,
            };
            driverPathJArray.Add(travelBeforePathItem);
        }

        static void AddHotelStayAfterToPath(Activity activityBeforeHotel, Activity activityAfterHotel, JArray driverPathJArray, SaInfo info) {
            int halfTravelTimeViaHotel = info.Instance.ExpectedHalfTravelTimeViaHotel(activityBeforeHotel, activityAfterHotel);

            JObject travelBeforeHotelPathItem = new JObject {
                ["type"] = "travelToHotel",
                ["startTime"] = activityBeforeHotel.EndTime,
                ["endTime"] = activityBeforeHotel.EndTime + halfTravelTimeViaHotel,
                ["startStationName"] = activityBeforeHotel.EndStationName,
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
            int halfTravelTimeViaHotel = info.Instance.ExpectedHalfTravelTimeViaHotel(activityBeforeHotel, activityAfterHotel);
            JObject travelAfterHotelPathItem = new JObject {
                ["type"] = "travelFromHotel",
                ["startTime"] = activityAfterHotel.StartTime - halfTravelTimeViaHotel,
                ["endTime"] = activityAfterHotel.StartTime,
                ["endStationName"] = activityAfterHotel.StartStationName,
            };
            driverPathJArray.Add(travelAfterHotelPathItem);
        }


        /* Writing JSON to file */

        static void ExportJsonFile(JObject jObject, string filePath) {
            string jsonString = SortJTokenAlphabetically(jObject).ToString();
            File.WriteAllText(filePath, jsonString);
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

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
                ["cost"] = info.TotalInfo.Stats.RawCost,
                ["drivers"] = CreateDriversJArray(info.DriverPaths, info),
                ["satisfaction"] = info.TotalInfo.Stats.SatisfactionScore.Value,
            };

            string jsonString = SortJTokenAlphabetically(jsonObj).ToString();
            string fileName = string.Format("{0}k-{1}p.json", Math.Round(info.TotalInfo.Stats.Cost / 1000), Math.Round(info.TotalInfo.Stats.SatisfactionScore.Value * 100));
            string filePath = Path.Combine(folderPath, fileName);
            File.WriteAllText(filePath, jsonString);
        }

        static JArray CreateDriversJArray(List<Trip>[] driverPaths, SaInfo info) {
            return new JArray(driverPaths.Select((driverPath, driverIndex) => CreateDriverJObject(driverIndex, driverPath, info)));
        }

        static JObject CreateDriverJObject(int driverIndex, List<Trip> driverPath, SaInfo info) {
            Driver driver = info.Instance.AllDrivers[driverIndex];
            SaDriverInfo driverInfo = info.DriverInfos[driverIndex];

            JObject driverJObject = new JObject {
                ["driverName"] = driver.GetName(false),
                ["realDriverName"] = driver.GetName(true),
                ["driverPath"] = CreateFullDriverPathJArray(driver, driverPath, info),
                ["stats"] = CreateDriverStatsJObject(driver, driverInfo),
                ["info"] = CreateDriverInfoJObject(driverInfo),
            };
            if (driver is InternalDriver internalDriver) {
                driverJObject["isInternal"] = true;
                driverJObject["contractTime"] = internalDriver.ContractTime;
            } else {
                driverJObject["isInternal"] = false;
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
                ["singleFreeDays"] = driverInfo.SingleFreeDays,
                ["doubleFreeDays"] = driverInfo.DoubleFreeDays,
                ["duplicateRouteCount"] = driverInfo.SharedRouteCounts.Sum(),
            };
        }

        static JObject CreateDriverSatisfactionCriteriaJObject(InternalDriver internalDriver, SaDriverInfo driverInfo) {
            Dictionary<string, double> satisfactionPerCriterion = SatisfactionCalculator.GetDriverSatisfactionPerCriterion(driverInfo, internalDriver);

            JObject driverSatisfactionCriteriaJObject = new JObject();
            foreach (KeyValuePair<string, double> criterionKvp in satisfactionPerCriterion) {
                driverSatisfactionCriteriaJObject[criterionKvp.Key] = criterionKvp.Value;
            }
            return driverSatisfactionCriteriaJObject;
        }

        static JArray CreateFullDriverPathJArray(Driver driver, List<Trip> driverPath, SaInfo info) {
            JArray fullDriverPathJArray = new JArray();
            if (driverPath.Count == 0) return fullDriverPathJArray;

            Trip prevTrip = null;
            Trip parkingTrip = driverPath[0];
            for (int i = 0; i < driverPath.Count; i++) {
                Trip searchTrip = driverPath[i];

                if (prevTrip == null) {
                    AddTravelFromHomeToPath(searchTrip, driver, fullDriverPathJArray);
                } else {
                    if (info.Instance.AreSameShift(prevTrip, searchTrip)) {
                        AddTravelAndWaitBetweenTripsToPath(prevTrip, searchTrip, fullDriverPathJArray, info);
                    } else {
                        if (info.IsHotelStayAfterTrip[prevTrip.Index]) {
                            AddHotelStayToPath(prevTrip, searchTrip, fullDriverPathJArray, info);
                        } else {
                            AddTravelToHomeToPath(prevTrip, parkingTrip, driver, fullDriverPathJArray, info);
                            AddRestToPath(prevTrip, searchTrip, parkingTrip, driver, fullDriverPathJArray, info);
                            AddTravelFromHomeToPath(searchTrip, driver, fullDriverPathJArray);
                            parkingTrip = searchTrip;
                        }
                    }
                }

                AddTripToPath(searchTrip, fullDriverPathJArray, info);

                prevTrip = searchTrip;
            }
            AddTravelToHomeToPath(prevTrip, parkingTrip, driver, fullDriverPathJArray, info);

            return fullDriverPathJArray;
        }

        static void AddTripToPath(Trip trip, JArray fullDriverPathJArray, SaInfo info) {
            JObject tripPathItem = new JObject {
                ["type"] = "trip",
                ["tripIndex"] = trip.Index,
                ["startTime"] = trip.StartTime,
                ["endTime"] = trip.EndTime,
                ["dutyName"] = trip.DutyName,
                ["activityName"] = trip.ActivityName,
                ["startStationName"] = info.Instance.StationNames[trip.StartStationAddressIndex],
                ["endStationName"] = info.Instance.StationNames[trip.EndStationAddressIndex]
            };
            fullDriverPathJArray.Add(tripPathItem);
        }

        static void AddTravelAndWaitBetweenTripsToPath(Trip trip1, Trip trip2, JArray fullDriverPathJArray, SaInfo info) {
            int carTravelTime = info.Instance.CarTravelTime(trip1, trip2);
            if (carTravelTime > 0) {
                JObject travelBetweenPathItem = new JObject {
                    ["type"] = "travelBetween",
                    ["startTime"] = trip1.EndTime,
                    ["endTime"] = trip1.EndTime + carTravelTime
                };
                fullDriverPathJArray.Add(travelBetweenPathItem);
            }

            int waitingTime = trip2.StartTime - trip1.EndTime - carTravelTime;
            if (waitingTime > 0) {
                JObject waitBetweenPathItem = new JObject {
                    ["type"] = "wait",
                    ["startTime"] = trip1.EndTime + carTravelTime,
                    ["endTime"] = trip2.StartTime
                };
                fullDriverPathJArray.Add(waitBetweenPathItem);
            } else if (waitingTime < 0) {
                JObject overlapErrorBetweenPathItem = new JObject {
                    ["type"] = "overlapError",
                    ["startTime"] = trip1.EndTime + carTravelTime,
                    ["endTime"] = trip2.StartTime
                };
                fullDriverPathJArray.Add(overlapErrorBetweenPathItem);
            }
        }

        static void AddRestToPath(Trip tripBeforeRest, Trip tripAfterRest, Trip parkingTrip, Driver driver, JArray fullDriverPathJArray, SaInfo info) {
            int travelTimeAfterShift = info.Instance.CarTravelTime(tripBeforeRest, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);
            int travelTimeBeforeShift = driver.HomeTravelTimeToStart(tripAfterRest);

            JObject restPathItem = new JObject {
                ["type"] = "rest",
                ["startTime"] = tripBeforeRest.EndTime + travelTimeAfterShift,
                ["endTime"] = tripAfterRest.StartTime - travelTimeBeforeShift
            };
            fullDriverPathJArray.Add(restPathItem);
        }

        static void AddTravelToHomeToPath(Trip tripBeforeHome, Trip parkingTrip, Driver driver, JArray fullDriverPathJArray, SaInfo info) {
            int travelTimeAfterShift = info.Instance.CarTravelTime(tripBeforeHome, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);

            JObject travelAfterPathItem = new JObject {
                ["type"] = "travelAfter",
                ["startTime"] = tripBeforeHome.EndTime,
                ["endTime"] = tripBeforeHome.EndTime + travelTimeAfterShift
            };
            fullDriverPathJArray.Add(travelAfterPathItem);
        }

        static void AddTravelFromHomeToPath(Trip tripAfterHome, Driver driver, JArray fullDriverPathJArray) {
            int travelTimeBeforeShift = driver.HomeTravelTimeToStart(tripAfterHome);

            JObject travelBeforePathItem = new JObject {
                ["type"] = "travelBefore",
                ["startTime"] = tripAfterHome.StartTime - travelTimeBeforeShift,
                ["endTime"] = tripAfterHome.StartTime
            };
            fullDriverPathJArray.Add(travelBeforePathItem);
        }

        static void AddHotelStayToPath(Trip tripBeforeHotel, Trip tripAfterHotel, JArray fullDriverPathJArray, SaInfo info) {
            int halfTravelTimeViaHotel = info.Instance.HalfTravelTimeViaHotel(tripBeforeHotel, tripAfterHotel);

            JObject travelBeforeHotelPathItem = new JObject {
                ["type"] = "travelBeforeHotel",
                ["startTime"] = tripBeforeHotel.EndTime,
                ["endTime"] = tripBeforeHotel.EndTime + halfTravelTimeViaHotel
            };
            fullDriverPathJArray.Add(travelBeforeHotelPathItem);

            JObject hotelPathItem = new JObject {
                ["type"] = "hotel",
                ["startTime"] = tripBeforeHotel.EndTime + halfTravelTimeViaHotel,
                ["endTime"] = tripAfterHotel.StartTime - halfTravelTimeViaHotel
            };
            fullDriverPathJArray.Add(hotelPathItem);

            JObject travelAfterHotelPathItem = new JObject {
                ["type"] = "travelAfterHotel",
                ["startTime"] = tripAfterHotel.StartTime - halfTravelTimeViaHotel,
                ["endTime"] = tripAfterHotel.StartTime
            };
            fullDriverPathJArray.Add(travelAfterHotelPathItem);
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

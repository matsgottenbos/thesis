using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class JsonHelper {
        public static void ExportSolutionJson(string folderPath, SaInfo info) {
            JObject jsonObj = new JObject();
            jsonObj["cost"] = info.TotalInfo.RawCost;
            jsonObj["satisfaction"] = info.TotalInfo.Satisfaction;
            jsonObj["drivers"] = CreateDriversJArray(info.DriverPaths, info);

            string jsonString = jsonObj.ToString();
            string fileName = string.Format("{0}k-{1}p.json", Math.Round(info.TotalInfo.Cost / 1000), Math.Round(info.TotalInfo.Satisfaction * 100));
            string filePath = Path.Combine(folderPath, fileName);
            File.WriteAllText(filePath, jsonString);
        }

        static JArray CreateDriversJArray(List<Trip>[] driverPaths, SaInfo info) {
            return new JArray(driverPaths.Select((driverPath, driverIndex) => CreateDriverJObject(driverIndex, driverPath, info)));
        }

        static JObject CreateDriverJObject(int driverIndex, List<Trip> driverPath, SaInfo info) {
            Driver driver = info.Instance.AllDrivers[driverIndex];
            DriverInfo driverInfo = info.DriverInfos[driverIndex];

            JObject driverJObject = new JObject();
            driverJObject["driverName"] = driver.GetName(false);
            driverJObject["realDriverName"] = driver.GetName(true);
            if (driver is InternalDriver) driverJObject["driverSatisfaction"] = driverInfo.DriverSatisfaction;
            driverJObject["driverPath"] = CreateFullDriverPathJArray(driver, driverPath, info);
            return driverJObject;
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
            JObject tripPathItem = new JObject();
            tripPathItem["type"] = "trip";
            tripPathItem["startTime"] = trip.StartTime;
            tripPathItem["endTime"] = trip.EndTime;
            tripPathItem["dutyName"] = trip.DutyName;
            tripPathItem["activityName"] = trip.ActivityName;
            tripPathItem["startStationCode"] = info.Instance.StationCodes[trip.StartStationIndex];
            tripPathItem["endStationCode"] = info.Instance.StationCodes[trip.EndStationIndex];
            fullDriverPathJArray.Add(tripPathItem);
        }

        static void AddTravelAndWaitBetweenTripsToPath(Trip trip1, Trip trip2, JArray fullDriverPathJArray, SaInfo info) {
            int carTravelTime = info.Instance.CarTravelTime(trip1, trip2);
            if (carTravelTime > 0) {
                JObject travelBetweenPathItem = new JObject();
                travelBetweenPathItem["type"] = "travelBetween";
                travelBetweenPathItem["startTime"] = trip1.EndTime;
                travelBetweenPathItem["endTime"] = trip1.EndTime + carTravelTime;
                fullDriverPathJArray.Add(travelBetweenPathItem);
            }

            int waitingTime = trip2.StartTime - trip1.EndTime - carTravelTime;
            if (waitingTime > 0) {
                JObject waitBetweenPathItem = new JObject();
                waitBetweenPathItem["type"] = "wait";
                waitBetweenPathItem["startTime"] = trip1.EndTime + carTravelTime;
                waitBetweenPathItem["endTime"] = trip2.StartTime;
                fullDriverPathJArray.Add(waitBetweenPathItem);
            }
        }

        static void AddRestToPath(Trip tripBeforeRest, Trip tripAfterRest, Trip parkingTrip, Driver driver, JArray fullDriverPathJArray, SaInfo info) {
            int travelTimeAfterShift = info.Instance.CarTravelTime(tripBeforeRest, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);
            int travelTimeBeforeShift = driver.HomeTravelTimeToStart(tripAfterRest);

            JObject restPathItem = new JObject();
            restPathItem["type"] = "rest";
            restPathItem["startTime"] = tripBeforeRest.EndTime + travelTimeAfterShift;
            restPathItem["endTime"] = tripAfterRest.StartTime - travelTimeBeforeShift;
            fullDriverPathJArray.Add(restPathItem);
        }

        static void AddTravelToHomeToPath(Trip tripBeforeHome, Trip parkingTrip, Driver driver, JArray fullDriverPathJArray, SaInfo info) {
            int travelTimeAfterShift = info.Instance.CarTravelTime(tripBeforeHome, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);

            JObject travelAfterPathItem = new JObject();
            travelAfterPathItem["type"] = "travelAfter";
            travelAfterPathItem["startTime"] = tripBeforeHome.EndTime;
            travelAfterPathItem["endTime"] = tripBeforeHome.EndTime + travelTimeAfterShift;
            fullDriverPathJArray.Add(travelAfterPathItem);
        }

        static void AddTravelFromHomeToPath(Trip tripAfterHome, Driver driver, JArray fullDriverPathJArray) {
            int travelTimeBeforeShift = driver.HomeTravelTimeToStart(tripAfterHome);

            JObject travelBeforePathItem = new JObject();
            travelBeforePathItem["type"] = "travelBefore";
            travelBeforePathItem["startTime"] = tripAfterHome.StartTime - travelTimeBeforeShift;
            travelBeforePathItem["endTime"] = tripAfterHome.StartTime;
            fullDriverPathJArray.Add(travelBeforePathItem);
        }

        static void AddHotelStayToPath(Trip tripBeforeHotel, Trip tripAfterHotel, JArray fullDriverPathJArray, SaInfo info) {
            int halfTravelTimeViaHotel = info.Instance.HalfTravelTimeViaHotel(tripBeforeHotel, tripAfterHotel);

            JObject travelBeforeHotelPathItem = new JObject();
            travelBeforeHotelPathItem["type"] = "travelBeforeHotel";
            travelBeforeHotelPathItem["startTime"] = tripBeforeHotel.EndTime;
            travelBeforeHotelPathItem["endTime"] = tripBeforeHotel.EndTime + halfTravelTimeViaHotel;
            fullDriverPathJArray.Add(travelBeforeHotelPathItem);

            JObject hotelPathItem = new JObject();
            hotelPathItem["type"] = "hotel";
            hotelPathItem["startTime"] = tripBeforeHotel.EndTime + halfTravelTimeViaHotel;
            hotelPathItem["endTime"] = tripAfterHotel.StartTime - halfTravelTimeViaHotel;
            fullDriverPathJArray.Add(hotelPathItem);

            JObject travelAfterHotelPathItem = new JObject();
            travelAfterHotelPathItem["type"] = "travelAfterHotel";
            travelAfterHotelPathItem["startTime"] = tripAfterHotel.StartTime - halfTravelTimeViaHotel;
            travelAfterHotelPathItem["endTime"] = tripAfterHotel.StartTime;
            fullDriverPathJArray.Add(travelAfterHotelPathItem);
        }
    }
}

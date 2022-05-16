using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Thesis {
    class DebugJsonExporter {
        readonly Instance instance;

        public DebugJsonExporter(Instance instance) {
            this.instance = instance;

            ExportAssignment("142k-68p", "6 e4.6h e3.4 e2.1 e0.5 e3.0 9 e3.6 e2.5 6 0 2 2 e1.10 4 4 9 10 e3.0 e3.2 5 2 4 e0.17 0 9 7 e1.1 11 0 7 e1.11 2 11 e1.11 10 5 4 e4.3 e4.9 0 e1.8h 10h 2 7 7 5 11 e3.5 e2.8 7 11 e4.6 e0.7 7 e0.12 e4.7 11 e4.7 13 e2.8 13 e1.3 e2.4 13 13 e2.7h e2.6 e0.5 8 4 0 7 0 e1.8h 8 1 e1.4h e0.10 0 e3.10 6 e0.0h 8 4 e4.1 6 7 e2.12 e1.11 e1.1 e1.5 e2.2 10 0 8 e1.10 10 3 4 3 4 7 e4.2h e1.2 6 e1.1 e1.7 6 0 10 7 e4.5 e4.5 6h e0.1h e1.0 e1.6h 10 e3.7 e0.11 e4.5h 3h e4.0 e2.1 e0.13 e2.1 e4.3 e1.5 1 e4.9 14 e2.7 e3.8 e1.8h e0.8 1 e2.2 14 e0.0 0 e1.2 e3.3 0 1 12 2 0 e3.4 14 e2.10h e4.6 11 e1.2 7 e2.9 1 e0.16 e0.2 14 0 e3.10 12 11 e0.1h e2.5 5 e2.6h 2 6 e0.14 13 11 10 e1.2 7 e2.3 2 5 e2.0 13 e4.4 e3.1 e1.3 e1.4 7 e4.2 e0.5 12 14h 12 0 e1.0h 3 11 13 e0.2 7 6 e0.13 10 13 6 e4.8 13h 7 e4.0 e1.9 6 e3.0 5 8 e3.9 e4.2 e0.4 5 e4.1h 6 e1.9h e1.6 e4.5 e0.8 e2.8h 8 8 e2.11 3 8 e4.5h e1.11 8 e2.6 e3.3h e1.8 8 e2.4 9 e0.1 12 e1.10 e0.13h e3.4 14 9 14 e4.8 e2.3 e3.4h 12 7 7 e2.10 e1.7h e2.3 9 14 12 2 14 13 e3.7 e4.0h e3.6 7 9 e0.12h 12 9 e1.4 e4.1 2 13 14 2 e4.4 14h 5 e4.5 e3.7h e0.16 7 2 e4.5 e1.5h e4.7h e1.0 e3.6 e0.4 e1.3 e0.9 e2.9 13 e3.10h 4 4 e4.6 5 6 4 e3.1h e0.14 e3.5h 6 e0.7h 5 e1.9 e0.15 e0.5 e2.8 e0.11 e1.6 4 5 8 e2.0h e2.4 e2.10h 4 9 e1.4 9 e0.13 14 4h e2.4h 3 e4.8h 8 9 e1.2h e1.7h 10 12 14 6 9 3 14 e3.3 e4.0 e0.11h 12 14 e0.10 2 e3.4 14 12 11 10 12 3 11 10 e0.3 e2.11h 3 2 9h 2 e3.7 3 11 10 e3.10 14 e1.5h 12h e2.12 1 11 e0.9 e4.3 e3.0 e2.9 e0.12 3h e3.1h 14h 2 e3.10 2 e0.7 e4.2 e3.6 1 0 2 2 e2.7h 11 0 0 e3.8h e0.1h 13 e2.1 e2.3h e1.9 13 1 e4.7h e1.3h 13 e0.0 e4.8h 0 e3.5 e2.4 e0.17 0 13 e2.0 e2.10 e2.10 e2.2h e1.7 3 e2.11 e0.4 14 e1.2h e1.5 e0.11 e3.2h 12 4 e3.7 e3.10 e2.9 9 e3.9 e3.6h 8 9 14 e4.9 9 3 5 4 e3.1 14 4 e4.6 e1.0 e0.6h e1.4 3 12 8 12 8 e1.0h 1 e2.3 e4.7 e2.7 9 e1.6h e2.6 5 3 12 e4.4h 4 e1.1 9 e2.8 14 e1.3 1 4 1 9 9h e4.8 e0.11 e1.10 e2.7h 8 e0.9 7 1 e4.1 5 e1.11h 5 e3.3 e2.5h 11 e2.3h e4.1h e0.1 7 8 e1.1h e2.12 1 e0.10 e0.10 e3.8 5 e0.12 2 11 e2.0 e4.3 7 e0.7 11 2 e4.2 e2.2h e0.2h e3.5h e0.15 11 e0.7 2 11 e0.14 e0.4h e0.17 e4.9h 7 2 e3.9 e1.9 e1.8 2 2 e0.6h e3.2h e0.13 2 4 e0.15 e4.4h e1.0 e3.3 3 e1.2 e1.10 3 9 e1.11 e2.1h 3 e3.6 e3.1 e1.10 e1.1 e0.9 4 13 14 e0.11 0 9 9 e2.8 e2.3 e2.6 0 e2.11 14 6 e2.7 13 9 e0.3 3 14 e3.6 e0.11 e2.9 e3.3 9 6 3 e4.5 8 10 e2.9 4 0 e3.7 3 e2.2 14 e3.3 10 8 e3.1 9 e2.5 e2.0 0 e2.10 13 e2.4 12 0 e0.6 e2.9 6 12 1 e4.7 e1.6 e3.9 5 12 10 e3.5 e3.2 12 e0.10 1 5 11 e4.1 e1.7 e2.1 11 11 e4.3 e4.9 e0.17 1 e0.4 e0.2 e2.11 12 e4.3 e3.4 e2.12 1 e4.4");

            ExportAssignment("147k-75p", "e0.13 e1.2 e0.15 6 e0.16 e3.6 5 9 e1.4 3 e3.8 e3.6 e0.15 e0.1 14 5 e3.0 1 5 6 0 14 e1.8 e1.6h 9 e2.0 e4.6 0 4 5 2 7 7 14 5 1 e4.2h e0.0h e4.5 9 0 e0.4h 4 1h 3 14 5 7 0 e1.10h e4.8 e0.5 4 e4.1 4 e4.0h 7 e1.5h 7 e4.8h e1.9h e3.2 e2.1 e2.12 e0.9 e3.5 e0.1h e1.6 e4.9 10 e3.3 0 13 12 10 4 0 e0.2 e4.0h 8 e0.12 e3.7 e0.3h 4 e4.8 e3.6 e2.6h 13 e1.10 12 1 10 13 9 12 e1.1 4 e1.5h 6 e0.0h e0.4 1 6 10 11 e1.11 e4.2 8 e3.4h 12h 13 0 e1.9h 1 8 9 9 6 e4.3 11 e0.7 e3.1h e3.9 e3.10h 11 e1.4 e3.3h e2.2 e0.2 2 14 5 e0.1 e1.1 e1.7 12 12 5 14 13 2 e0.5 14 0 12 5 e0.9 e4.9 e2.11 e4.2 e4.7h e2.1 e3.4 e4.1 13 2 2 e4.0 12 7 0 e0.17h 14 13 e2.4h e2.10h 0 e2.6 e3.8 e4.2 e0.14h e2.7 7 e2.5 9 5 e0.3 e2.1 e3.4 e1.5 e0.11h 10 e4.0 e0.0h e1.9 1 e4.9h e4.0 0 e2.12 0 e0.4 10 e2.12 7 e2.3 0 3 9 e3.8 9 3 10 e3.10 e3.3h 7 10 9 e2.3 e3.10h 11 e2.0 e1.3h 11 1 e4.3 3 10 1 e3.1 8 e4.6h e4.4 e1.11h e0.17h 3 11 3 8 e2.0 e1.8h e1.10h e4.4 e4.7 e0.1h 8 e2.11 e2.6h e0.8 e3.0 e3.6 5 e0.12 14 8 e1.7h 4 e2.10h e0.11 e3.7h e0.0 e2.9 e0.16 4 5 e2.8h 13 e4.1 e4.9 e0.10 e1.11 12 5 e2.4 14 10 13 0 4 e1.3 5 e0.14 0 13 5 0 11 14 e0.17 0 3 e1.8 12 11 e3.10 10 13 e3.3 e0.3 e3.8 e4.6h e2.12 1 e2.4h e0.4 3 0 e2.0 e0.14h 12h e2.7h e0.1 e0.5h e3.5 0 3 3h e3.8 e3.0 e2.6 e3.7 e1.1h 1 e2.8 e2.0 e1.10 e1.0h e0.10 5 e0.6 e4.3h e0.15h 1 1 e1.7h e1.4 2 e2.1 e4.7 e4.0 e2.11 2 8 6 4 5 8 e2.10 4 e3.1 e4.8 8 14 e2.2 e2.3 e4.0 6 8 e2.6 e2.5h 4 8 14 e0.9h e1.9h 2 4 e3.9h 8 5 e2.2 4 14 e2.4h e4.0 e0.12 e0.13 e0.16h e2.3 2 e0.7h 5 6 e2.9h e0.14 e4.6 11 4h 14 2h e2.7h 13 8 e0.13 10 e3.5h 14 7 11 12 e1.2h 13 13 7 12 11 e1.0 e3.10 e1.1 e4.3 3 e2.11 7 13 11 e3.4h 12 e4.2 e3.8h 7 12 3 3 e1.9 7 3 7 e2.8h e0.5 e0.0 3 9 e3.9h e2.5h 10 e2.0 8 e0.0 4 e0.15 e3.3 e4.2h e4.8 1 8 e1.3 e1.7 8 e2.2 1 e2.10 10 4 e1.3h 2 e0.9 6 e0.12h e0.15 9 1 1 9 e1.10 8 e1.8 e1.0 2 8 e3.5 e2.0 e3.4h 4 e2.4h e3.1 6 e2.6 10 e2.9h e1.6h 2 6 e3.3 6 e4.5 10 e0.16 e3.6h 4 e3.2h e2.7h e0.7 e2.3 6 8 10 7 e4.9 e2.6 e1.11 1 e3.7h e1.2h 2 e1.5 13 4 e1.0 6 13 e0.10 e0.6h 7 e2.11 6 13 e2.1 e4.9 e0.8 e1.5h e4.7 7 13 5 e2.10 e2.8 e2.11 e0.17h e0.10 e0.11 e0.13h 7 e0.4 e0.3 e0.2 5 13 12 e0.11 5 12 12 e4.0 13 e1.3 5 12 e2.5 e4.8 e2.7 5 14 14 e2.4 e3.7 8 e1.6h e2.8h 14 e1.7 e3.8 e2.10 12 e4.2 e2.4 8 e2.1 11 6 e0.15 e1.3 e0.5 6 e1.4 e2.9 3 e3.10 e3.0 11 e2.0 0 11 e3.7 e2.4 8 3 e3.9 e1.2 11 e1.0 0 14 e4.4 e3.2 3 8 0 14 e3.10 e3.0 8 0 9 3 3 e3.4 e0.12 2 3 e4.6 6 2 e4.7 3 7 0 e3.1 e0.16 6 e0.7 e3.6 6 0 9 e3.4 e3.5 9 e4.1 e0.9 e1.6 2 7 e0.15 e1.5 e0.3 7 e4.5 e2.5 e0.17 e4.9 e2.5 e2.8 e0.13 7 e3.5 e1.4 e3.6 e2.12 e2.5 e0.6");
        }

        public void ExportAssignment(string name, string assignmentStr) {
            SaInfo info = ParseHelper.ParseAssignmentString(assignmentStr, instance);

            JObject jsonObj = new JObject();
            jsonObj["cost"] = info.TotalInfo.CostWithoutPenalty;
            jsonObj["satisfaction"] = info.TotalInfo.Satisfaction;
            jsonObj["drivers"] = CreateDriversJArray(info.DriverPaths, info);

            string jsonString = jsonObj.ToString();
            string fileName = Path.Combine(Config.OutputFolder, name + "-visualise.json");
            File.WriteAllText(fileName, jsonString);
        }

        JArray CreateDriversJArray(List<Trip>[] driverPaths, SaInfo info) {
            return new JArray(driverPaths.Select((driverPath, driverIndex) => CreateDriverJObject(driverIndex, driverPath, info)));
        }

        JObject CreateDriverJObject(int driverIndex, List<Trip> driverPath, SaInfo info) {
            Driver driver = instance.AllDrivers[driverIndex];
            DriverInfo driverInfo = info.DriverInfos[driverIndex];

            JObject driverJObject = new JObject();
            driverJObject["driverName"] = driver.GetName(false);
            driverJObject["realDriverName"] = driver.GetName(true);
            if (driver is InternalDriver) driverJObject["driverSatisfaction"] = driverInfo.DriverSatisfaction;
            driverJObject["driverPath"] = CreateFullDriverPathJArray(driver, driverPath, info);
            return driverJObject;
        }

        JArray CreateFullDriverPathJArray(Driver driver, List<Trip> driverPath, SaInfo info) {
            JArray fullDriverPathJArray = new JArray();
            if (driverPath.Count == 0) return fullDriverPathJArray;

            Trip prevTrip = null;
            Trip parkingTrip = driverPath[0];
            for (int i = 0; i < driverPath.Count; i++) {
                Trip searchTrip = driverPath[i];

                if (prevTrip == null) {
                    AddTravelFromHomeToPath(searchTrip, driver, fullDriverPathJArray);
                } else {
                    if (instance.AreSameShift(prevTrip, searchTrip)) {
                        AddTravelAndWaitBetweenTripsToPath(prevTrip, searchTrip, fullDriverPathJArray);
                    } else {
                        if (info.IsHotelStayAfterTrip[prevTrip.Index]) {
                            AddHotelStayToPath(prevTrip, searchTrip, fullDriverPathJArray);
                        } else {
                            AddTravelToHomeToPath(prevTrip, parkingTrip, driver, fullDriverPathJArray);
                            AddRestToPath(prevTrip, searchTrip, parkingTrip, driver, fullDriverPathJArray);
                            AddTravelFromHomeToPath(searchTrip, driver, fullDriverPathJArray);
                            parkingTrip = searchTrip;
                        }
                    }
                }

                AddTripToPath(searchTrip, fullDriverPathJArray);

                prevTrip = searchTrip;
            }
            AddTravelToHomeToPath(prevTrip, parkingTrip, driver, fullDriverPathJArray);

            return fullDriverPathJArray;
        }

        void AddTripToPath(Trip trip, JArray fullDriverPathJArray) {
            JObject tripPathItem = new JObject();
            tripPathItem["type"] = "trip";
            tripPathItem["startTime"] = trip.StartTime;
            tripPathItem["endTime"] = trip.EndTime;
            tripPathItem["dutyName"] = trip.DutyName;
            tripPathItem["activityName"] = trip.ActivityName;
            tripPathItem["startStationCode"] = instance.StationCodes[trip.StartStationIndex];
            tripPathItem["endStationCode"] = instance.StationCodes[trip.EndStationIndex];
            fullDriverPathJArray.Add(tripPathItem);
        }

        void AddTravelAndWaitBetweenTripsToPath(Trip trip1, Trip trip2, JArray fullDriverPathJArray) {
            int carTravelTime = instance.CarTravelTime(trip1, trip2);
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

        void AddRestToPath(Trip tripBeforeRest, Trip tripAfterRest, Trip parkingTrip, Driver driver, JArray fullDriverPathJArray) {
            int travelTimeAfterShift = instance.CarTravelTime(tripBeforeRest, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);
            int travelTimeBeforeShift = driver.HomeTravelTimeToStart(tripAfterRest);

            JObject restPathItem = new JObject();
            restPathItem["type"] = "rest";
            restPathItem["startTime"] = tripBeforeRest.EndTime + travelTimeAfterShift;
            restPathItem["endTime"] = tripAfterRest.StartTime - travelTimeBeforeShift;
            fullDriverPathJArray.Add(restPathItem);
        }

        void AddTravelToHomeToPath(Trip tripBeforeHome, Trip parkingTrip, Driver driver, JArray fullDriverPathJArray) {
            int travelTimeAfterShift = instance.CarTravelTime(tripBeforeHome, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);

            JObject travelAfterPathItem = new JObject();
            travelAfterPathItem["type"] = "travelAfter";
            travelAfterPathItem["startTime"] = tripBeforeHome.EndTime;
            travelAfterPathItem["endTime"] = tripBeforeHome.EndTime + travelTimeAfterShift;
            fullDriverPathJArray.Add(travelAfterPathItem);
        }

        void AddTravelFromHomeToPath(Trip tripAfterHome, Driver driver, JArray fullDriverPathJArray) {
            int travelTimeBeforeShift = driver.HomeTravelTimeToStart(tripAfterHome);

            JObject travelBeforePathItem = new JObject();
            travelBeforePathItem["type"] = "travelBefore";
            travelBeforePathItem["startTime"] = tripAfterHome.StartTime - travelTimeBeforeShift;
            travelBeforePathItem["endTime"] = tripAfterHome.StartTime;
            fullDriverPathJArray.Add(travelBeforePathItem);
        }

        void AddHotelStayToPath(Trip tripBeforeHotel, Trip tripAfterHotel, JArray fullDriverPathJArray) {
            int halfTravelTimeViaHotel = instance.HalfTravelTimeViaHotel(tripBeforeHotel, tripAfterHotel);

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

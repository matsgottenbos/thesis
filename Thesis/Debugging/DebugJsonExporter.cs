using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Thesis {
    class DebugJsonExporter {
        readonly Instance instance;
        readonly SaInfo info;

        public DebugJsonExporter(Instance instance) {
            this.instance = instance;

            info = new SaInfo(instance, null, null);

            ExportAssignment("140k-81p", "e1.6 e4.6 e1.7 e1.10 e3.2 e2.3h 9 e2.6 10 8 e3.4 e1.4 12 e2.6 9 e3.7 1 e3.3 9 e3.1 e3.2 8 e3.9 9 9 10 8 12 3 1 e0.7 6 8 13 3 e4.3 7 9 6 e3.5 1 e4.7 1 13 2 2 3 7 13 e4.2 e4.9 3 e0.3 e4.1 e2.5 e1.5 e2.9 e4.9 5 13 5 e3.6 e0.14 e3.10 e2.0 e0.4 e2.11h 5 5 e2.3 e0.11 e0.5 1 3 7 8 11 e0.13 8 e4.1 8 3 e1.7 7 8 12 3 14 e0.4 14 0 e4.6 14 1 2 0 e4.5 1 11 8 e1.8h 14 12 e4.3h 11 0 e2.8h e2.4 7 2 3 e3.2 0 2 7 0 12 0 12 0 e3.5 e0.1h e4.9 e0.10 e3.4 e1.11h e2.5 8 e2.11h 9 e1.4h 13 e3.3h 6 4 8 9 14 e4.4 4 8 e0.16 4 14 13 e3.7 e1.10 14 3 5 9 1 7 10 e0.0 5 e4.8 7 13 e0.15 e3.2 e1.9 8 e1.6 5 e4.5 10 e3.7 0 e4.3 e0.5 1 e1.1 13 e2.8 e2.12 6 e2.7 4 e1.10h 1 12 0 12 e0.14h 4 5 0 14 e4.0 14 e1.9 e3.8 7 e2.10 10 e1.1 e4.8h 7 1 7 e4.0h e1.7 10 11 10 e0.1 e3.9 e0.6 e3.5 e1.1h e2.2 e1.11 11 e0.17 e4.6 e1.2 e2.8h e1.8 e2.11 e0.2 e0.13 e2.4 e4.4 e3.0 e2.3 e2.2 e4.7 e1.0h e3.5 e0.9 4 e1.6h e2.6h e0.10 6 e1.7 e1.4 4 5 e2.9 5 e2.5h 6 4 4 14 e4.8 e2.0 4h e3.3 e2.0 10 5 6 e1.2 6 e2.10 2 e3.3 e2.2 e4.8 10 6h e1.1 e1.3 11 e4.0h e4.2 e1.10 5 11 e0.0 10 e0.11 e0.0h e2.4 14 e3.0 e4.4 e0.12 8 e3.4 e2.8 e2.4 e1.3 8 2 e1.9 e0.15 11 11 e0.14 e0.12 e0.8 e4.1 e2.9 9 e4.6 3 11 e3.7h 9 8 e3.5 e1.3 e1.3h e0.7 e0.4h e3.9 3 e4.6 e1.0 8 7 e1.11 7 e1.1h e1.6 7 3 e2.5 14 e1.8 e1.11 1 14 e2.0 7 5 4 14 13 1 10 7 e2.12h 0 10 e0.5 1 13 0 e3.8h 10 5 e4.7 13 0 5 0 13 1 4 14 5 13 0 e3.0 1 e4.2h e2.7 0 10 2 e0.0 14 e0.2 e2.6 10 4 13h e4.0h e0.8 e3.10 e2.9h 1 5 5 14 2 6 1 0h 2 e0.11 e0.0h 11 6 2 2 11 11 e3.6 6 12 e3.7h e0.10 9 2h e2.12 e0.4 11 12 6 e2.2 e2.10h e0.13h e1.1 e0.14 e1.3 11 11 9 9 e0.17 9 8 e4.3h e4.9 10 10 e3.8h 4 e0.12h 10 e3.9 e3.10h e4.8 e3.0 8 e3.2 e0.5h e1.9h 8 e4.8 13 4 e4.4 13 e0.6 e0.1 e0.8h 8 e1.5h 10 8 e1.2 e2.1 13 e1.11 13 13 e2.2 4 e1.10 e3.3 e3.1 10 e0.16 e2.7 8 e4.0 e2.10h 12 4 2 10 e3.0h e0.15 e4.4 13 4 13 10 e4.2 e1.4h e4.1 e3.9h e2.6h e4.5h 8 e0.0 e2.12h e2.2h e2.7 e2.4 e2.0 e1.3h 0 e0.9 e0.17 e2.9 e0.2 12 e1.11h e0.7 e2.4h 13 2 2 e3.6h e2.1 e4.0 e2.8 e0.3h e2.1 e4.7 12 e3.7 e0.11h 7 e3.5 0 e3.2 e0.13h e1.7 e4.8 e2.3 7 12 e1.1 e3.10h e4.7h e4.9 1 e0.12 e0.8 e0.6 e0.12 1 1 7 e1.8 e0.14 1 e1.2 e4.5 e0.12 e0.5 1 14 e1.8h e1.5 5 3 11 e3.2 14 6 e3.9 13 e1.0 e2.6 e1.2 6 e3.8 e2.2 3 13 1 11 e2.5 5 e1.9 14 e0.1 3 0 13 e0.9 3 e3.4 e2.10 e2.11 14 0 5 e2.2 e3.3 13 e2.8 3 6 14 e3.8 13 0 e2.11 5 e1.9 14 6 14 3 4 e1.11 e0.3 e2.12 e3.0 e2.7 e4.3 0 e2.4 e4.1 e3.6 e0.8 e2.3 13 e0.6 e1.3 e0.8 0 4 e2.1 e0.3 e0.10 4 e0.17 e4.9 e1.10 e0.7 9 e0.13 e4.8 9 9 e0.11 e4.1 e4.6 e2.7 e1.4 e3.10 e3.1 9 e1.8 9 e4.7 e0.4 e4.9");

            ExportAssignment("169k-88p", "e0.0 e2.3 e4.7 e2.12 e2.0h e1.2 e0.8 14 2 e3.6 e2.11 e4.0 3 14 e1.11 7 0 e1.0 7 e2.3 e0.11 e0.1 13 12 9 3 0 2 14 9 12 e0.5 7 4 10 14 e2.2 0 e4.2 9 13 e0.6 13 e2.9 e0.1 e4.1 4 e2.7 12 7 9 e3.5h 13 e2.1 e1.8h 10 4 4 e4.1 e2.1 11 4 e2.7 e0.0 4 e2.4 e0.12h e2.0 8 14 9 e2.8 e4.8 9 1 14 e2.3h e2.11h e3.5 5 9 13 e0.9 14 7 e3.0 13 e0.13h e3.1 8 5 e4.5 1 e0.6 e0.11 6 5 e1.8 4 e3.5 e0.4h 9 e1.6 13 1 e4.1 e2.10h 3 5 7 e4.9h e1.1 e3.1h e0.9h 4 e4.1h 6 e2.9 e1.3 e1.4 e0.1 e3.2 e4.3h e2.5 e1.3h e3.2 e4.2 e1.5 e0.12 2 0 10 e0.3h e3.6 11 11 e1.11 10 e0.12 2 e3.4 8 e1.9 e2.11 0 e4.7 e3.0 e0.17 12 10 e0.7 13 4 e3.3 e3.1 e4.6 e2.10 2 e0.9 e1.7h 1 e2.4 9 e3.1h e4.6 e4.9 e0.10 e2.3 13 4 e2.11 0 e2.12 8 e0.1 13 e1.6 e2.6 2 e4.3 e0.13 5 1 e0.5 e4.1h e2.10 e1.1 7 9 1 9 e0.0 e3.4h e4.8 e3.3h 12 9 5 4 e1.3h 4 5 12 e3.9 5 e2.1 14 6 e2.3 e0.16 e2.4 5 e0.2 7 14 e4.2 e4.8 6 e0.4 e4.0h e2.8 e1.2 e1.0h 14 11 e0.6 6 11 6 e3.2 e0.17 11 e0.14 e0.8h e4.6 4 e4.5 e0.6 0 3 2 10 11 4 1 1 e1.7h 3 10 2 e1.9 0 3 1 e3.3h e3.1 e3.0 12 e2.6 4 10 13 e0.11 e4.5 10 e3.4h 1 12 3 e0.3 5 e4.1 0 1 5 e2.6 2 e1.5 e1.4 e0.7 e3.1 e2.12 e1.3 e0.12h e1.8h e3.10 e0.11 e0.10h e4.4 e1.0 e3.1 5 e3.9 9 e0.13 e2.10 13 e4.8 e4.1 6 5 e0.17h e0.13h 5 e0.14 9 e2.1 e4.7 e0.7h e2.1 e2.12 6 e0.9 e1.3 9 e4.8 9 6 14 e3.2h e0.4 e4.0 6 3 e4.7 2 e2.2 7 4 e1.6h 14 e3.6 2 12 8 e1.2h 12 6 1 e3.3 12 3 e1.10h e3.8 8 e1.9 12 4 2 e1.7 e3.4h 0 4 1 8 3 2 e3.0h 4 7 14 4 0 12 e0.2 e2.2 e2.0 4 e0.8 e4.4 0 e3.9h 7 e0.1 e3.8 e1.4h e0.12 3 1 8 10 8 5 e4.6 e3.1 11 0 e2.4 e0.15 e4.3 e0.8 e2.8 e4.4 e3.10 5 e4.5 e0.7h e3.7 e0.10h e1.11 11 e4.2 e1.8 e3.5h e2.8h 13 10 e1.10h e4.7h 11 5 e4.3h e2.9 13 13 e3.0 e2.6 8 e0.4h e4.1 e0.13 13 e4.9 e2.6h 6 e2.0 e0.17 e1.6 e2.11 e2.3 e1.2h e3.2 e1.0 1 e0.14h e1.1 1 e3.0 8 e0.6 e1.6 e2.5 e3.8 e3.4 e1.7 e2.12 e0.13h e3.3 e3.2 e0.0 1 1 e4.1 e0.1 e0.10 e1.7 e4.2 e3.9 14 e2.1 e2.7 e1.11 e3.10 e4.3h e3.8h 6 8 1 e2.3 14 e3.10h e1.0 e2.0 6 e2.8h 1 e2.2h e2.12h e1.4 e3.4 8 e4.1 e4.9h e2.5h e0.10 e4.4 14 e3.7h e2.3h 11 e0.15 e2.4 e2.7 e0.8h e0.6 e3.1 e3.3 e0.5h 1 e4.4h e1.3 e2.4 3 14 e3.9 e1.10 e4.5 e0.7 2 3 11 e2.9 9 e4.7 e3.5 e1.9h e4.7 3 e0.15 11 e3.1 e3.6 e4.6 e0.12 e0.16h 2 11 3 5 e1.1h e4.0 2 9 e2.10 e0.13 3 2 e0.14h e3.8 e2.8 e4.7 10 e0.11 e4.8 9 7 0 10 e3.5h 7 e1.2 5 10 e2.6 e1.8 12 e0.10 e0.17 6 e4.8h e3.10 e0.6 e2.12 0 8 e0.8 e1.6 7 5 12 e2.5 e3.7 e0.4 e3.8 e2.3 7 e2.11 e0.5 e0.11 10 e4.3 6 6 12 e2.3 e2.11 0 e4.5 e1.2 e1.4 e3.4 0 e2.5 6 0 e4.9 e1.1 e0.3 0 e2.1 e3.2 e4.5 8 6 e1.4 e2.9 e0.15 e4.4 12 e3.3 e1.5 e2.7 e2.4 e4.3 e2.0 e1.10 e0.15 e0.0 e4.0 e1.4 e1.3 e0.16 e2.9 e2.2 e3.9 e3.4 e4.8 e3.5 e3.6 e0.7 e0.9 e3.5 e0.14 e1.9 e3.9 e4.6 e2.0 e1.11 e4.8 e2.10");
        }

        public void ExportAssignment(string name, string assignmentStr) {
            (info.Assignment, info.IsHotelStayAfterTrip) = ParseHelper.ParseAssignmentString(assignmentStr, instance);
            JObject jsonObj = new JObject();
            jsonObj["drivers"] = CreateDriversJArray(info.DriverPaths);

            string jsonString = jsonObj.ToString();
            string fileName = Path.Combine(Config.OutputFolder, name + "-visualise.json");
            File.WriteAllText(fileName, jsonString);
        }

        JArray CreateDriversJArray(List<Trip>[] driverPaths) {
            return new JArray(driverPaths.Select((driverPath, driverIndex) => CreateDriverJObject(driverIndex, driverPath)));
        }

        JObject CreateDriverJObject(int driverIndex, List<Trip> driverPath) {
            Driver driver = instance.AllDrivers[driverIndex];

            JObject driverJObject = new JObject();
            driverJObject["driverName"] = driver.GetName(false);
            driverJObject["realDriverName"] = driver.GetName(true);
            driverJObject["driverPath"] = CreateFullDriverPathJArray(driver, driverPath);
            return driverJObject;
        }

        JArray CreateFullDriverPathJArray(Driver driver, List<Trip> driverPath) {
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

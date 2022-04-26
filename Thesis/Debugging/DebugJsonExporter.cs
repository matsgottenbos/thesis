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

            ExportAssignment("e1.10 2 14 10 e3.10 3 e1.8 14 2 e3.5 5 3 3 14 10 e1.10 e3.10 e1.8 e1.0 6 e4.6 2h e4.0 10 10h e3.10 5 e3.5 6 6 3 5 e3.1h e4.2 6 e4.5 7 e4.6 e1.0 5 e3.5 e3.2 e4.2 1 e4.0 1 7 6 7 e2.6 1 e3.3 6 e4.3 1 e2.6 e4.5 e4.2 e3.3 7 e3.3 7 1 e3.2 7 7 e0.13h 2 2 11 10 11 e0.7 8 e1.8 e1.7 e4.5 e3.1 8 10 e2.7 11 e1.3 9 8 e4.2 e2.4 12 e0.7 12 6 e3.1 e3.6 e1.5 12 e2.11 9 e1.8 e4.5 8 e1.6 e3.1h e1.7h 11h e4.5 e4.8 e2.7 9 e1.3h 6 e2.9h 8 12 12h 9 6 9 7 9h 7 14 e0.9 e1.6h e0.9 14 7 14 e1.1 1 e4.8 10 14h e0.13 e1.9 13 e1.1 e0.9 1 4 1 13 e4.6 e3.10 e1.3 1 e0.11 e4.3 e4.4 e4.9 4 e1.5 6 6 13 6 9 e0.12 11 10 e3.1 1 e1.1 e1.0 0 9 e4.6 1 e0.11 e4.8 e1.4 e1.10 3 e3.10 4 11 e4.8h 10h e4.3 e2.1 e3.1 3 13 e4.7 e1.11 e2.9h 0 6 e1.3 e1.9 e4.4h e4.6 e0.3 e1.0 12 e1.7 e3.2 e0.11 4h 12 e4.1 12 6 e1.5h 3 11 e3.1 e4.7 12 7 e3.2 8 12 e2.1 7 e1.11 8 e1.6 e1.4 e1.7 e1.6h 12 e3.2h e4.1h 7 7 8 14 e2.12h 8h 14 e2.11 14 0 e4.8h 4 10 5 e0.6 13 e0.10 e2.11 1 10 5 10 0 4h e4.4 14 10 1 0 e1.11 10 13 e2.7 13 0h e0.6 1h e0.10h e4.5 e0.11 13 5 e4.6 10 2 11 e1.4h 10 e1.5 5h 10 e2.9 e2.10 e3.2 e2.4 e4.4 e2.12 2 e2.7 e3.2 e2.6 e2.12 e1.11 e2.10 e2.4 e1.5 e3.4 11 11 e4.5h 2 2h e4.1 e0.4 e4.6 e3.2 e3.5 11h e4.1 e1.6 8 e2.9 e2.10 e1.5 e4.7 e0.0 e0.1 e3.4 8 e0.4 e2.6 e3.0 4 e4.7 e1.0 e3.5 e1.8 13 e3.9h e0.0 7 0 4 8 e3.6 e0.6 e4.7 e4.8 8h e1.6 5 4 e3.0 14 4 1 e1.8h 1 5 14 e0.12 7 13 4 9 12 13 14 4 9 13 1 0 14 4 7 4h e0.6h 14 9 1 e3.6 13h 3 e4.6 e1.1 12 0h 9 7h e2.7 5h e0.10h e0.12 e2.4 14 e4.8h 2 e1.4 1 12 2 3h e4.2 14 9 11 11 6 e3.4 e1.1 e2.4 e4.6 e1.4 e3.4 2h e4.2 11 e2.7 e4.5 e4.2 e2.4 e1.1 6 e3.5h e4.5 e0.1 11 6 6 11 e3.4 e1.8 11 e1.8 e0.7h e0.11 e3.0 0 e1.5h e0.1 5 e4.4 13 e3.0 4 e0.2 e1.8 e4.5h e3.9 3 5 e1.0 8 e0.11 e1.7 13 e1.8 7 0 e0.6 e1.1 4 5 e1.9 8 13 3 13 3 e2.3 e1.8h e3.0 e0.2 4 e2.11 e0.6 e1.0 5 8 e4.3 8 e3.2 e4.4 4 e2.5 e4.0 7 e4.4 e3.9 e3.9 e0.11h 9 e3.3h 12 e1.7 e2.11 e2.4h 0h e0.14 13h e3.1 5 4h 9 3 e0.14 11 e4.8 e2.6 7 e1.9 8 12 e4.2 e1.1h 8h e1.2 e0.10 12 e4.8 9 e4.0h e4.3 e3.2h e3.1h 11 12 e4.2 11 9 e4.5 e3.5 e4.8 12 e1.2 9h 12h e4.2h e0.4 e2.6 e3.4 2 14 11 e3.7 14 2 2 e1.8 e1.11 e0.1 2 14 6 e3.4h e1.5 2 3 4 e1.11 13 8 e3.7 14 13 8 e3.0 4 2 e2.3 10 0 5 e1.8 3 4 4 10 e4.9 e3.7 4 14 5 13 3 e4.0 e1.11 13 0 5 8 6 4 8 e2.4 e2.11 0 3 3 e4.3 13 6 0 e3.8 e4.0 e2.6 12 4 10 4 e1.3 e3.2 e0.7 9 13 9 e0.11 e0.14 5 e1.1 e3.8 e4.3 e4.1 e4.9 1 e4.2 e3.1 e4.1 e4.9 12 e1.3 e2.12 12 e0.9 12 e1.1 e2.6 e0.11 1 e0.14 e4.1 1 1 9 e3.2 e4.2 e3.1 e3.4 e3.3 9 1 e4.1 e0.9 1 e1.6 e2.12");
        }

        public void ExportAssignment(string assignmentStr) {
            (info.Assignment, info.IsHotelStayAfterTrip) = ParseHelper.ParseAssignmentString(assignmentStr, instance);
            List<Trip>[] driverPaths = TotalCostCalculator.GetPathPerDriver(info);

            JObject jsonObj = new JObject();
            jsonObj["drivers"] = CreateDriversJArray(driverPaths);

            string jsonString = jsonObj.ToString();
            string fileName = Path.Combine(Config.OutputFolder, "visualise.json");
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

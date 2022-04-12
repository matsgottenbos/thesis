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

            //ExportAssignment("3h 13 14h 9 e1.4 9 8 e4.3 6 12 e0.4 9 9 10 8 8 e0.7 12 2 e2.6 6 9 5 2 2 6 7 1 4 2 11 0 0 12 e4.7 1 0 2 7 4 1 e2.3 1 4 4 4h 0 6h 0 2 3 1 2h 3 0 14h 10 e0.14 10 0 10h 0 8 13 13 13 e3.5h 5 12 6h 9 9 4h 0 9 11 0 7 3 1 3 8 8 9h 3 e4.3 7 2 e2.6 2 1 e3.8h 2h 11 e0.6 8 1 11 14 5 e4.5h 14 14 7 3 e1.10 e0.5h 1h 10 13 11 14 13 13 10 10 10 14 10 14 12 6h e4.7 e0.3 12 4 e2.3 e4.9 0 9h e4.3 e4.0 e3.5 e3.8 4 4 4h 8 8 1 8 11 e2.6 e2.4h 5 e0.5 5 0 3 1 e4.4 8 8 11 8 5 5 13 11 13 0 0 14 1 e0.6 7 e3.3 e3.10h 6 14 e1.10 8 13 11h e4.5 1 e1.4 5 6 e1.6 8 e1.5 e2.10 e3.2 e1.1h 6 e1.8 2 7 2 7 3 2 12 13 8 7h 10 14h 5 12 10 6 3 10 5h 2 12 6h e3.9h 10 e2.11 e0.15 12 2 10 e4.7 e1.11 2 e0.0h 10 12h e4.0 4 4 0 9 4 0 10 9 4 e2.4 e1.0h 0 9 4h e2.3 1 11 9 11 0 13 11 11 5 14 1 11 11 3 1 5 13 14 5 8 14h 13 6 5 e0.13h 11h 6 8 13 1 6 e3.10h 13 13h 7 6 6 7 2 e4.1 e0.14 7 10 2 e3.6 e2.0h e4.1h e1.4 e3.9 e4.3 2 7 12 12 e1.1 4 4 10 0 9 0 2h 12 e3.8h e0.3 12 7h 10 10h e0.7 4 e0.0h 0 12 e1.5 4h 3 12 3 e0.17h 3 e0.4 3 e2.9h 11 14 e2.1 1 11 e2.4 3 e0.3h e3.3 11 6 8 e1.6 3 8 13 1 14 13 6 8 5 1 e0.10 13 8 6 e2.10 8 6 13 11 1 11 8 13 6 8 14 e2.0 13 1h 5 2 9 e0.13 2h 10 13 8 6 e4.9 14h e1.0 10 11 11 7 5 7 10 10 5 9 e2.6 7 7 5h 4 7 7 e0.11 4 0 e2.11 e2.12 e3.10 12 7 e3.1 e4.5 0h 4 e2.4 7 e0.0 e4.5h e0.17 7h 12 12 1h e4.1 12h 8 e3.2 e0.3 2 10 10 e1.2h e3.9 e3.7h e3.8 6 11 6 10 e0.9 13 14 13 11 3 9 11 13 9 6 10 e0.1 11 14 3 11 5 13 9 6 9 9 3 14 10 e4.9 e1.9 14 e4.2 11 e0.4h 13 6 e0.10 4 7 0 e2.4 14 5 9 4 9 0 0 e1.6h 13 e0.16 e2.9h 11 7 8 9 3 0 14 11 6 7h 0h 12 12 5h 4 3 13 e2.11 9h 3h e4.4 e2.11 e1.3 e2.10 1 8 e1.10 e2.0 e3.6h 1 12 e3.3 4 2 e4.5 e4.0h e1.0 12 e0.2 2 12h e3.4 e4.8h e2.10 e1.11 1 2 e0.7 e3.2 2h 1 1 e1.7 e1.0 e0.13h e1.9 1 e3.2 e3.8 e3.7 e0.1 14 14 11 7 13 e0.15 6 7 13 e0.4 0 6 e4.3 10h 13 e0.5 3 6 0 0h e1.2 e1.4 9 6 7 5 8 e1.10 e1.3 8 3 13 5 9 7 6 9 8 e3.5 13 e3.1 6 e4.2 7 4h 13 3 7 6 5 13 e1.6 9 9 e1.5 e2.9 2 9 2 8 e3.6 12 9 e4.1 5 e4.6 e2.4 8 5 14 8 12 e4.5 e0.14 e0.8 1 10 1 e4.7 e0.2 12 2 e0.13 10 2 2 11 e3.9 14 11 e4.0 e4.8 0 2 10 2 11 0 4");

            ExportAssignment("e0.2 13 e0.15 7 e0.14 e1.4 7 9 e0.2 11 13 e1.4 e0.15 3 7 7 9 11 7 13 e0.14 3 e0.2 7 14 e0.14 3 e2.12 1 1 e0.5 7 e3.10 14 e1.4 11 3h 1 e4.6 9 11 e1.8 11 e3.10 e3.10 14 e2.12 9 e0.5 0 14 4 0 e3.10 14 0 1 4 1 8 4 8 e3.2 e3.4 8 e3.4 8 4 e3.4 e3.2 e4.4 13 10 13 e1.3 1 e2.0h e3.1 e3.9 10 1 14 14 3 1 12 e1.3 2 e4.1 2 10 13 e0.7h e3.1 e4.4h 14 10 e3.1 1 e3.9h e3.3h 1 1 2 13h 14h e1.4 12 3 2 12 10 0 0 3 2h e4.1h 0 e0.16 12 0 e1.4 9 e0.16 0 0 e1.1 9 e4.6 e3.9 e0.11 6 e2.0 e1.6 4 4 4 14 e2.2 10 e3.2 6 e1.8 14 4 e2.3 10 14 e3.9h e1.10 e4.4 9 9h e4.8 e0.1 10 10 11 e1.1 11 14 14 e4.6 5 e1.10 e4.7 6 e2.2h e0.11h e2.3 e0.1 14 11 4h e1.5 e1.6 e3.3 e2.12 5 e2.1 6h 14 2 e3.1 e1.11 5 10 e3.2 13 e4.8h 13 e0.7 e3.2 2 e1.8 14h e2.10 11 e4.1 e4.7h 2 11 5 e2.11 11 12 2 e3.3h 5 e2.1 11h e2.10 e2.11 12 13 e2.12 e1.5 e3.1 13 e0.10 e2.10 e4.1 e3.0 2h 8 e2.11 e1.11 8 e2.4h 12 8 e0.11 e4.8 e3.6h 6 e0.11 9 8h e3.0h 10 e0.10h 10 6 9 4 4 e1.7 e0.11 e4.8 4 4 e2.2 7 10 9 e3.9 9 e4.8h e3.9 10 e1.5 e1.7h e4.2 5 6h e3.9 14 11 1 e1.1h 9h e2.6 e3.3 7 7 10h 14 4h 5 0 e2.2 e3.3 e4.2 e2.6 e4.7h e1.2 1 e1.5 14 7h 0 0 e3.3h 2 2 11 5h 3 2 14 0 e2.9h e3.4 0 1 e3.0 e0.17 e4.6 e3.7 e3.0 e2.4 2 3 e0.17 e3.2 2 0 e1.2h 12 2h 12 e2.7h 3 3 6 8 e2.4 e3.6 e4.6 e3.4 e0.17 3h 9 10 4 e3.7h 8 4 10 e0.10 10 4 8 e2.0 e4.5 e4.0 4 10 8 12 e4.4 4 8 9 6 e3.6 8 4 e1.8 8 6 4 e1.8 9 10 e4.8 e4.0 e1.7 12h 9 8h e4.5h e0.10h e4.4 e4.8 e3.3 e4.0 4 4 e1.1 10 5 e3.6h e0.9 e0.3 e1.8h e2.0h e4.7 5 7 7 5 e4.7 e1.7 e0.13 e4.8 e4.7h e2.2 e1.1 e0.9 e2.3 5 e0.3 e0.9 e0.13 3 7 e2.3h e4.9 e0.3 e1.2 5h 3 7 e0.13h 3 e2.2h 3h e1.9h e4.2 2 2 e2.7 e1.2h e3.0h e4.6 13 1 e2.0 8 e3.6 e4.9h 0 13 1 12 13 1 e1.10 13 2 e3.5 8 1 0 e4.2 1 e4.5 e2.9 2 13 2 e3.1 e3.7 6 e1.3 e2.0 e4.7 12 e2.12 11 e2.6 e4.2 13h e0.10 0 6 8h 12 e3.6 e4.7 0 1h 0h 5 5 e1.8 2h e4.6h 7 e3.1 6 e2.7h e1.11 e3.5 5 e4.4 6h e4.5 e0.9 5 e4.0 11 e2.9 e4.3 12h e1.10 e0.7 14 e3.5 e0.10 e0.11h e1.3 e3.2 7 5 e3.1 e4.4 14 e4.7h 11h e0.17 e1.11 7h e4.9 e0.13 e2.1 e0.17 e4.3 10 e0.9 e4.4h e0.1 e4.0 e0.15 10 4 e0.7 14 10 4 4 14 e3.2h e2.2 10 3 e0.12 e3.8 e3.0 e0.17h 9 9 e2.3 e2.1 1 4 2 e0.15 1 e1.2 6 2 e2.3 10 1 e1.0 9 3 6 6 1 13 e2.2 e2.4 3 e1.9 e1.4 9 e1.5 2 9 e0.12 e1.9 e3.6 8 6 e3.6 e2.4 e3.8 9 e1.4 6 e4.7 3 13 2 e4.6 3 9 e1.9 1 8 1 1 e1.0 e2.7 e1.2 1 12 11 13 e3.1 e0.11 e3.2 e2.4 e4.2 7 5 8 e0.5 e1.4 0 e4.5 e4.7 5 e4.5 7 e4.2 11 e3.2 12 e0.11 12 7 0 0 e3.1 e4.6 e0.5 5 e4.4 e0.17 e3.1 11 7 11 5 0 e4.5");
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

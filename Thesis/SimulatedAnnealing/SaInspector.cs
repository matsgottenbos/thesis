using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Thesis {
    class SaInspector {
        readonly Instance instance;
        readonly SaInfo dummyInfo;

        public SaInspector(Instance instance) {
            this.instance = instance;

            dummyInfo = new SaInfo(instance, null, null);
            dummyInfo.PenaltyFactor = 1f;

            InspectAssignment("6 13 7 1 8 14 9 10h 5 14 7 12 9 3 2 2 7 7 11 10 11 5 10 0 14 2 3 7 5 12 2 7 4 9 11 10 9h 0 11 5 12 2 14 3 8 0 10h 2 12 8 0 5 13 0 4 1 3 7 12 14 0 13 8 5 1 4 12 14 2 5 8 6 e2.7 9 2 9 10 0 13 5 4 7 11 0 9 2 3 8 13 5 3 6 5 2 14 9 0 0 6 13 4 7 e0.6 3 10 7 1 13 12 11 0 9 6 4 13 9 12 2 e2.2 14 10 3 14 10 3 11 e2.7 12 7 3 0 14 9 4 10 8 2 1 5 12 2 10 13 8 11 8 7 14 6 9 12 13 6 7 4 14 6 0 10 3 8 12 11 11 1 7 14 13 12 2 e0.3 0 6 5 8 1 11 13 e2.2 7 14 12 3 5 9 12 14 5 3 7 4 10 0 e2.0 11 7 3 7 6 4 8 9 1 5 13 2 7 12 2 11 12 9 0 14 1 5 2 11 3 1 14 7 13 4 10 2 10 2 6 12 11 8 7 13 4 11 3 8 12 0 e2.0 6 13 11 14 0 9 5 8 10 2 13 1 12 3 2 e2.2 12 14 10 12 3 0 11 7 14 11 4 10 4 13 0 11 2 12 2 1 9 3 4 10 10 5 6 1 e0.2 1 8 7 14 5 0 2 11 e1.4 13 14 12 9 5 3 7 2 3 9 5 11 3 11 0 6 10 6 0 12 7 2 8 4 13 6 11 14 10 e0.1 1 8 9 4 10 4 11 2 14 2 10 7 1 8 8 6 5 0 8 12 3 6 7 5 e0.1 4 3 13 6 14 0 6 13 11 9 8 2 13 1 7 0 3 13 14 10 2 10 12 9 1 8 9 8 10 9 11 3 e1.4 8 5 2 14 7 6 13 4 13 11 14 9 12 e2.0 6 10 1 1 8 5 12 10 2 3 11 14 5 7 0 13 4 9 11 8 8 9 13 e1.4 6 1 0 3 10 8 14 5 13 12 3 2 11 9 6 8 1 11 6 0 e2.0 8 8 9 12 6 14 5 3 7 10 5 0 14 1 6 4 12 9 11 5 1 2 13 3 14 8 1 0 7 14 2 12 8h 0 9 10 1 2 13 12 9 10 13 11 5 6 10 10 11 9 6 2 9 14 4 0 13 11 14 5 5 3 6 14 10 12 2 6 9 11 13 11 12 0 3 2 9 0 14 13 6 11 1 14 6 11 7 11 0 6 8 3");

            Console.ReadLine();
        }

        void InspectAssignment(string assignmentStr) {
            string[] driverStrings = assignmentStr.Split();
            dummyInfo.Assignment = new Driver[instance.Trips.Length];
            dummyInfo.IsHotelStayAfterTrip = new bool[instance.Trips.Length];
            for (int tripIndex = 0; tripIndex < instance.Trips.Length; tripIndex++) {
                (Driver driver, bool isHotelStayAfter) = ParseDriver(driverStrings[tripIndex]);
                dummyInfo.Assignment[tripIndex] = driver;
                dummyInfo.IsHotelStayAfterTrip[tripIndex] = isHotelStayAfter;
            }

            List<Trip>[] driverPaths = TotalCostCalculator.GetPathPerDriver(dummyInfo);

            (double cost, double costWithoutPenalty, double basePenalty, int[] driversWorkedTime, int precedenceViolationCount, int shiftLengthViolationCount, int restTimeViolationCount, int contractTimeViolationCount, int invalidHotelCount) = TotalCostCalculator.GetAssignmentCost(dummyInfo);

            Console.WriteLine("Assignment: {0}\nCost: {1}\nCost without penalty: {2}\nBase penalty: {3}\nWorked times: {4}\n", assignmentStr, ParseHelper.ToString(cost), ParseHelper.ToString(costWithoutPenalty), ParseHelper.ToString(basePenalty), ParseHelper.ToString(driversWorkedTime));

            for (int driverIndex = 0; driverIndex < driverPaths.Length; driverIndex++) {
                Driver driver = dummyInfo.Instance.AllDrivers[driverIndex];
                Console.WriteLine("Driver {0}: {1}", driver.GetId(), ParseHelper.DriverPathToString(driverPaths[driverIndex], dummyInfo));
            }
        }

        (Driver, bool) ParseDriver(string driverStr) {
            Driver driver;
            if (driverStr[0] == 'e') {
                // External driver
                int typeIndex = int.Parse(Regex.Replace(driverStr, @"^e(\d+)\.(\d+)h?$", "$1"));
                int indexInType = int.Parse(Regex.Replace(driverStr, @"^e(\d+)\.(\d+)h?$", "$2"));
                driver = instance.ExternalDriversByType[typeIndex][indexInType];
            } else {
                // Internal driver
                int internalDriverIndex = int.Parse(Regex.Replace(driverStr, @"(\d+)h?", "$1"));
                driver = instance.InternalDrivers[internalDriverIndex];
            }
            bool isHotelStayAfter = Regex.Match(driverStr, @"h$").Success;
            return (driver, isHotelStayAfter);
        }
    }

    class DebugObj {
        public readonly int Value;
        public DebugObj Prev;

        public DebugObj(int value, DebugObj prev) {
            Value = value;
            Prev = prev;
        }
    }
}

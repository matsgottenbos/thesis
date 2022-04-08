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

            InspectAssignment("7 0 4 14 8 11 5 4 4h 1 6 13 9 12 0h 3 8h 2 2h 4h 11 13 10 13 5h e2.11 9 e3.0 8 8 12 10 0h 6 7 14 11 3 3h 14 8 12 11h 6 8 e0.7 2 7 12 8 1 e3.11 12 1 2 e2.3 8 8h 7h 2 e2.3 e0.12h 13 2h e2.10 5 10 13 e4.14 4 5h 6 e2.2 e2.12 14 14 0 4 9 4 6 9 e2.7 3 14 13h 14 e1.6 0 4 14 0 4 14 6 e1.1 3 11 0 7 9 6 4 e3.5 1 9 7 0 1 6 e0.8h 10 8 12 0 10 1 14 1 8 11 6 8 11 9h 11 7 14 7 3h 12 6h 8 12h 11 0h e1.12h 2 11h 5 e1.8 e4.16 8 7 5 7h 1h e4.3 8 2 e2.14 13 8 5 e1.3 8 13 e4.15h 13 e0.18 e1.0 2 2 e0.12 e4.1 5 2 5 6 e0.7 e2.12 13 6 10 e3.8 e2.6 9 e2.2h e0.8 0 13h 10 e4.2h 12 e4.14 e2.9 14 3 6 9 6 0 11 e0.3 10 12 1 12 1 6h 12 e1.6 3 e2.13 3 9 e1.5 11 9 14 e1.12 e2.11h 4 7 8 10 11 1 9 3 7 5 3 e3.7 0h 10 4 11 3 14 12 8 5 2 12h 3 3h 1h 8 2 e1.9 7 2 5 13 2 e0.13 e0.0 e4.15 e1.3 4 13 5 8 7 4 e1.3 8 7 5 8 e4.16 2 e3.9h e0.11h e4.2 2 7 5 7h e4.7 5h e3.8h 13 2h e0.15 e0.17 e2.6 13 6 e2.4 e0.6h 13 13h 0 e1.8 e3.6 12 6 9 12 6 e0.5h 14 e2.12 e3.10 e0.12 9 6h 0h 10 12 1 e0.7 e1.2 3 14 e1.15 14 11 10 12 10 11 e2.2 1 9 4 14 14h e3.0 10 3 e4.3 1 9 4 10 7 3 e2.8 e4.16h e0.19 e1.16 e0.15 1h 7 9 e3.1h 11 2 e3.8 5 11 3 4 2 7 e3.8 e3.9 e2.6 3 8 2 4 11h e2.11 13 2 8 5 e3.7 13 4h 8 2 8 e2.5 13 6 8 6 e1.7h 5 13 e1.13 5 0 6 5h 0 e2.6 6 e0.11 e4.11h 12 13 e0.6 0 13 10 13 12h 10 6h e4.16 e1.11 14 e2.12 9 e1.8 11 e3.4 9 10 e3.1 11 e2.2 14 11 e0.5 11 e3.10 e4.13 4 9 7 4 10 1 3 e2.10 11 3 8 7 10h 8 8 e1.10 e0.17 7 1 9 11 3h 7 2 9 4 2 e3.6 7 7 11 2 4 e0.6 8 13 13 2 8 e2.4 0 0 4h e1.3 6 7h e0.1h e4.1 0 13 12 e4.5 13 0 6 e4.0 2h 13 5 14 6h e1.7 5 12 0 e3.12 12 e4.11 5 14 14 e1.2 14 e1.3 12 5 3 14 11 5 14 10 3 3 1 10 4 7 7 8 11h 4 1 8 1 9 8 7 10 7 3 e4.5 10 3 4 e0.1 8 1 6 0 13 5 11 5 11 2 11 11 5 10");

            InspectAssignment("7 0 4 14 8 11 5 4 4h 1 6 13 9 12 0h 3 8h 2 2h 4h 11 13 10 13 5h e2.11 9 e3.0 8 8 12 10 0h 6 7 14 11 3 3h 14 8 12 11h 6 8 e0.7 2 7 12 8 1 e3.11 12 1 2 e2.3 8 8h 7h 2 e2.3 e0.12h 13 2h e2.10 5 10 13 e4.14 4 5h 6 e2.2 e2.12 14 14 0 4 9 4 6 9 e2.7 3 14 13h 14 e1.6 0 4 14 0 4 14 6 e1.1 3 11 0 7 9 6 4 e3.5 1 9 7 0 1 6 e0.8h 10 8 12 0 10 1 14 1 8 11 6 8 11 9h 11 7 14 7 3h 12 6h 8 12h 11 0h e1.12h 2 11h 5 e1.8 e4.16 8 7 5 7h 1h e4.3 8 2 e2.14 13 8 5 e1.3 8 13 e4.15h 13 e0.18 e1.0 2 2 e0.12 e4.1 5 2 5 6 e0.7 e2.12 13 6 10 e3.8 e2.6 9 e2.2h e0.8 0 13h 10 e4.2h 12 e4.14 e2.9 14 3 6 9 6 0 11 e0.3 10 12 1 12 1 6h 12 e1.6 3 e2.13 3 9 e1.5 11 9 14 e1.12 e2.11h 4 7 8 10 11 1 9 3 7 5 3 e3.7 0h 10 4 11 3 14 12 8 5 2 12h 3 3h 1h 8 2 e1.9 7 2 5 13 2 e0.13 e0.0 e4.15 e1.3 4 13 5 8 7 4 e1.3 8 7 5 8 e4.16 2 e3.9h e0.11h e4.2 2 7 5 7h e4.7 5h e3.8h 13 2h e0.15 e0.17 e2.6 13 6 e2.4 e0.6h 13 13h 0 e1.8 e3.6 12 6 9 12 6 e0.5h 14 e2.12 e3.10 e0.12 9 6h 0h 10 12 1 e0.7 e1.2 3 14 e1.15 14 11 10 12 10 11 e2.2 1 9 4 14 14h e3.0 10 3 e4.3 1 9 4 10 7 3 e2.8 e4.16h e0.19 e1.16 e0.15 1h 7 9 e3.1h 11 2 e3.8 5 11 3 4 2 7 e3.8 e3.9 e2.6 3 8 2 4 11h e2.11 13 2 8 5 e3.7 13 4h 8 2 8 e2.5 13 6 8 6 e1.7h 5 13 e1.13 5 0 6 5h 0 e2.6 6 e0.11 e4.11h 12 13 e0.6 0 13 10 13 12h 10 6h e4.16 e1.11 14 e2.12 9 e1.8 11 e3.4 9 10 e3.1 11 e2.2 14 11 e0.5 11 e3.10 e4.13 4 9 7 4 10 1 e2.0 e2.10 11 e2.1 8 7 10h 8 8 e1.10 e0.17 7 1 9 11 e0.2 7 2 9 4 2 e3.6 7 7 11 2 4 e0.6 8 13 13 2 8 e2.4 0 0 4h e1.3 6 7h e0.1h e4.1 0 13 12 e4.5 13 0 6 e4.0 2h 13 5 14 6h e1.7 5 12 0 e3.12 12 e4.11 5 14 14 e1.2 14 e1.3 12 5 3 14 11 5 14 10 3 3 1 10 4 7 7 8 11h 4 1 8 1 9 8 7 10 7 3 e4.5 10 3 4 e0.1 8 1 6 0 13 5 11 5 11 2 11 11 5 10");

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

            (dummyInfo.Cost, dummyInfo.CostWithoutPenalty, dummyInfo.Penalty, dummyInfo.DriversWorkedTime, dummyInfo.PrecedenceViolationCount, dummyInfo.ShiftLengthViolationCount, dummyInfo.RestTimeViolationCount, dummyInfo.ContractTimeViolationCount, dummyInfo.InvalidHotelCount) = TotalCostCalculator.GetAssignmentCost(dummyInfo);

            string penaltyString = "-";
            if (dummyInfo.Penalty > 0) {
                List<string> penaltyTypes = new List<string>();
                if (dummyInfo.PrecedenceViolationCount > 0) penaltyTypes.Add("Pr " + dummyInfo.PrecedenceViolationCount);
                if (dummyInfo.ShiftLengthViolationCount > 0) penaltyTypes.Add("SL " + dummyInfo.ShiftLengthViolationCount);
                if (dummyInfo.RestTimeViolationCount > 0) penaltyTypes.Add("RT " + dummyInfo.RestTimeViolationCount);
                if (dummyInfo.ContractTimeViolationCount > 0) penaltyTypes.Add("CT " + dummyInfo.ContractTimeViolationCount);
                if (dummyInfo.InvalidHotelCount > 0) penaltyTypes.Add("IH " + dummyInfo.InvalidHotelCount);
                string penaltyTypesStr = string.Join(", ", penaltyTypes);

                penaltyString = string.Format("{0} ({1})", ParseHelper.ToString(dummyInfo.Penalty, "0"), penaltyTypesStr);
            };

            Console.WriteLine("Assignment: {0}", assignmentStr);
            Console.WriteLine("Cost: {0}", ParseHelper.ToString(dummyInfo.Cost));
            Console.WriteLine("Cost without penalty: {0}", ParseHelper.ToString(dummyInfo.CostWithoutPenalty));
            Console.WriteLine("Penalty: {0}", penaltyString);
            Console.WriteLine("Worked times: {0}", ParseHelper.ToString(dummyInfo.DriversWorkedTime));
            Console.WriteLine();

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

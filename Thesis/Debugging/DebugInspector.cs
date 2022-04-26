using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Thesis {
    class DebugInspector {
        readonly Instance instance;
        readonly SaInfo info;

        public DebugInspector(Instance instance) {
            this.instance = instance;

            info = new SaInfo(instance, null, null);

            Console.WriteLine(instance.Trips.Sum(trip => trip.Duration));

            InspectAssignment("e1.10 2 14 10 e3.10 3 e1.8 14 2 e3.5 5 3 3 14 10 e1.10 e3.10 e1.8 e1.0 6 e4.6 2h e4.0 10 10h e3.10 5 e3.5 6 6 3 5 e3.1h e4.2 6 e4.5 7 e4.6 e1.0 5 e3.5 e3.2 e4.2 1 e4.0 1 7 6 7 e2.6 1 e3.3 6 e4.3 1 e2.6 e4.5 e4.2 e3.3 7 e3.3 7 1 e3.2 7 7 e0.13h 2 2 11 10 11 e0.7 8 e1.8 e1.7 e4.5 e3.1 8 10 e2.7 11 e1.3 9 8 e4.2 e2.4 12 e0.7 12 6 e3.1 e3.6 e1.5 12 e2.11 9 e1.8 e4.5 8 e1.6 e3.1h e1.7h 11h e4.5 e4.8 e2.7 9 e1.3h 6 e2.9h 8 12 12h 9 6 9 7 9h 7 14 e0.9 e1.6h e0.9 14 7 14 e1.1 1 e4.8 10 14h e0.13 e1.9 13 e1.1 e0.9 1 4 1 13 e4.6 e3.10 e1.3 1 e0.11 e4.3 e4.4 e4.9 4 e1.5 6 6 13 6 9 e0.12 11 10 e3.1 1 e1.1 e1.0 0 9 e4.6 1 e0.11 e4.8 e1.4 e1.10 3 e3.10 4 11 e4.8h 10h e4.3 e2.1 e3.1 3 13 e4.7 e1.11 e2.9h 0 6 e1.3 e1.9 e4.4h e4.6 e0.3 e1.0 12 e1.7 e3.2 e0.11 4h 12 e4.1 12 6 e1.5h 3 11 e3.1 e4.7 12 7 e3.2 8 12 e2.1 7 e1.11 8 e1.6 e1.4 e1.7 e1.6h 12 e3.2h e4.1h 7 7 8 14 e2.12h 8h 14 e2.11 14 0 e4.8h 4 10 5 e0.6 13 e0.10 e2.11 1 10 5 10 0 4h e4.4 14 10 1 0 e1.11 10 13 e2.7 13 0h e0.6 1h e0.10h e4.5 e0.11 13 5 e4.6 10 2 11 e1.4h 10 e1.5 5h 10 e2.9 e2.10 e3.2 e2.4 e4.4 e2.12 2 e2.7 e3.2 e2.6 e2.12 e1.11 e2.10 e2.4 e1.5 e3.4 11 11 e4.5h 2 2h e4.1 e0.4 e4.6 e3.2 e3.5 11h e4.1 e1.6 8 e2.9 e2.10 e1.5 e4.7 e0.0 e0.1 e3.4 8 e0.4 e2.6 e3.0 4 e4.7 e1.0 e3.5 e1.8 13 e3.9h e0.0 7 0 4 8 e3.6 e0.6 e4.7 e4.8 8h e1.6 5 4 e3.0 14 4 1 e1.8h 1 5 14 e0.12 7 13 4 9 12 13 14 4 9 13 1 0 14 4 7 4h e0.6h 14 9 1 e3.6 13h 3 e4.6 e1.1 12 0h 9 7h e2.7 5h e0.10h e0.12 e2.4 14 e4.8h 2 e1.4 1 12 2 3h e4.2 14 9 11 11 6 e3.4 e1.1 e2.4 e4.6 e1.4 e3.4 2h e4.2 11 e2.7 e4.5 e4.2 e2.4 e1.1 6 e3.5h e4.5 e0.1 11 6 6 11 e3.4 e1.8 11 e1.8 e0.7h e0.11 e3.0 0 e1.5h e0.1 5 e4.4 13 e3.0 4 e0.2 e1.8 e4.5h e3.9 3 5 e1.0 8 e0.11 e1.7 13 e1.8 7 0 e0.6 e1.1 4 5 e1.9 8 13 3 13 3 e2.3 e1.8h e3.0 e0.2 4 e2.11 e0.6 e1.0 5 8 e4.3 8 e3.2 e4.4 4 e2.5 e4.0 7 e4.4 e3.9 e3.9 e0.11h 9 e3.3h 12 e1.7 e2.11 e2.4h 0h e0.14 13h e3.1 5 4h 9 3 e0.14 11 e4.8 e2.6 7 e1.9 8 12 e4.2 e1.1h 8h e1.2 e0.10 12 e4.8 9 e4.0h e4.3 e3.2h e3.1h 11 12 e4.2 11 9 e4.5 e3.5 e4.8 12 e1.2 9h 12h e4.2h e0.4 e2.6 e3.4 2 14 11 e3.7 14 2 2 e1.8 e1.11 e0.1 2 14 6 e3.4h e1.5 2 3 4 e1.11 13 8 e3.7 14 13 8 e3.0 4 2 e2.3 10 0 5 e1.8 3 4 4 10 e4.9 e3.7 4 14 5 13 3 e4.0 e1.11 13 0 5 8 6 4 8 e2.4 e2.11 0 3 3 e4.3 13 6 0 e3.8 e4.0 e2.6 12 4 10 4 e1.3 e3.2 e0.7 9 13 9 e0.11 e0.14 5 e1.1 e3.8 e4.3 e4.1 e4.9 1 e4.2 e3.1 e4.1 e4.9 12 e1.3 e2.12 12 e0.9 12 e1.1 e2.6 e0.11 1 e0.14 e4.1 1 1 9 e3.2 e4.2 e3.1 e3.4 e3.3 9 1 e4.1 e0.9 1 e1.6 e2.12");

            InspectAssignment("e1.10 2 14 10 e3.10 3 e1.8 14 2 e3.5 5 3 3 14 10 e1.10 e3.10 e1.8 e1.0 6 e4.6 2h e4.0 10 10h e3.10 5 e3.5 6 6 3 5 e3.1h e4.2 6 e4.5 7 e4.6 e1.0 5 e3.5 e3.2 e4.2 1 e4.0 1 7 6 7 e2.6 1 e3.3 6 e4.3 1 e2.6 e4.5 e4.2 e3.3 7 e3.3 7 1 e3.2 7 7 e0.13h 2 2 11 10 11 e0.7 8 e1.8 e1.7 e4.5 e3.1 8 10 e2.7 11 e1.3 9 8 e4.2 e2.4 12 e0.7 12 6 e3.1 e3.6 e1.5 12 e2.11 9 e1.8 e4.5 8 e1.6 e3.1h e1.7h 11h e4.5 e4.8 e2.7 9 e1.3h 6 e2.9h 8 12 12h 9 6 9 7 9h 7 14 e0.9 e1.6h e0.9 14 7 14 e1.1 1 e4.8 10 14h e0.13 e1.9 13 e1.1 e0.9 1 4 1 13 e4.6 e3.10 e1.3 1 e0.11 e4.3 e4.4 e4.9 4 e1.5 6 6 13 6 9 e0.12 11 10 e3.1 1 e1.1 e1.0 0 9 e4.6 1 e0.11 e4.8 e1.4 e1.10 3 e3.10 4 11 e4.8h 10h e4.3 e2.1 e3.1 3 13 e4.7 e1.11 e2.9h 0 6 e1.3 e1.9 e4.4h e4.6 e0.3 e1.0 12 e1.7 e3.2 e0.11 4h 12 e4.1 12 6 e1.5h 3 11 e3.1 e4.7 12 7 e3.2 8 12 e2.1 7 e1.11 8 e1.6 e1.4 e1.7 e1.6h 12 e3.2h e4.1h 7 7 8 14 e2.12h 8h 14 e2.11 14 0 e4.8h 4 10 5 e0.6 13 e0.10 e2.11 1 10 5 10 0 4h e4.4 14 10 1 0 e1.11 10 13 e2.7 13 0 e0.6 1h e0.10h e4.5 e0.11 13 5 e4.6 10 2 11 e1.4h 10 e1.5 5h 10 e2.9 e2.10 e3.2 e2.4 e4.4 e2.12 2 e2.7 e3.2 e2.6 e2.12 e1.11 e2.10 e2.4 e1.5 e3.4 11 11 e4.5h 2 2h e4.1 e0.4 e4.6 e3.2 e3.5 11h e4.1 e1.6 8 e2.9 e2.10 e1.5 e4.7 e0.0 e0.1 e3.4 8 e0.4 e2.6 e3.0 4 e4.7 e1.0 e3.5 e1.8 13 e3.9h e0.0 7 0 4 8 e3.6 e0.6 e4.7 e4.8 8h e1.6 5 4 e3.0 14 4 1 e1.8h 1 5 14 e0.12 7 13 4 9 12 13 14 4 9 13 1 0 14 4 7 4h e0.6h 14 9 1 e3.6 13h 3 e4.6 e1.1 12 0h 9 7h e2.7 5h e0.10h e0.12 e2.4 14 e4.8h 2 e1.4 1 12 2 3h e4.2 14 9 11 11 6 e3.4 e1.1 e2.4 e4.6 e1.4 e3.4 2h e4.2 11 e2.7 e4.5 e4.2 e2.4 e1.1 6 e3.5h e4.5 e0.1 11 6 6 11 e3.4 e1.8 11 e1.8 e0.7h e0.11 e3.0 0 e1.5h e0.1 5 e4.4 13 e3.0 4 e0.2 e1.8 e4.5h e3.9 3 5 e1.0 8 e0.11 e1.7 13 e1.8 7 0 e0.6 e1.1 4 5 e1.9 8 13 3 13 3 e2.3 e1.8h e3.0 e0.2 4 e2.11 e0.6 e1.0 5 8 e4.3 8 e3.2 e4.4 4 e2.5 e4.0 7 e4.4 e3.9 e3.9 e0.11h 9 e3.3h 12 e1.7 e2.11 e2.4h 0h e0.14 13h e3.1 5 4h 9 3 e0.14 11 e4.8 e2.6 7 e1.9 8 12 e4.2 e1.1h 8h e1.2 e0.10 12 e4.8 9 e4.0h e4.3 e3.2h e3.1h 11 12 e4.2 11 9 e4.5 e3.5 e4.8 12 e1.2 9h 12h e4.2h e0.4 e2.6 e3.4 2 14 11 e3.7 14 2 2 e1.8 e1.11 e0.1 2 14 6 e3.4h e1.5 2 3 4 e1.11 13 8 e3.7 14 13 8 e3.0 4 2 e2.3 10 0 5 e1.8 3 4 4 10 e4.9 e3.7 4 14 5 13 3 e4.0 e1.11 13 0 5 8 6 4 8 e2.4 e2.11 0 3 3 e4.3 13 6 0 e3.8 e4.0 e2.6 12 4 10 4 e1.3 e3.2 e0.7 9 13 9 e0.11 e0.14 5 e1.1 e3.8 e4.3 e4.1 e4.9 1 e4.2 e3.1 e4.1 e4.9 12 e1.3 e2.12 12 e0.9 12 e1.1 e2.6 e0.11 1 e0.14 e4.1 1 1 9 e3.2 e4.2 e3.1 e3.4 e3.3 9 1 e4.1 e0.9 1 e1.6 e2.12");

            Console.ReadLine();
        }

        void InspectAssignment(string assignmentStr) {
            (info.Assignment, info.IsHotelStayAfterTrip) = ParseHelper.ParseAssignmentString(assignmentStr, instance);
            List<Trip>[] driverPaths = TotalCostCalculator.GetPathPerDriver(info);

            (info.Cost, info.CostWithoutPenalty, info.Penalty, info.DriversWorkedTime, info.DriversShiftCounts, info.PrecedenceViolationCount, info.ShiftLengthViolationCount, info.RestTimeViolationCount, info.ContractTimeViolationCount, info.ShiftCountViolationAmount, info.InvalidHotelCount) = TotalCostCalculator.GetAssignmentCost(info);

            // Log assignment info
            Console.WriteLine("Assignment: {0}", assignmentStr);
            Console.WriteLine("Cost: {0}", ParseHelper.ToString(info.Cost));
            Console.WriteLine("Cost without penalty: {0}", ParseHelper.ToString(info.CostWithoutPenalty));
            Console.WriteLine("Penalty: {0}", ParseHelper.GetPenaltyString(info));
            Console.WriteLine("Worked times: {0}", ParseHelper.ToString(info.DriversWorkedTime));
            Console.WriteLine("Shift counts: {0}", ParseHelper.ToString(info.DriversShiftCounts));
            Console.WriteLine("Sum of worked times: {0}", info.DriversWorkedTime.Sum());
            Console.WriteLine();

            // Log driver penalties
            for (int driverIndex = 0; driverIndex < instance.AllDrivers.Length; driverIndex++) {
                Driver driver = instance.AllDrivers[driverIndex];
                List<Trip> driverPath = driverPaths[driverIndex];
                (_, _, double driverPenalty, int driverWorkedTime, int driverShiftCount, int precedenceViolationCount, int shiftLengthViolationCount, int restTimeViolationCount, int contractTimeViolationCount, int shiftCountViolationAmount, int invalidHotelCount) = TotalCostCalculator.GetDriverPathCost(driverPath, info.IsHotelStayAfterTrip, driver, info, false);

                if (driverPenalty > 0) {
                    Console.WriteLine("Driver {0} penalty: {1}", driver.GetId(), ParseHelper.GetPenaltyString(driverPenalty, precedenceViolationCount, shiftLengthViolationCount, restTimeViolationCount, contractTimeViolationCount, shiftCountViolationAmount, invalidHotelCount));
                }
            }
            Console.WriteLine();

            // Log driver paths
            //for (int driverIndex = 0; driverIndex < driverPaths.Length; driverIndex++) {
            //    Driver driver = info.Instance.AllDrivers[driverIndex];
            //    Console.WriteLine("Driver {0}: {1}", driver.GetId(), ParseHelper.DriverPathToString(driverPaths[driverIndex], info));
            //}
            //Console.WriteLine();
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

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

            InspectAssignment("3h 13 14h 9 e1.4 9 8 e4.3 6 12 e0.4 9 9 10 8 8 e0.7 12 2 e2.6 6 9 5 2 2 6 7 1 4 2 11 0 0 12 e4.7 1 0 2 7 4 1 e2.3 1 4 4 4h 0 6h 0 2 3 1 2h 3 0 14h 10 e0.14 10 0 10h 0 8 13 13 13 e3.5h 5 12 6h 9 9 4h 0 9 11 0 7 3 1 3 8 8 9h 3 e4.3 7 2 e2.6 2 1 e3.8h 2h 11 e0.6 8 1 11 14 5 e4.5h 14 14 7 3 e1.10 e0.5h 1h 10 13 11 14 13 13 10 10 10 14 10 14 12 6h e4.7 e0.3 12 4 e2.3 e4.9 0 9h e4.3 e4.0 e3.5 e3.8 4 4 4h 8 8 1 8 11 e2.6 e2.4h 5 e0.5 5 0 3 1 e4.4 8 8 11 8 5 5 13 11 13 0 0 14 1 e0.6 7 e3.3 e3.10h 6 14 e1.10 8 13 11h e4.5 1 e1.4 5 6 e1.6 8 e1.5 e2.10 e3.2 e1.1h 6 e1.8 2 7 2 7 3 2 12 13 8 7h 10 14h 5 12 10 6 3 10 5h 2 12 6h e3.9h 10 e2.11 e0.15 12 2 10 e4.7 e1.11 2 e0.0h 10 12h e4.0 4 4 0 9 4 0 10 9 4 e2.4 e1.0h 0 9 4h e2.3 1 11 9 11 0 13 11 11 5 14 1 11 11 3 1 5 13 14 5 8 14h 13 6 5 e0.13h 11h 6 8 13 1 6 e3.10h 13 13h 7 6 6 7 2 e4.1 e0.14 7 10 2 e3.6 e2.0h e4.1h e1.4 e3.9 e4.3 2 7 12 12 e1.1 4 4 10 0 9 0 2h 12 e3.8h e0.3 12 7h 10 10h e0.7 4 e0.0h 0 12 e1.5 4h 3 12 3 e0.17h 3 e0.4 3 e2.9h 11 14 e2.1 1 11 e2.4 3 e0.3h e3.3 11 6 8 e1.6 3 8 13 1 14 13 6 8 5 1 e0.10 13 8 6 e2.10 8 6 13 11 1 11 8 13 6 8 14 e2.0 13 1h 5 2 9 e0.13 2h 10 13 8 6 e4.9 14h e1.0 10 11 11 7 5 7 10 10 5 9 e2.6 7 7 5h 4 7 7 e0.11 4 0 e2.11 e2.12 e3.10 12 7 e3.1 e4.5 0h 4 e2.4 7 e0.0 e4.5h e0.17 7h 12 12 1h e4.1 12h 8 e3.2 e0.3 2 10 10 e1.2h e3.9 e3.7h e3.8 6 11 6 10 e0.9 13 14 13 11 3 9 11 13 9 6 10 e0.1 11 14 3 11 5 13 9 6 9 9 3 14 10 e4.9 e1.9 14 e4.2 11 e0.4h 13 6 e0.10 4 7 0 e2.4 14 5 9 4 9 0 0 e1.6h 13 e0.16 e2.9h 11 7 8 9 3 0 14 11 6 7h 0h 12 12 5h 4 3 13 e2.11 9h 3h e4.4 e2.11 e1.3 e2.10 1 8 e1.10 e2.0 e3.6h 1 12 e3.3 4 2 e4.5 e4.0h e1.0 12 e0.2 2 12h e3.4 e4.8h e2.10 e1.11 1 2 e0.7 e3.2 2h 1 1 e1.7 e1.0 e0.13h e1.9 1 e3.2 e3.8 e3.7 e0.1 14 14 11 7 13 e0.15 6 7 13 e0.4 0 6 e4.3 10h 13 e0.5 3 6 0 0h e1.2 e1.4 9 6 7 5 8 e1.10 e1.3 8 3 13 5 9 7 6 9 8 e3.5 13 e3.1 6 e4.2 7 4h 13 3 7 6 5 13 e1.6 9 9 e1.5 e2.9 2 9 2 8 e3.6 12 9 e4.1 5 e4.6 e2.4 8 5 14 8 12 e4.5 e0.14 e0.8 1 10 1 e4.7 e0.2 12 2 e0.13 10 2 2 11 e3.9 14 11 e4.0 e4.8 0 2 10 2 11 0 4");

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
            for (int driverIndex = 0; driverIndex < driverPaths.Length; driverIndex++) {
                Driver driver = info.Instance.AllDrivers[driverIndex];
                Console.WriteLine("Driver {0}: {1}", driver.GetId(), ParseHelper.DriverPathToString(driverPaths[driverIndex], info));
            }
            Console.WriteLine();
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

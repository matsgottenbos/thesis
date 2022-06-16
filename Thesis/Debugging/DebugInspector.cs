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

        public DebugInspector(Instance instance) {
            this.instance = instance;

            InspectAssignment("e1.0 16 e2.4 e4.13 16 e6.0 17 22 e4.11 17 10 2 7 21 16h 13 21h 17 22 11 0 9 10 17 19 13 6 15 2 22 11 e3.4h 9h 17 0 e4.7 13 11 e0.2 7 e6.3 10 8 13h e5.5h 10h 11 e6.5 e0.11 e4.7 e5.4 15h 0 e1.1 e6.2h 11 e2.1 e3.3 0h e2.7 18 e2.10 e4.4 6 e3.1 21 3 e1.2 e3.6h 18 21 6 e0.8 3 10 1 12 18 e4.2 16 14 12 1 e4.16 13 12 e2.4 5 0 22 3h 16 13 11 4 10 1h 0 13 e6.2 9 e3.2 22h e3.3h 14 e5.5 e1.0 0 19 e5.1h 14h e3.4 12 13 0 5 e0.12 15 10 16 19 e2.7 e1.0h e2.2h 5 11 e3.2 17 15 12h e4.1 0 e0.14 4 9 9 15 e4.7 e3.1 e2.7 e6.3 4h 9 19h 8 e0.10 17 7 2 e6.4 20h 17 e3.3 e0.4 8 7 8 e3.6 6 2 e2.4 e4.12 7 14 e5.1 23 22 12 3 2h 18 16 0 23h 1 3 10 5 e1.0 e6.2 e1.2 0 16 18 9h 1 1 14 22 e2.3 10 22 e3.0 16 12 5h 22h e5.1 e5.4 3 11 e0.5 e2.2 0h 11 18h e2.11 e5.5 e4.4 4 10 20 10 e3.2 19h e4.1 4 2 2h e5.5 20 e0.12 e4.15 20 9 e0.6 4 9 e0.6 e1.1 e5.2 18 23 21 5 e0.9 7 e1.2h e4.5 18 5 21 8 e6.5 21 11 23 15 17 7 5 22 e6.1 7 e5.2 21 e2.5 15 e2.0 e0.9 e1.1 12 7h 19 18h 10 23h 21 15 11 19 17 14 e5.1 10h e0.10 e5.2 0 15 22 21 e4.14 11 15 e5.1 19 e0.1h 0 e2.4 14 19h 20 2 e5.0 e1.2 2 2 e0.0 e0.11h e5.2 e1.1 e1.0 18 17 15 e4.6 6 13 8 13 21 e5.0 23 e4.12 7 13 18 e4.15 6 e5.2 e0.1 12 5 1 e1.0 e1.1h 14 1 19 5 5 18 17 1 23h 6 5 8 10 e0.11 5h e2.9h 7h 19 3 0 22 3 e1.2 e4.10 22 3h 5 22 e1.3 1 e0.2 e5.0 22 5 12 8 15 e2.9 4 21 2 17 12 20 2 8 23 4 13 20 15 14 9 21 5 6 2 1 13 7 e3.4 e1.1 14 20 e2.8 23 e5.4 5 14 9 15 4 16 e4.16 13 7 13 4 4 20h 14 6 e5.4 21 e1.0 9 13h e3.1 16h 3 7 e3.6 16 e0.12 e6.1 e4.13 20 e5.1 e2.0 11 13 3 4 16 e4.1 1 20 e5.3 16 e3.5 e5.1 13 1 1 18 13 e0.1 23 18 11 e5.1 e1.1 e2.1 e1.1 e3.5 e6.3");

            //InspectAssignment("e1.0 16 e2.4 e4.13 16 e6.0 17 22 e4.11 17 10 2 7 21 16h 13 21h 17 22 11 0 9 10 17 19 13 6 15 2 22 11 e3.4h 9h 17 0 e4.7 13 11 e0.2 7 e6.3 10 8 13h e5.5h 10h 11 e6.5 e0.11 e4.7 e5.4 15h 0 e1.1 e6.2h 11 e2.1 e3.3 0h e2.7 18 e2.10 e4.4 6 e3.1 21 3 e1.2 e3.6h 18 21 6 e0.8 3 10 1 12 18 e4.2 16 14 12 1 e4.16 13 12 e2.4 5 0 22 3h 16 13 11 4 10 1h 0 13 e6.2 9 e3.2 22h e3.3h 14 e5.5 e1.0 0 19 e5.1h 14h e3.4 12 13 0 5 e0.12 15 10 16 19 e2.7 e1.0h e2.2h 5 11 e3.2 17 15 12h e4.1 0 e0.14 4 9 9 15 e4.7 e3.1 e2.7 e6.3 4h 9 19h 8 e0.4 17 7 2 e6.4 20h 17 e3.3 e0.4 8 7 8 e3.6 6 2 e2.4 e4.12 7 14 e5.1 23 22 12 3 2h 18 16 0 23h 1 3 10 5 e1.0 e6.2 e1.2 0 16 18 9h 1 1 14 22 e2.3 10 22 e3.0 16 12 5h 22h e5.1 e5.4 3 11 e0.5 e2.2 0h 11 18h e2.11 e5.5 e4.4 4 10 20 10 e3.2 19h e4.1 4 2 2h e5.5 20 e0.12 e4.15 20 9 e0.6 4 9 e0.6 e1.1 e5.2 18 23 21 5 e0.9 7 e1.2h e4.5 18 5 21 8 e6.5 21 11 23 15 17 7 5 22 e6.1 7 e5.2 21 e2.5 15 e2.0 e0.9 e1.1 12 7h 19 18h 10 23h 21 15 11 19 17 14 e5.1 10h e0.10 e5.2 0 15 22 21 e4.14 11 15 e5.1 19 e0.1h 0 e2.4 14 19h 20 2 e5.0 e1.2 2 2 e0.0 e0.11h e5.2 e1.1 e1.0 18 17 15 e4.6 6 13 8 13 21 e5.0 23 e4.12 7 13 18 e4.15 6 e5.2 e0.1 12 5 1 e1.0 e1.1h 14 1 19 5 5 18 17 1 23h 6 5 8 10 e0.11 5h e2.9h 7h 19 3 0 22 3 e1.2 e4.10 22 3h 5 22 e1.3 1 e0.2 e5.0 22 5 12 8 15 e2.9 4 21 2 17 12 20 2 8 23 4 13 20 15 14 9 21 5 6 2 1 13 7 e3.4 e1.1 14 20 e2.8 23 e5.4 5 14 9 15 4 16 e4.16 13 7 13 4 4 20h 14 6 e5.4 21 e1.0 9 13h e3.1 16h 3 7 e3.6 16 e0.12 e6.1 e4.13 20 e5.1 e2.0 11 13 3 4 16 e4.1 1 20 e5.3 16 e3.5 e5.1 13 1 1 18 13 e0.1 23 18 11 e5.1 e1.1 e2.1 e1.1 e3.5 e6.3");
        }

        void InspectAssignment(string assignmentStr) {
            SaInfo info = ParseHelper.ParseAssignmentString(assignmentStr, instance);
            TotalCostCalculator.ProcessAssignmentCost(info);

            // Log assignment info
            info.TotalInfo.DebugLog(false);
            Console.WriteLine();

            var testTrip = info.Assignment.ToList().FindIndex(driver => driver == instance.ExternalDriversByType[0][0]); // 297

            var test = new AssignInternalOperation(297, instance.InternalDrivers[0], info);
            var test2 = test.GetCostDiff();

            //foreach (Trip trip in instance.Trips) {
            //    Console.WriteLine("{0}: {1}; {2}", trip.Index, trip.DutyName, trip.ActivityName);
            //}

            // Log driver penalties
            //for (int driverIndex = 0; driverIndex < instance.AllDrivers.Length; driverIndex++) {
            //    Driver driver = instance.AllDrivers[driverIndex];
            //    List<Trip> driverPath = info.DriverPaths[driverIndex];
            //    SaDriverInfo driverInfo = TotalCostCalculator.GetDriverInfo(driverPath, info.IsHotelStayAfterTrip, driver, info);

            //    if (driverInfo.Stats.Penalty > 0) {
            //        Console.WriteLine("Driver {0} penalty: {1}", driver.GetId(), ParseHelper.GetPenaltyString(driverInfo));
            //    }
            //}
            //Console.WriteLine();

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

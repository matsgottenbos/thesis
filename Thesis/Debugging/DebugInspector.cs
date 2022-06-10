using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Thesis {
    class DebugClass {
        public int a, b, c, d, e, f, g, h, i, j;
    }

    struct DebugStruct {
        public int a, b, c, d, e, f, g, h, i, j;
    }

    class DebugInspector {
        readonly Instance instance;

        public DebugInspector(Instance instance) {
            this.instance = instance;

            InspectAssignment("e3.19 e2.7 6 e4.3 e3.13 13 2 6 e2.2 e2.0 e2.8 13 13 12 2 e2.6 6 14 e2.2 9 e4.0 0 8 12 2 e4.0 9 8 2 12 9 6 e2.5h 0 e3.15 14 e1.2 2h e2.1h 12 e2.2 7 14 10 e3.3 10 1 e3.18 e3.16 9 10 e3.18 1 e1.1 1 e3.17 7 e0.7 3 10 3 e1.1 1 7 10 10 e0.7 3 e0.3 13 e3.7 4 2 e1.4 13 e3.0 4 e2.8 e3.6 e0.1 e1.3h 5 e1.4 e4.0h e2.5 e0.5 13 2 e1.6 2 11 e4.4 e2.1 e1.1 e4.1 2 e2.8 e1.5h e3.8 e0.2h e3.8h 11 e3.1 4 e0.1 5 e2.1 e3.11 2h 11 10 e1.4h 5 e2.5 11 5h e2.1 11 e1.2 e3.1h 10 e3.17 14 e1.2 10 10h 14 e3.9 3 e3.7h 14 7 e1.5 e4.0 0 e0.2 e3.4 3 14 e3.0 0 4 e3.2h 7 5 e3.19 6 0 e2.4 e4.4h e0.6 12 12 4 e3.16 e4.5h e2.0 3 4 3 e3.15 0 11 e1.6 e3.16 7 e4.1h e0.1h 5 e3.10 12 0 e3.8 e0.4h e0.0h 2 4 e2.0 11 5 0 e3.1 e4.2 e1.4h e1.3 e3.15 e0.7 6 e3.4h e3.12 9 e3.19 e2.3h 6 11 4 e1.6 e3.14 6 10 12 5h 6h e1.0h e0.3 e2.4h 2 8 12h e3.12 e3.7 e2.6 e3.13 e3.3h e3.18h 9 e3.17h 2 e4.3 e0.5h e2.6 e2.5 e3.5 10 9h 1 8 e4.3h 1 14 e3.6h 14 e2.2h e0.4 e3.2h 1 e4.1 14 e3.1 e3.4 7 e4.4 e3.14 e4.5 1h e1.6 e3.14 e0.2 7 13 e1.1 e0.0h 7 e4.1 14 e3.16 e3.14 e3.8 e3.16 e0.7 e2.4 e1.6 e4.5h 5 13 e0.6 e4.1 e2.8 7 e3.3 6 12 e4.1 3 3 e0.1h e0.7 e1.0 e0.2h 5 13h e2.3 12 e0.7h e1.0h 6h e1.4 e3.15 e2.4h 5 e2.1 0 e1.2h 3 e3.18h e3.2 e3.12 12 3 0 e2.3 e3.2 e3.6 e3.9 e2.2h e3.1 e3.12h e4.2 12 e3.17 11 0 4 9 e4.3 e3.9h 9 e3.10h 10 e1.5 9 e0.5h 14 e3.6 e1.1 11 4 e0.1 9 e4.3 14 4 e2.7 e3.0 1 e4.2 10 e2.6 11 e3.13h 11 e2.7h 7 2 1 e4.3 10 11 13 8 13 9 e1.5 4 e4.3h 8 9 14 7 13 e0.3 7 14 1 10 e2.5 e0.2 e0.0 e2.5 1 e0.1 7 8 e2.2 e2.4 e3.12 e4.5h 14 14h 2 1 2 e0.2 e2.3h 8 13h e3.4 e1.2 2h 7 8h e2.5h e0.0 e1.2 e0.6h e1.6 e3.18 e3.16h e0.4h e1.0 e1.3 6 e3.19 e1.4h e2.1h e3.9 e1.2h 6 e0.7 e1.0 e2.7h e1.3h e4.2 e3.10 9 e1.5 e1.6h e0.3h e4.3 0 6 e4.5 e3.4 e0.5 e2.0h e3.13 e2.5 9 8 e4.1 3 2 2 e2.6 12 0 8 9h e2.5 1 2 2 e3.15 5 e4.4 8 e0.5 e4.1h 11 1 12 0 12 e3.11h 3 2 e3.5 4 e2.6h e4.0 5 8 11 e0.4h e0.6h 1 14 e2.3 e3.15 0 e2.5 14 e3.14h 14 2 2 e1.3 e4.4h e0.5h e2.7 8h 5 3 14 4 11 12 2 e2.3h 5 e1.2 1 e3.19 e3.7 e1.5 3h e3.0 12h 14 4 e3.10 e1.4 11 13 14 e0.2 11 e2.4 e1.2 4h e1.6 e2.8 e2.1 14 e3.16 e3.8h e3.19h e1.5 e1.0 e0.0h 6 e3.6 e3.9h 13 e3.12 e3.2h e4.2h e1.5 e2.2 7 e2.6 6 e3.3h 13 e2.6 e2.8h 6 e1.1 e2.0h e3.11 e0.3 7 7 e0.6 9 8 e3.17 e4.3 e3.13 3 e4.1 0 10 e0.4 e3.4 3 7 e3.7 10 0 0 3 e0.7 e0.1 0 1 7 8 10 e4.3 e2.3 11 e0.3 e3.14 e0.6 5 0 e4.4 e4.0 1 9 8 5 e3.13 e0.5 12 11 e0.6 0 10 e4.5 11 12 3 8 1 e3.19 9 11 e3.6 e3.13 e0.0 0 e2.4 e0.5 3 5 11 12 e2.7 e4.5 5 1 11 e3.15 e1.0 12 e3.18 e4.2 e2.7 e3.2 4 5 e3.0 e3.10 4 4 e3.8 e3.3 e2.0 e3.5 e3.8 e3.3 e0.0 4 e1.3 e2.8 e3.18 4 e3.9");
        }

        void InspectAssignment(string assignmentStr) {
            SaInfo info = ParseHelper.ParseAssignmentString(assignmentStr, instance);
            TotalCostCalculator.ProcessAssignmentCost(info);

            // Log assignment info
            info.TotalInfo.DebugLog(false);
            Console.WriteLine();

            //foreach (Trip trip in instance.Trips) {
            //    Console.WriteLine("{0}: {1}; {2}", trip.Index, trip.DutyName, trip.ActivityName);
            //}

            // Log driver penalties
            for (int driverIndex = 0; driverIndex < instance.AllDrivers.Length; driverIndex++) {
                Driver driver = instance.AllDrivers[driverIndex];
                List<Trip> driverPath = info.DriverPaths[driverIndex];
                DriverInfo driverInfo = TotalCostCalculator.GetDriverPathCost(driverPath, info.IsHotelStayAfterTrip, driver, info);

                if (driverInfo.Penalty > 0) {
                    Console.WriteLine("Driver {0} penalty: {1}", driver.GetId(), ParseHelper.GetPenaltyString(driverInfo));
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

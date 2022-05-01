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

            //List<Trip> driverPath = new List<Trip>() { instance.Trips[1], instance.Trips[5], instance.Trips[19] };
            //bool[] isHotelStayAfterTrip = new bool[instance.Trips.Length];
            //isHotelStayAfterTrip[5] = true;
            //TotalCostCalculator.GetDriverPathCost(driverPath, isHotelStayAfterTrip, instance.AllDrivers[8], info, true);

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

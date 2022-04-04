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

            InspectAssignment("1h 0 2 3 4 1 0 0 0 3 2 4 e0.1 e0.0 1");

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

            (double cost, double costWithoutPenalty, double basePenalty, int[] driversWorkedTime) = TotalCostCalculator.GetAssignmentCost(dummyInfo);

            Console.WriteLine("Assignment: {0}\nCost: {1}\nCost without penalty: {2}\nBase penalty: {3}\n", assignmentStr, ParseHelper.ToString(cost), ParseHelper.ToString(costWithoutPenalty), ParseHelper.ToString(basePenalty));
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

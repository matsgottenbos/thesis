using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Thesis {
    class SaInspector {
        readonly Instance instance;

        public SaInspector(Instance instance) {
            this.instance = instance;

            InspectAssignment("e0.0 0 1 3 4 e0.1 e1.0 0 3 0 2 1 e0.0 e0.1 4"); // Opt
            InspectAssignment("e0.0 1 2 3 4 0 0 e1.0 3 1 2 4 e0.0 1 0"); // SA

            Console.ReadLine();
        }

        void InspectAssignment(string assignmentStr) {
            Driver[] assignment = assignmentStr.Split().Select(driverIndexStr => ParseDriver(driverIndexStr)).ToArray();

            (double cost, double costWithoutPenalty, double basePenalty, int[] driversWorkedTime) = TotalCostCalculator.GetAssignmentCost(assignment, instance, 1f);

            Console.WriteLine("Assignment: {0}\nCost: {1}\nCost without penalty: {2}\nBase penalty: {3}\n", assignmentStr, ParseHelper.ToString(cost), ParseHelper.ToString(costWithoutPenalty), ParseHelper.ToString(basePenalty));
        }

        Driver ParseDriver(string driverStr) {
            if (driverStr[0] == 'e') {
                // External driver
                int typeIndex = int.Parse(Regex.Replace(driverStr, @"e(\d+)\.(\d+)", "$1"));
                int indexInType = int.Parse(Regex.Replace(driverStr, @"e(\d+)\.(\d+)", "$2"));
                return instance.ExternalDriversByType[typeIndex][indexInType];
            } else {
                // Internal driver
                int internalDriverIndex = int.Parse(driverStr);
                return instance.InternalDrivers[internalDriverIndex];
            }
        }
    }
}

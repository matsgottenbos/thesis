using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class SaInspector {
        public SaInspector(Instance instance) {
            string optAssignmentStr = "0 1 7 6 7 2 8 3 5 4 2 8 9 4 7 0 1 9 3 6";
            string saAssignment = "0 3 7 6 7 2 8 1 5 4 4 8 5 2 7 9 1 6 0 3";

            string assignmentStr = optAssignmentStr;
            Driver[] assignment = assignmentStr.Split().Select(driverIndexStr => instance.Drivers[int.Parse(driverIndexStr)]).ToArray();

            (double cost, double costWithoutPenalty, double penaltyBase, double[] driversWorkedHours) = CostHelper.AssignmentCostWithPenalties(assignment, instance, 1f);

            Console.WriteLine("{0}; {1}; {2}", cost, costWithoutPenalty, penaltyBase);

            Console.ReadLine();
        }
    }
}

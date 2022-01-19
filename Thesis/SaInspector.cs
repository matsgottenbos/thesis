using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class SaInspector {
        public SaInspector(Instance instance) {
            string optAssignmentStr = "0 6 4 7 4 2 1 3 8 9 2 3 4 8 7 0 1 6 9 5";
            string saAssignment = "0 7 4 6 4 2 1 3 8 9 2 3 4 8 7 0 1 6 9 5";

            string assignmentStr = optAssignmentStr;
            Driver[] assignment = assignmentStr.Split().Select(driverIndexStr => instance.Drivers[int.Parse(driverIndexStr)]).ToArray();

            (double cost, double costWithoutPenalty, double penaltyBase, double[] driversWorkedTime) = CostHelper.AssignmentCostWithPenalties(assignment, instance, 1f);

            Console.WriteLine("{0}; {1}; {2}", cost, costWithoutPenalty, penaltyBase);

            Console.ReadLine();
        }
    }
}

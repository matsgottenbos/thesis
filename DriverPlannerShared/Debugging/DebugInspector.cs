using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public class DebugInspector {
        readonly Instance instance;

        public DebugInspector(Instance instance) {
            this.instance = instance;

            string assignment = "e2.9 16 15 16 15 16 16 4 6 0 4 12 6 7 8 7 4 8 13 13 6 1 6 1 0 e6.0 1 13 3 14 1 14 2 1 e6.0 9 14 9 9 2 e3.4 2 24 23 23 11 23 11 16 11 4 12 e3.4 10 4 5 12 16 e3.2 0 15 6 10 11 7 11 10 13 12 5 e6.4 e8.1 13 6 12 6 15 5 e1.2 13 e1.2 5 4 15 e1.2 e3.2 19 7h e3.2 1 e1.0 20 19 20 e1.0 8 2 8 1 2 1 8 2 26 14 9 26 3 14 9 25 9 e3.5 e8.0 3 25 0 16 10 3 4 12 3 10 5 5 e3.4 14 5 10 14 e1.2 9 12 e3.5 12 e3.4 6 e1.2 12 7 15 6 15 11 11 11 11 25 1 25 1 25 2 13 8 2 3 8 10 3 e8.7 4 e3.3 1 8 8 0 8 0 3 5 16 e6.1 5 e6.1 12 10 e8.7 13 14 14 e8.6 e8.6 16 e8.6 14 e8.7 6 0 e6.1 6 15 6 16 14 6 16 6 15 15 9 e8.8 9 e8.8 9 e8.8 24 22 24 22 22 e1.0 1 24 11 e8.8 7 e1.0 3 8 3 8 11 10 13 e1.0 13 3 e1.0 13 8 2 7 5 3 5 10 14 7 10 26 5 2 13 14 e3.3 5 13 5 26 26 1 1 19 19 15 19 1 12 0 0 6 e8.6 0 15 15 9 12 6 8 e8.0h 9 9 8 9 0 6 e6.4 6 e6.4 4 4 4 8 11 16 11 4 16 e6.4 16 11 16 e6.0 e3.2 e8.0 e6.4 e6.4 14 10 7 5 14 10 3 e8.5 e6.4 13 25 22 25 3 22 25 10 e6.4 13 10 e8.5 e6.4 13 2 3 2 2";
            //InspectAssignment(assignment);

            string[] altAssignmentParts = assignment.Split();
            string activity59OldDriverStr = altAssignmentParts[59];
            altAssignmentParts[59] = altAssignmentParts[71];
            altAssignmentParts[71] = activity59OldDriverStr;
            string altAssignment = string.Join(" ", altAssignmentParts);
            InspectAssignment(altAssignment);

            throw new Exception("Debug inspector");
        }

        void InspectAssignment(string assignmentStr) {
            SaInfo info = ParseHelper.ParseAssignmentString(assignmentStr, instance);
            TotalCostCalculator.ProcessAssignmentCost(info);

            // Log assignment info
            info.TotalInfo.DebugLog(false);
            Console.WriteLine();

            // Test operation
            //AssignInternalOperation operation = new AssignInternalOperation(204, instance.InternalDrivers[21], info);
            //AssignExternalOperation operation = new AssignExternalOperation(70, instance.ExternalDriversByType[3][6], info);
            //SaTotalInfo totalInfoDiff = operation.GetCostDiff();
            //double oldAdjustedCost = SimulatedAnnealing.GetAdjustedCost(info.TotalInfo.Stats.Cost, info.TotalInfo.Stats.SatisfactionScore.Value, info.SatisfactionFactor);
            //double newAdjustedCost = SimulatedAnnealing.GetAdjustedCost(info.TotalInfo.Stats.Cost + totalInfoDiff.Stats.Cost, info.TotalInfo.Stats.SatisfactionScore.Value + totalInfoDiff.Stats.SatisfactionScore.Value, info.SatisfactionFactor);
            //double adjustedCostDiff = newAdjustedCost - oldAdjustedCost;

            throw new Exception("Debug inspector");
        }
    }

    public class DebugObj {
        public readonly int Value;
        public DebugObj Prev;

        public DebugObj(int value, DebugObj prev) {
            Value = value;
            Prev = prev;
        }
    }
}

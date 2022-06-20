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

            InspectAssignment("e5.4 e5.4 20 e5.4 12 e5.3 e5.3 23 20 e5.3 e5.2 11 4 15 12 15 6 17 12 10 16 e2.6 16 17 16 e2.6 11 12 e5.2 16 23 e5.3 23 4 15 10 7 17 4 17 e5.2 7 22 17 6 23 16 6 7 11 7 17 17 7 e2.6 22 17 7 16 7 18 22 18 21 18 20 21 21 9 3 e1.3 9 e1.3 18 9 9 3 e4.2 9 3 e0.5 21 9 e4.17 20 21 9 3 16 e4.17 20 9 13 e1.3 e4.14 e6.2 20 9 13 3 3 e0.8 e4.2 1 13 e0.8 3 0 13 e0.8 e0.5 0 e1.3 13 e4.17 e0.5 16 8 5 e0.8 8 e0.8 1 16 e0.5 1 e0.5 6 6 5 e4.14 1 8 e0.5 13 13 0 8 8 e6.2 1 8 1 e0.8 6 5 5 6 6 6 8 10 18 15 10 18 18 18 12 23 7 14 18 e3.2 18 15 e3.2 10 15 12 15 18 10 15 e3.2 7 15 22 23 e2.0 22 e3.2 18 e3.2 22 15 14 22 12 14 22 12 7 e3.6 2 4 2 e4.3 21 21 e4.3 11 2 2 22 17 21 e4.3 17 16 2 11 e2.0 16 e3.6 17 11 21 21 16 2 17 1 4 1 13 13 13 1 13 6 6 18 6 18 13 e6.0 5 18 1 13 6 0 18 6 0 18 13 8 9 e6.0 6 13 9 6 18 18 e2.3 9 e4.4 19 e4.8 e6.0 5 0 5 e6.0 5 5 e3.4 8 3 3 0 0 e3.4 5 18 5 18 8 9 e4.4 5 3 0 8 e4.8 19 8 e4.4 e1.2 e2.3 e1.2 3 e1.2 e1.2 20 e1.2 e0.6 16 16 16 2 14 e6.3 14 20 e4.1 2 e2.11 16 16 20 e4.15 16 2 16 e0.6 e2.5 e2.11 18 21 18 2 18 e2.5 14 e2.2 21 14 e6.3 14 14 2 14 14 e4.1 18 21 14 e2.11 18 21 18 18 e1.3 e1.3 20 e1.3 e1.3 19 19 e0.10 e1.3 19 e5.1 e0.2 19 19 2 2 20 e0.10 e6.2 2 19 2 10 e5.1 19 e3.2 2 e6.2 13 e5.1 23 2 2 1 e0.5 13 2 23 e0.2 e0.2 e3.2 e0.10 e0.5 13 23 1 0 e6.2 0 17 e6.2 0 0 17 10 10 1 e0.2 1 0 0 17 10 13 23 e0.5 23 13 0 23 17 e4.9 18 e0.8 18 e0.1 13 e0.8 18 13 20 20 13 12 e0.8 12 12 20 e0.8 18 e4.15 13 e2.0 e0.8 12 12 14 14 13 14 12 e2.0 20 14 e2.0 14");
        }

        void InspectAssignment(string assignmentStr) {
            SaInfo info = ParseHelper.ParseAssignmentString(assignmentStr, instance);
            TotalCostCalculator.ProcessAssignmentCost(info);

            // Log assignment info
            info.TotalInfo.DebugLog(false);
            Console.WriteLine();

            // Test operation
            AssignInternalOperation operation = new AssignInternalOperation(391, instance.InternalDrivers[1], info);
            SaTotalInfo totalInfoDiff = operation.GetCostDiff();
            double oldAdjustedCost = SimulatedAnnealing.GetAdjustedCost(info.TotalInfo.Stats.Cost, info.TotalInfo.Stats.SatisfactionScore.Value, info.SatisfactionFactor);
            double newAdjustedCost = SimulatedAnnealing.GetAdjustedCost(info.TotalInfo.Stats.Cost + totalInfoDiff.Stats.Cost, info.TotalInfo.Stats.SatisfactionScore.Value + totalInfoDiff.Stats.SatisfactionScore.Value, info.SatisfactionFactor);
            double adjustedCostDiff = newAdjustedCost - oldAdjustedCost;

            throw new Exception("Debug inspector");
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

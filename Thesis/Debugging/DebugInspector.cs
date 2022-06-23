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

            InspectAssignment("e6.3 e0.0 e6.3 e0.0 e6.3 10 10 2 17 10 19 0 10 13 2 12 6 4 e3.2 7 12 10 12 4 12 10 0 e3.2 e2.5 12 9 2 9 19 13 4 3 7 19 7 17 3 e2.5 7 12 9 0 12 6 e4.1 6 7 7 6 e2.5 e3.2 7 3 9 3 8 e4.1 8 e4.15 8 20 e4.15 e4.15 e1.2 e0.10 17 e1.2 17 8 20 20 11 e5.4 20 11 16 e4.15 20 21 e4.9 11 20 11 2 21 e4.9 20 17 0 e1.2 e0.10 e4.9 20 17 e4.15 11 e5.4 7 12 17 e5.4 11 e3.5 17 e5.4 16 e3.5 e4.9 17 e3.3 16 2 12 3 e5.4 12 e5.4 e6.4 2 16 e6.4 16 21 21 3 e6.4 e6.3 12 16 0 0 e3.5 12 12 e6.4 e3.3 12 3 7 e6.3 e3.3 3 e6.3 e6.3 e6.3 e6.4 9 18 19 9 18 18 18 e2.3 18 e2.1 e1.0 19 6 19 18 6 9 18 15 18 19 6 18 6 15 0 1 10 e2.3 1 6 19 6 1 0 0 1 4 0 1 4 15 e3.5 e1.0 e4.5 e1.0 5 14 e5.4 5 12 e1.0 13 e4.4 4 e5.4 10 4 14 13 12 1 14 e3.5 4 5 13 13 14 e5.4 e4.4 12 16 12 16 16 16 e1.3 8 7 7 15 7 15 8 17 e1.3 15 16 8 7 2 15 7 2 15 8 3 20 17 e0.4 8 21 7 15 15 e1.3 21 8 e6.4 21 17 2 3 2 17 2 2 e0.4 14 20 20 3 18 e0.4 2 15 2 15 14 e4.13 3 2 20 18 14 e2.0 e6.4 14 3 e5.1 21 14 18 e5.1 e5.1 e4.13 e5.1 4 11 11 11 10 16 0 16 12 e2.10 10 e4.17 11 11 12 e0.10 11 16 11 2 1 e4.17 7 10 7 16 7 1 4 e4.8 16 4 0 4 12 0 12 12 10 15 7 12 2 15 7 1 1 13 13 15 13 13 14 14 5 19 14 e0.9 10 0 0 16 16 13 5 e0.12 16 14 16 19 e0.9 14 8 16 e0.12 1 e0.9 18 16 16 10 1 e5.3 16 19 0 0 12 5 1 e5.3 19 0 21 e0.12 21 19 e0.12 21 21 19 18 18 8 10 8 21 21 e0.9 18 1 12 e5.3 12 1 21 12 12 e0.3 11 e2.3 11 e0.2 15 11 e2.3 15 e2.2 19 15 1 11 1 1 19 11 e4.12 e4.1 15 e2.2 e2.3 1 1 9 9 15 9 1 9 19 5 9 5");
        }

        void InspectAssignment(string assignmentStr) {
            SaInfo info = ParseHelper.ParseAssignmentString(assignmentStr, instance);
            TotalCostCalculator.ProcessAssignmentCost(info);

            // Log assignment info
            info.TotalInfo.DebugLog(false);
            Console.WriteLine();

            for (int internalDriverIndex = 0; internalDriverIndex < info.Instance.InternalDrivers.Length; internalDriverIndex++) {
                InternalDriver driver = info.Instance.InternalDrivers[internalDriverIndex];
                SaDriverInfo driverInfo = info.DriverInfos[internalDriverIndex];
                Dictionary<string, double> satisfactionPerCriterion = SatisfactionCalculator.GetDriverSatisfactionPerCriterion(driver, driverInfo);

                Console.WriteLine("\n* Driver {0} satisfaction *", driver.GetId());
                foreach (KeyValuePair<string, double> criterionKvp in satisfactionPerCriterion) {
                    Console.WriteLine("{0}: {1}%", criterionKvp.Key, ParseHelper.ToString(criterionKvp.Value * 100, "0"));
                }
            }

            // Test operation
            //AssignInternalOperation operation = new AssignInternalOperation(391, instance.InternalDrivers[1], info);
            //SaTotalInfo totalInfoDiff = operation.GetCostDiff();
            //double oldAdjustedCost = SimulatedAnnealing.GetAdjustedCost(info.TotalInfo.Stats.Cost, info.TotalInfo.Stats.SatisfactionScore.Value, info.SatisfactionFactor);
            //double newAdjustedCost = SimulatedAnnealing.GetAdjustedCost(info.TotalInfo.Stats.Cost + totalInfoDiff.Stats.Cost, info.TotalInfo.Stats.SatisfactionScore.Value + totalInfoDiff.Stats.SatisfactionScore.Value, info.SatisfactionFactor);
            //double adjustedCostDiff = newAdjustedCost - oldAdjustedCost;

            //throw new Exception("Debug inspector");
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

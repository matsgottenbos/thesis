using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class SwapOperation : AbstractOperation {
        readonly Trip trip1, trip2;
        readonly Driver driver1, driver2;
        int driver1WorkedTimeDiff, driver2WorkedTimeDiff;

        public SwapOperation(int tripIndex1, int tripIndex2, SaInfo info) : base(info) {
            trip1 = info.Instance.Trips[tripIndex1];
            trip2 = info.Instance.Trips[tripIndex2];
            driver1 = info.Assignment[tripIndex1];
            driver2 = info.Assignment[tripIndex2];
        }

        public override (double, double, double) GetCostDiff() {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().Description = string.Format("Swap trip {0} from driver {1} with trip {2} from driver {3}", trip1.Index, driver1.GetId(), trip2.Index, driver2.GetId());
            }
            #endif

            int driver1WorkedTime = info.DriversWorkedTime[driver1.AllDriversIndex];
            (double driver1UnassignCostDiff, double driver1UnassignCostWithoutPenaltyDiff, double driver1UnassignBasePenaltyDiff, int driver1UnassignShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(false, trip1, null, driver1, driver1WorkedTime, info);

            int driver2WorkedTime = info.DriversWorkedTime[driver2.AllDriversIndex];
            (double driver2UnassignCostDiff, double driver2UnassignCostWithoutPenaltyDiff, double driver2UnassignBasePenaltyDiff, int driver2UnassignShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(false, trip2, null, driver2, driver2WorkedTime, info);

            int driver1WorkedTimeAfterUnassign = driver1WorkedTime + driver1UnassignShiftLengthDiff;
            (double driver1AssignCostDiff, double driver1AssignCostWithoutPenaltyDiff, double driver1AssignBasePenaltyDiff, int driver1AssignShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(true, trip2, trip1, driver1, driver1WorkedTimeAfterUnassign, info);

            int driver2WorkedTimeAfterUnassign = driver2WorkedTime + driver2UnassignShiftLengthDiff;
            (double driver2AssignCostDiff, double driver2AssignCostWithoutPenaltyDiff, double driver2AssignBasePenaltyDiff, int driver2AssignShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(true, trip1, trip2, driver2, driver2WorkedTimeAfterUnassign, info);

            double costDiff = driver1UnassignCostDiff + driver2UnassignCostDiff + driver1AssignCostDiff + driver2AssignCostDiff;
            double costWithoutPenalty = driver1UnassignCostWithoutPenaltyDiff + driver2UnassignCostWithoutPenaltyDiff + driver1AssignCostWithoutPenaltyDiff + driver2AssignCostWithoutPenaltyDiff;
            double basePenaltyDiff = driver1UnassignBasePenaltyDiff + driver2UnassignBasePenaltyDiff + driver1AssignBasePenaltyDiff + driver2AssignBasePenaltyDiff;

            driver1WorkedTimeDiff = driver1UnassignShiftLengthDiff + driver1AssignShiftLengthDiff;
            driver2WorkedTimeDiff = driver2UnassignShiftLengthDiff + driver2AssignShiftLengthDiff;

            return (costDiff, costWithoutPenalty, basePenaltyDiff);
        }

        public override void Execute() {
            info.Assignment[trip2.Index] = driver1;
            info.Assignment[trip1.Index] = driver2;
            info.DriversWorkedTime[driver1.AllDriversIndex] += driver1WorkedTimeDiff;
            info.DriversWorkedTime[driver2.AllDriversIndex] += driver2WorkedTimeDiff;
        }

        public static SwapOperation CreateRandom(SaInfo info) {
            int tripIndex1 = info.FastRand.NextInt(info.Instance.Trips.Length);

            // Select random second trip that is not the first trip, and that isn't assigned to the same driver as the first trip
            int tripIndex2;
            do {
                tripIndex2 = info.FastRand.NextInt(info.Instance.Trips.Length);
            } while (tripIndex1 == tripIndex2 || info.Assignment[tripIndex1] == info.Assignment[tripIndex2]);

            return new SwapOperation(tripIndex1, tripIndex2, info);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class SwapOperation : AbstractOperation {
        readonly Trip trip1, trip2;
        readonly Driver driver1, driver2;
        int driver1WorkedTimeDiff, driver2WorkedTimeDiff, driver1ShiftCountDiff, driver2ShiftCountDiff;

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

            (double driver1CostDiff, double driver1CostWithoutPenaltyDiff, double driver1PenaltyDiff, int driver1WorkedTimeDiff, int driver1ShiftCountDiff) = CostDiffCalculator.GetDriverCostDiff(trip1, trip2, null, null, driver1, info);
            (double driver2CostDiff, double driver2CostWithoutPenaltyDiff, double driver2PenaltyDiff, int driver2WorkedTimeDiff, int driver2ShiftCountDiff) = CostDiffCalculator.GetDriverCostDiff(trip2, trip1, null, null, driver2, info);

            double costDiff = driver1CostDiff + driver2CostDiff;
            double costWithoutPenalty = driver1CostWithoutPenaltyDiff + driver2CostWithoutPenaltyDiff;
            double penaltyDiff = driver1PenaltyDiff + driver2PenaltyDiff;

            this.driver1WorkedTimeDiff = driver1WorkedTimeDiff;
            this.driver2WorkedTimeDiff = driver2WorkedTimeDiff;
            this.driver1ShiftCountDiff = driver1ShiftCountDiff;
            this.driver2ShiftCountDiff = driver2ShiftCountDiff;

            return (costDiff, costWithoutPenalty, penaltyDiff);
        }

        public override void Execute() {
            info.Assignment[trip2.Index] = driver1;
            info.Assignment[trip1.Index] = driver2;
            info.DriversWorkedTime[driver1.AllDriversIndex] += driver1WorkedTimeDiff;
            info.DriversWorkedTime[driver2.AllDriversIndex] += driver2WorkedTimeDiff;
            info.DriversShiftCounts[driver1.AllDriversIndex] += driver1ShiftCountDiff;
            info.DriversShiftCounts[driver2.AllDriversIndex] += driver2ShiftCountDiff;
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

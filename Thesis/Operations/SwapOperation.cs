﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class SwapOperation : AbstractOperation {
        readonly Trip trip1, trip2;
        readonly Driver driver1, driver2;
        readonly DriverInfo driver1Info, driver2Info;
        DriverInfo driver1InfoDiff, driver2InfoDiff;

        public SwapOperation(int tripIndex1, int tripIndex2, SaInfo info) : base(info) {
            trip1 = info.Instance.Trips[tripIndex1];
            trip2 = info.Instance.Trips[tripIndex2];
            driver1 = info.Assignment[tripIndex1];
            driver2 = info.Assignment[tripIndex2];
            driver1Info = info.DriverInfos[driver1.AllDriversIndex];
            driver2Info = info.DriverInfos[driver2.AllDriversIndex];
        }

        public override (double, double) GetCostDiff() {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().Description = string.Format("Swap trip {0} from driver {1} with trip {2} from driver {3}", trip1.Index, driver1.GetId(), trip2.Index, driver2.GetId());
            }
            #endif

            (double driver1CostDiff, double driver1CostWithoutPenaltyDiff, double driver1PenaltyDiff, double driver1SatisfactionDiff, DriverInfo driver1InfoDiff) = CostDiffCalculator.GetSwapDriverCostDiff(trip1, trip2, driver1, driver1Info, info);
            (double driver2CostDiff, double driver2CostWithoutPenaltyDiff, double driver2PenaltyDiff, double driver2SatisfactionDiff, DriverInfo driver2InfoDiff) = CostDiffCalculator.GetSwapDriverCostDiff(trip2, trip1, driver2, driver2Info, info);

            costDiff = driver1CostDiff + driver2CostDiff;
            costWithoutPenaltyDiff = driver1CostWithoutPenaltyDiff + driver2CostWithoutPenaltyDiff;
            penaltyDiff = driver1PenaltyDiff + driver2PenaltyDiff;
            satisfactionDiff = driver1SatisfactionDiff + driver2SatisfactionDiff;

            this.driver1InfoDiff = driver1InfoDiff;
            this.driver2InfoDiff = driver2InfoDiff;

            return (costDiff, satisfactionDiff);
        }

        public override void Execute() {
            base.Execute();
            info.ReassignTrip(trip1, driver1, driver2);
            info.ReassignTrip(trip2, driver2, driver1);
            info.DriverInfos[driver1.AllDriversIndex] += driver1InfoDiff;
            info.DriverInfos[driver2.AllDriversIndex] += driver2InfoDiff;
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class SwapOperation : AbstractOperation {
        readonly Trip trip1, trip2;
        readonly Driver driver1, driver2;
        readonly SaDriverInfo driver1Info, driver2Info;
        SaDriverInfo driver1InfoDiff, driver2InfoDiff;
        SaExternalDriverTypeInfo externalDriver1TypeDiff, externalDriver2TypeDiff;

        public SwapOperation(int tripIndex1, int tripIndex2, SaInfo info) : base(info) {
            trip1 = info.Instance.Trips[tripIndex1];
            trip2 = info.Instance.Trips[tripIndex2];
            driver1 = info.Assignment[tripIndex1];
            driver2 = info.Assignment[tripIndex2];
            driver1Info = info.DriverInfos[driver1.AllDriversIndex];
            driver2Info = info.DriverInfos[driver2.AllDriversIndex];
        }

        public override SaTotalInfo GetCostDiff() {
            #if DEBUG
            if (AppConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentOperation().Description = string.Format("Swap trip {0} from driver {1} with trip {2} from driver {3}", trip1.Index, driver1.GetId(), trip2.Index, driver2.GetId());
            }
            #endif

            // Get cost diffs
            driver1InfoDiff = CostDiffCalculator.GetSwapDriverCostDiff(trip1, trip2, driver1, driver1Info, info);
            driver2InfoDiff = CostDiffCalculator.GetSwapDriverCostDiff(trip2, trip1, driver2, driver2Info, info);
            (externalDriver1TypeDiff, externalDriver2TypeDiff) = GlobalCostDiffCalculator.GetExternalDriversGlobalCostDiff(driver1, driver2, driver1InfoDiff, driver2InfoDiff, info);

            // Store cost diffs in total diff object
            totalInfoDiff.AddDriverInfo(driver1InfoDiff);
            totalInfoDiff.AddDriverInfo(driver2InfoDiff);
            if (externalDriver1TypeDiff != null) totalInfoDiff.AddExternalDriverTypeInfo(externalDriver1TypeDiff);
            if (externalDriver2TypeDiff != null) totalInfoDiff.AddExternalDriverTypeInfo(externalDriver2TypeDiff);

            // Determine satisfaction score diff
            totalInfoDiff.Stats.SatisfactionScore = SatisfactionCalculator.GetSatisfactionScoreDiff(totalInfoDiff, driver1, driver1InfoDiff, driver2, driver2InfoDiff, info);

            return totalInfoDiff;
        }

        public override void Execute() {
            base.Execute();
            info.ReassignTrip(trip1, driver1, driver2);
            info.ReassignTrip(trip2, driver2, driver1);
            UpdateDriverInfo(driver1, driver1InfoDiff);
            UpdateDriverInfo(driver2, driver2InfoDiff);
            UpdateExternalDriverTypeInfo(driver1, externalDriver1TypeDiff);
            UpdateExternalDriverTypeInfo(driver2, externalDriver2TypeDiff);
        }

        public static SwapOperation CreateRandom(SaInfo info, XorShiftRandom rand) {
            int tripIndex1 = rand.Next(info.Instance.Trips.Length);

            // Select random second trip that is not the first trip, and that isn't assigned to the same driver as the first trip
            int tripIndex2;
            do {
                tripIndex2 = rand.Next(info.Instance.Trips.Length);
            } while (tripIndex1 == tripIndex2 || info.Assignment[tripIndex1] == info.Assignment[tripIndex2]);

            return new SwapOperation(tripIndex1, tripIndex2, info);
        }
    }
}

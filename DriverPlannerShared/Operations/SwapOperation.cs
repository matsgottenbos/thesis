using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public class SwapOperation : AbstractOperation {
        readonly Activity activity1, activity2;
        readonly Driver driver1, driver2;
        readonly SaDriverInfo driver1Info, driver2Info;
        SaDriverInfo driver1InfoDiff, driver2InfoDiff;
        SaExternalDriverTypeInfo externalDriver1TypeDiff, externalDriver2TypeDiff;

        public SwapOperation(int activityIndex1, int activityIndex2, SaInfo info) : base(info) {
            activity1 = info.Instance.Activities[activityIndex1];
            activity2 = info.Instance.Activities[activityIndex2];
            driver1 = info.Assignment[activityIndex1];
            driver2 = info.Assignment[activityIndex2];
            driver1Info = info.DriverInfos[driver1.AllDriversIndex];
            driver2Info = info.DriverInfos[driver2.AllDriversIndex];
        }

        public override SaTotalInfo GetCostDiff() {
            #if DEBUG
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentOperation().Description = string.Format("Swap activity {0} from driver {1} with activity {2} from driver {3}", activity1.Index, driver1.GetId(), activity2.Index, driver2.GetId());
            }
            #endif

            // Get cost diffs
            driver1InfoDiff = CostDiffCalculator.GetSwapDriverCostDiff(activity1, activity2, driver1, driver1Info, info);
            driver2InfoDiff = CostDiffCalculator.GetSwapDriverCostDiff(activity2, activity1, driver2, driver2Info, info);
            (externalDriver1TypeDiff, externalDriver2TypeDiff) = GlobalCostDiffCalculator.GetExternalDriversGlobalCostDiff(driver1, driver2, driver1InfoDiff, driver2InfoDiff, info);

            // Store cost diffs in total diff object
            totalInfoDiff.AddDriverInfo(driver1InfoDiff);
            totalInfoDiff.AddDriverInfo(driver2InfoDiff);
            if (externalDriver1TypeDiff != null) totalInfoDiff.AddExternalDriverTypeInfo(externalDriver1TypeDiff);
            if (externalDriver2TypeDiff != null) totalInfoDiff.AddExternalDriverTypeInfo(externalDriver2TypeDiff);

            // Determine satisfaction score diff
            totalInfoDiff.Stats.SatisfactionScore = SatisfactionScoreCalculator.GetSatisfactionScoreDiff(totalInfoDiff, driver1, driver1InfoDiff, driver2, driver2InfoDiff, info);

            return totalInfoDiff;
        }

        public override void Execute() {
            base.Execute();
            info.ReassignActivity(activity1, driver1, driver2);
            info.ReassignActivity(activity2, driver2, driver1);
            UpdateDriverInfo(driver1, driver1InfoDiff);
            UpdateDriverInfo(driver2, driver2InfoDiff);
            UpdateExternalDriverTypeInfo(driver1, externalDriver1TypeDiff);
            UpdateExternalDriverTypeInfo(driver2, externalDriver2TypeDiff);
        }

        public static SwapOperation CreateRandom(SaInfo info, XorShiftRandom rand) {
            int activityIndex1 = rand.Next(info.Instance.Activities.Length);

            // Select random second activity that is not the first activity, and that isn't assigned to the same driver as the first activity
            int activityIndex2;
            do {
                activityIndex2 = rand.Next(info.Instance.Activities.Length);
            } while (activityIndex1 == activityIndex2 || info.Assignment[activityIndex1] == info.Assignment[activityIndex2]);

            return new SwapOperation(activityIndex1, activityIndex2, info);
        }
    }
}

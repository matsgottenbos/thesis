using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    abstract class AbstractAssignOperation : AbstractOperation {
        readonly Activity activity;
        readonly Driver unassignedDriver, assignedDriver;
        readonly SaDriverInfo unassignedDriverInfo, assignedDriverInfo;
        SaDriverInfo unassignedDriverInfoDiff, assignedDriverInfoDiff;
        SaExternalDriverTypeInfo unassignedExternalDriverTypeDiff, assignedExternalDriverTypeDiff;

        public AbstractAssignOperation(int activityIndex, Driver assignedDriver, SaInfo info) : base(info) {
            this.assignedDriver = assignedDriver;
            activity = info.Instance.Activities[activityIndex];
            unassignedDriver = info.Assignment[activityIndex];
            unassignedDriverInfo = info.DriverInfos[unassignedDriver.AllDriversIndex];
            assignedDriverInfo = info.DriverInfos[assignedDriver.AllDriversIndex];
        }

        public override SaTotalInfo GetCostDiff() {
            #if DEBUG
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentOperation().Description = string.Format("Re-assign activity {0} from driver {1} to driver {2}", activity.Index, unassignedDriver.GetId(), assignedDriver.GetId());
            }
            #endif

            // Get cost diffs
            unassignedDriverInfoDiff = CostDiffCalculator.GetUnassignDriverCostDiff(activity, unassignedDriver, unassignedDriverInfo, info);
            assignedDriverInfoDiff = CostDiffCalculator.GetAssignDriverCostDiff(activity, assignedDriver, assignedDriverInfo, info);
            (unassignedExternalDriverTypeDiff, assignedExternalDriverTypeDiff) = GlobalCostDiffCalculator.GetExternalDriversGlobalCostDiff(unassignedDriver, assignedDriver, unassignedDriverInfoDiff, assignedDriverInfoDiff, info);

            // Store cost diffs in total diff object
            totalInfoDiff.AddDriverInfo(unassignedDriverInfoDiff);
            totalInfoDiff.AddDriverInfo(assignedDriverInfoDiff);
            if (unassignedExternalDriverTypeDiff != null) totalInfoDiff.AddExternalDriverTypeInfo(unassignedExternalDriverTypeDiff);
            if (assignedExternalDriverTypeDiff != null) totalInfoDiff.AddExternalDriverTypeInfo(assignedExternalDriverTypeDiff);

            // Determine satisfaction score diff
            totalInfoDiff.Stats.SatisfactionScore = SatisfactionCalculator.GetSatisfactionScoreDiff(totalInfoDiff, unassignedDriver, unassignedDriverInfoDiff, assignedDriver, assignedDriverInfoDiff, info);

            return totalInfoDiff;
        }

        public override void Execute() {
            base.Execute();
            info.ReassignActivity(activity, unassignedDriver, assignedDriver);
            UpdateDriverInfo(unassignedDriver, unassignedDriverInfoDiff);
            UpdateDriverInfo(assignedDriver, assignedDriverInfoDiff);
            UpdateExternalDriverTypeInfo(unassignedDriver, unassignedExternalDriverTypeDiff);
            UpdateExternalDriverTypeInfo(assignedDriver, assignedExternalDriverTypeDiff);
        }
    }
}

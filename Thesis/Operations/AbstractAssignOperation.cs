using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    abstract class AbstractAssignOperation : AbstractOperation {
        readonly Trip trip;
        readonly Driver unassignedDriver, assignedDriver;
        readonly DriverInfo unassignedDriverInfo, assignedDriverInfo;
        int unassignedDriverWorkedTimeDiff, assignedDriverWorkedTimeDiff, unassignedDriverShiftCountDiff, assignedDriverShiftCountDiff;

        public AbstractAssignOperation(int tripIndex, Driver assignedDriver, SaInfo info) : base(info) {
            this.assignedDriver = assignedDriver;
            trip = info.Instance.Trips[tripIndex];
            unassignedDriver = info.Assignment[tripIndex];
            unassignedDriverInfo = info.DriverInfos[unassignedDriver.AllDriversIndex];
            assignedDriverInfo = info.DriverInfos[assignedDriver.AllDriversIndex];
        }

        public override (double, double, double) GetCostDiff() {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().Description = string.Format("Re-assign trip {0} from driver {1} to driver {2}", trip.Index, unassignedDriver.GetId(), assignedDriver.GetId());
            }
            #endif

            (double unassignedDriverCostDiff, double unassignedDriverCostWithoutPenaltyDiff, double unassignedDriverPenaltyDiff, int unassignedDriverWorkedTimeDiff, int unassignedDriverShiftCountDiff) = CostDiffCalculator.GetUnassignDriverCostDiff(trip, unassignedDriver, unassignedDriverInfo, info);
            (double assignedDriverCostDiff, double assignedDriverCostWithoutPenaltyDiff, double assignedDriverPenaltyDiff, int assignedDriverWorkedTimeDiff, int assignedDriverShiftCountDiff) = CostDiffCalculator.GetAssignDriverCostDiff(trip, assignedDriver, assignedDriverInfo, info);

            this.unassignedDriverWorkedTimeDiff = unassignedDriverWorkedTimeDiff;
            this.assignedDriverWorkedTimeDiff = assignedDriverWorkedTimeDiff;
            this.unassignedDriverShiftCountDiff = unassignedDriverShiftCountDiff;
            this.assignedDriverShiftCountDiff = assignedDriverShiftCountDiff;

            return (unassignedDriverCostDiff + assignedDriverCostDiff, unassignedDriverCostWithoutPenaltyDiff + assignedDriverCostWithoutPenaltyDiff, unassignedDriverPenaltyDiff + assignedDriverPenaltyDiff);
        }

        public override void Execute() {
            info.ReassignTrip(trip, unassignedDriver, assignedDriver);
            unassignedDriverInfo.WorkedTime += unassignedDriverWorkedTimeDiff;
            assignedDriverInfo.WorkedTime += assignedDriverWorkedTimeDiff;
            unassignedDriverInfo.ShiftCount += unassignedDriverShiftCountDiff;
            assignedDriverInfo.ShiftCount += assignedDriverShiftCountDiff;
        }
    }
}

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
        DriverInfo unassignedDriverInfoDiff, assignedDriverInfoDiff;

        public AbstractAssignOperation(int tripIndex, Driver assignedDriver, SaInfo info) : base(info) {
            this.assignedDriver = assignedDriver;
            trip = info.Instance.Trips[tripIndex];
            unassignedDriver = info.Assignment[tripIndex];
            unassignedDriverInfo = info.DriverInfos[unassignedDriver.AllDriversIndex];
            assignedDriverInfo = info.DriverInfos[assignedDriver.AllDriversIndex];
        }

        public override (double, double) GetCostDiff() {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().Description = string.Format("Re-assign trip {0} from driver {1} to driver {2}", trip.Index, unassignedDriver.GetId(), assignedDriver.GetId());
            }
            #endif

            (double unassignedDriverCostDiff, double unassignedDriverCostWithoutPenaltyDiff, double unassignedDriverPenaltyDiff, double unassignedDriverSatisfactionDiff, DriverInfo unassignedDriverInfoDiff) = CostDiffCalculator.GetUnassignDriverCostDiff(trip, unassignedDriver, unassignedDriverInfo, info);
            (double assignedDriverCostDiff, double assignedDriverCostWithoutPenaltyDiff, double assignedDriverPenaltyDiff, double assignedDriverSatisfactionDiff, DriverInfo assignedDriverInfoDiff) = CostDiffCalculator.GetAssignDriverCostDiff(trip, assignedDriver, assignedDriverInfo, info);

            costDiff = unassignedDriverCostDiff + assignedDriverCostDiff;
            costWithoutPenaltyDiff = unassignedDriverCostWithoutPenaltyDiff + assignedDriverCostWithoutPenaltyDiff;
            penaltyDiff = unassignedDriverPenaltyDiff + assignedDriverPenaltyDiff;
            satisfactionDiff = unassignedDriverSatisfactionDiff + assignedDriverSatisfactionDiff;

            this.unassignedDriverInfoDiff = unassignedDriverInfoDiff;
            this.assignedDriverInfoDiff = assignedDriverInfoDiff;

            return (costDiff, satisfactionDiff);
        }

        public override void Execute() {
            base.Execute();
            info.ReassignTrip(trip, unassignedDriver, assignedDriver);
            info.DriverInfos[unassignedDriver.AllDriversIndex] += unassignedDriverInfoDiff;
            info.DriverInfos[assignedDriver.AllDriversIndex] += assignedDriverInfoDiff;
        }
    }
}

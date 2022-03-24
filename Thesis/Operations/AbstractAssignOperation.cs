using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    abstract class AbstractAssignOperation : AbstractOperation {
        readonly Trip trip;
        readonly Driver oldDriver, newDriver;
        int oldDriverWorkedTimeDiff, newDriverWorkedTimeDiff;

        public AbstractAssignOperation(int tripIndex, Driver newDriver, SaInfo info) : base(info) {
            this.newDriver = newDriver;
            trip = info.Instance.Trips[tripIndex];
            oldDriver = info.Assignment[tripIndex];
        }

        public override (double, double, double) GetCostDiff() {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().Description = string.Format("Re-assign trip {0} from driver {1} to driver {2}", trip.Index, oldDriver.GetId(), newDriver.GetId());
            }
            #endif

            int oldDriverWorkedTime = info.DriversWorkedTime[oldDriver.AllDriversIndex];
            (double oldDriverCostDiff, double oldDriverCostWithoutPenaltyDiff, double oldDriverBasePenaltyDiff, int oldDriverShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(false, trip, null, oldDriver, oldDriverWorkedTime, info);

            int newDriverWorkedTime = info.DriversWorkedTime[newDriver.AllDriversIndex];
            (double newDriverCostDiff, double newDriverCostWithoutPenaltyDiff, double newDriverBasePenaltyDiff, int newDriverShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(true, trip, null, newDriver, newDriverWorkedTime, info);

            oldDriverWorkedTimeDiff = oldDriverShiftLengthDiff;
            newDriverWorkedTimeDiff = newDriverShiftLengthDiff;

            return (oldDriverCostDiff + newDriverCostDiff, oldDriverCostWithoutPenaltyDiff + newDriverCostWithoutPenaltyDiff, oldDriverBasePenaltyDiff + newDriverBasePenaltyDiff);
        }

        public override void Execute() {
            info.Assignment[trip.Index] = newDriver;
            info.DriversWorkedTime[oldDriver.AllDriversIndex] += oldDriverWorkedTimeDiff;
            info.DriversWorkedTime[newDriver.AllDriversIndex] += newDriverWorkedTimeDiff;
        }
    }
}

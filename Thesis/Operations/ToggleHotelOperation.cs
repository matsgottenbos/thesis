using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class ToggleHotelOperation : AbstractOperation {
        readonly Trip trip;
        readonly bool isAddition;
        readonly Driver driver;
        readonly DriverInfo driverInfo;
        DriverInfo driverInfoDiff;

        public ToggleHotelOperation(int tripIndex, bool isAddition, SaInfo info) : base(info) {
            trip = info.Instance.Trips[tripIndex];
            this.isAddition = isAddition;
            driver = info.Assignment[tripIndex];
            driverInfo = info.DriverInfos[driver.AllDriversIndex];
        }

        public override (double, double) GetCostDiff() {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string templateStr = isAddition ? "Add hotel stay to trip {0} with driver {1}" : "Remove hotel stay from trip {0} with driver {1}";
                SaDebugger.GetCurrentOperation().Description = string.Format(templateStr, trip.Index, driver.GetId());
            }
            #endif

            if (isAddition) {
                (costDiff, costWithoutPenaltyDiff, penaltyDiff, satisfactionDiff, driverInfoDiff) = CostDiffCalculator.GetAddHotelDriverCostDiff(trip, driver, driverInfo, info);
            } else {
                (costDiff, costWithoutPenaltyDiff, penaltyDiff, satisfactionDiff, driverInfoDiff) = CostDiffCalculator.GetRemoveHotelDriverCostDiff(trip, driver, driverInfo, info);
            }

            return (costDiff, satisfactionDiff);
        }

        public override void Execute() {
            info.IsHotelStayAfterTrip[trip.Index] = isAddition;
            info.DriverInfos[driver.AllDriversIndex] += driverInfoDiff;
        }

        public static ToggleHotelOperation CreateRandom(SaInfo info) {
            int tripIndex = info.FastRand.NextInt(info.Instance.Trips.Length);

            bool isAddition = !info.IsHotelStayAfterTrip[tripIndex];

            return new ToggleHotelOperation(tripIndex, isAddition, info);
        }
    }
}

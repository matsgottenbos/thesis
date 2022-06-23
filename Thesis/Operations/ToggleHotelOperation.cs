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
        readonly SaDriverInfo driverInfo;
        SaDriverInfo driverInfoDiff;

        public ToggleHotelOperation(int tripIndex, bool isAddition, SaInfo info) : base(info) {
            trip = info.Instance.Trips[tripIndex];
            this.isAddition = isAddition;
            driver = info.Assignment[tripIndex];
            driverInfo = info.DriverInfos[driver.AllDriversIndex];
        }

        public override SaTotalInfo GetCostDiff() {
            #if DEBUG
            if (AppConfig.DebugCheckOperations) {
                string templateStr = isAddition ? "Add hotel stay to trip {0} with driver {1}" : "Remove hotel stay from trip {0} with driver {1}";
                SaDebugger.GetCurrentOperation().Description = string.Format(templateStr, trip.Index, driver.GetId());
            }
            #endif

            // Get cost diffs
            if (isAddition) {
                driverInfoDiff = CostDiffCalculator.GetAddHotelDriverCostDiff(trip, driver, driverInfo, info);
            } else {
                driverInfoDiff = CostDiffCalculator.GetRemoveHotelDriverCostDiff(trip, driver, driverInfo, info);
            }

            // Store cost diffs in total diff object
            totalInfoDiff.AddDriverInfo(driverInfoDiff);

            // Determine satisfaction score diff
            totalInfoDiff.Stats.SatisfactionScore = SatisfactionCalculator.GetSatisfactionScoreDiff(totalInfoDiff, driver, driverInfoDiff, null, null, info);

            return totalInfoDiff;
        }

        public override void Execute() {
            info.IsHotelStayAfterTrip[trip.Index] = isAddition;
            UpdateDriverInfo(driver, driverInfoDiff);
        }

        public static ToggleHotelOperation CreateRandom(SaInfo info) {
            int tripIndex = info.Instance.Rand.Next(info.Instance.Trips.Length);

            bool isAddition = !info.IsHotelStayAfterTrip[tripIndex];

            return new ToggleHotelOperation(tripIndex, isAddition, info);
        }
    }
}

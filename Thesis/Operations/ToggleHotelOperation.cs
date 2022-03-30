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
        int driverWorkedTimeDiff;

        public ToggleHotelOperation(int tripIndex, bool isAddition, SaInfo info) : base(info) {
            trip = info.Instance.Trips[tripIndex];
            this.isAddition = isAddition;
            driver = info.Assignment[tripIndex];
        }

        public override (double, double, double) GetCostDiff() {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                string templateStr = isAddition ? "Add hotel stay to trip {0} with driver {1}" : "Remove hotel stay from trip {0} with driver {1}";
                SaDebugger.GetCurrentOperation().Description = string.Format(templateStr, trip.Index, driver.GetId());
            }
            #endif
            
            int driverOldWorkedTime = info.DriversWorkedTime[driver.AllDriversIndex];
            (double costDiff, double costWithoutPenalty, double basePenaltyDiff, int driverWorkedTimeDiff) = TravelHelper.AddOrRemoveHotelStay(isAddition, trip, driver, driverOldWorkedTime, info);

            this.driverWorkedTimeDiff = driverWorkedTimeDiff;
            return (costDiff, costWithoutPenalty, basePenaltyDiff);
        }

        public override void Execute() {
            info.IsHotelStayAfterTrip[trip.Index] = isAddition;
            info.DriversWorkedTime[driver.AllDriversIndex] += driverWorkedTimeDiff;
        }

        public static ToggleHotelOperation CreateRandom(SaInfo info) {
            int tripIndex = info.FastRand.NextInt(info.Instance.Trips.Length);

            bool isAddition = !info.IsHotelStayAfterTrip[tripIndex];

            return new ToggleHotelOperation(tripIndex, isAddition, info);
        }
    }
}

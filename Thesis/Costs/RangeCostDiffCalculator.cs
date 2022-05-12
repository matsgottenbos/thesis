using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class RangeCostDiffCalculator {
        public static (double, double, double, double, DriverInfo) GetRangeCostDiff(Trip rangeFirstTrip, Trip rangeLastTrip, DriverInfo oldDriverInfo, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            (double newCostWithoutPenalty, double newPenalty, DriverInfo newRangeDriverInfo) = RangeCostCalculator.GetRangeCost(rangeFirstTrip, rangeLastTrip, newIsHotelAfterTrip, driver, driverPath, info, true);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldDriverInfo, newCostWithoutPenalty, newPenalty, newRangeDriverInfo, driver, driverPath, info);
        }

        public static (double, double, double, double, DriverInfo) GetRangeCostDiffWithUnassign(Trip rangeFirstTrip, Trip rangeLastTrip, DriverInfo oldDriverInfo, Trip unassignedTrip, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            (double newCostWithoutPenalty, double newPenalty, DriverInfo newDriverInfo) = RangeCostCalculator.GetRangeCostWithUnassign(rangeFirstTrip, rangeLastTrip, unassignedTrip, newIsHotelAfterTrip, driver, driverPath, info, true);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldDriverInfo, newCostWithoutPenalty, newPenalty, newDriverInfo, driver, driverPath, info);
        }

        public static (double, double, double, double, DriverInfo) GetRangeCostDiffWithAssign(Trip rangeFirstTrip, Trip rangeLastTrip, DriverInfo oldDriverInfo, Trip assignedTrip, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            (double newCostWithoutPenalty, double newPenalty, DriverInfo newDriverInfo) = RangeCostCalculator.GetRangeCostWithAssign(rangeFirstTrip, rangeLastTrip, assignedTrip, newIsHotelAfterTrip, driver, driverPath, info, true);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldDriverInfo, newCostWithoutPenalty, newPenalty, newDriverInfo, driver, driverPath, info);
        }

        public static (double, double, double, double, DriverInfo) GetRangeCostDiffWithSwap(Trip rangeFirstTrip, Trip rangeLastTrip, DriverInfo oldDriverInfo, Trip unassignedTrip, Trip assignedTrip, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            (double newCostWithoutPenalty, double newPenalty, DriverInfo newDriverInfo) = RangeCostCalculator.GetRangeCostWithSwap(rangeFirstTrip, rangeLastTrip, unassignedTrip, assignedTrip, newIsHotelAfterTrip, driver, driverPath, info, true);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldDriverInfo, newCostWithoutPenalty, newPenalty, newDriverInfo, driver, driverPath, info);
        }

        public static (double, double, double, double, DriverInfo) GetRangeCostDiffFromNewCosts(Trip rangeFirstTrip, Trip rangeLastTrip, DriverInfo oldFullDriverInfo, double newCostWithoutPenalty, double newPartialPenalty, DriverInfo newRangeDriverInfo, Driver driver, List<Trip> driverPath, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentNormalDiff().DriverPathString = ParseHelper.DriverPathToString(driverPath, info);
            }
            #endif

            // Old range cost
            Func<Trip, bool> oldIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];
            (double oldCostWithoutPenalty, double oldPartialPenalty, DriverInfo oldRangeDriverInfo) = RangeCostCalculator.GetRangeCost(rangeFirstTrip, rangeLastTrip, oldIsHotelAfterTrip, driver, driverPath, info, false);
            (double oldPenalty, double oldSatisfaction) = ProcessFullPathValues(oldFullDriverInfo, oldPartialPenalty, driver, false);
            double oldCost = oldCostWithoutPenalty + oldPenalty;

            // Determine full new driver info from partial info
            DriverInfo driverInfoDiff = newRangeDriverInfo - oldRangeDriverInfo;
            DriverInfo newFullDriverInfo = oldFullDriverInfo + driverInfoDiff;

            // New range cost
            (double newPenalty, double newSatisfaction) = ProcessFullPathValues(newFullDriverInfo, newPartialPenalty, driver, true);
            double newCost = newCostWithoutPenalty + newPenalty;

            // Diffs
            double costDiff = newCost - oldCost;
            double costWithoutPenaltyDiff = newCostWithoutPenalty - oldCostWithoutPenalty;
            double penaltyDiff = newPenalty - oldPenalty;
            double driverSatisfactionDiff = newSatisfaction - oldSatisfaction;
            double totalSatisfactionDiff = driverSatisfactionDiff / info.Instance.InternalDrivers.Length;

            return (costDiff, costWithoutPenaltyDiff, penaltyDiff, totalSatisfactionDiff, driverInfoDiff);
        }

        public static (double, double) ProcessFullPathValues(DriverInfo fullDriverInfo, double partialPenalty, Driver driver, bool debugIsNew) {
            // Contract time and shift count penalties
            double penalty = partialPenalty;
            penalty += driver.GetContractTimePenalty(fullDriverInfo.WorkedTime, debugIsNew);
            penalty += PenaltyHelper.GetShiftCountPenalty(fullDriverInfo.ShiftCount, debugIsNew);

            // Satisfaction
            double satisfaction = driver.GetSatisfaction(fullDriverInfo);

            return (penalty, satisfaction);
        }
    }
}

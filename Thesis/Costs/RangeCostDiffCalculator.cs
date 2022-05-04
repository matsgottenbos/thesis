using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class RangeCostDiffCalculator {
        public static (double, double, double, int, int) GetRangeCostDiff(Trip rangeFirstTrip, Trip rangeLastTrip, DriverInfo oldDriverInfo, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            (double newCostWithoutPenalty, double newPenalty, int newWorkedTime, int newShiftCount) = RangeCostCalculator.GetRangeCost(rangeFirstTrip, rangeLastTrip, newIsHotelAfterTrip, driver, driverPath, info, true);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldDriverInfo, newCostWithoutPenalty, newPenalty, newWorkedTime, newShiftCount, driver, driverPath, info);
        }

        public static (double, double, double, int, int) GetRangeCostDiffWithUnassign(Trip rangeFirstTrip, Trip rangeLastTrip, DriverInfo oldDriverInfo, Trip unassignedTrip, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            (double newCostWithoutPenalty, double newPenalty, int newWorkedTime, int newShiftCount) = RangeCostCalculator.GetRangeCostWithUnassign(rangeFirstTrip, rangeLastTrip, unassignedTrip, newIsHotelAfterTrip, driver, driverPath, info, true);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldDriverInfo, newCostWithoutPenalty, newPenalty, newWorkedTime, newShiftCount, driver, driverPath, info);
        }

        public static (double, double, double, int, int) GetRangeCostDiffWithAssign(Trip rangeFirstTrip, Trip rangeLastTrip, int oldFullWorkedTime, int oldFullShiftCount, Trip assignedTrip, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            (double newCostWithoutPenalty, double newPenalty, int newWorkedTime, int newShiftCount) = RangeCostCalculator.GetRangeCostWithAssign(rangeFirstTrip, rangeLastTrip, assignedTrip, newIsHotelAfterTrip, driver, driverPath, info, true);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldFullWorkedTime, oldFullShiftCount, newCostWithoutPenalty, newPenalty, newWorkedTime, newShiftCount, driver, driverPath, info);
        }
        public static (double, double, double, int, int) GetRangeCostDiffWithAssign(Trip rangeFirstTrip, Trip rangeLastTrip, DriverInfo oldDriverInfo, Trip assignedTrip, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            return GetRangeCostDiffWithAssign(rangeFirstTrip, rangeLastTrip, oldDriverInfo.WorkedTime, oldDriverInfo.ShiftCount, assignedTrip, newIsHotelAfterTrip, driver, driverPath, info); ;
        }

        public static (double, double, double, int, int) GetRangeCostDiffWithSwap(Trip rangeFirstTrip, Trip rangeLastTrip, DriverInfo oldDriverInfo, Trip unassignedTrip, Trip assignedTrip, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            (double newCostWithoutPenalty, double newPenalty, int newWorkedTime, int newShiftCount) = RangeCostCalculator.GetRangeCostWithSwap(rangeFirstTrip, rangeLastTrip, unassignedTrip, assignedTrip, newIsHotelAfterTrip, driver, driverPath, info, true);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldDriverInfo, newCostWithoutPenalty, newPenalty, newWorkedTime, newShiftCount, driver, driverPath, info);
        }

        public static (double, double, double, int, int) GetRangeCostDiffFromNewCosts(Trip rangeFirstTrip, Trip rangeLastTrip, int oldFullWorkedTime, int oldFullShiftCount, double newCostWithoutPenalty, double newPenalty, int newWorkedTime, int newShiftCount, Driver driver, List<Trip> driverPath, SaInfo info) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentNormalDiff().DriverPathString = ParseHelper.DriverPathToString(driverPath, info);
            }
            #endif

            // Old range cost
            Func<Trip, bool> oldIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];
            (double oldCostWithoutPenalty, double oldPenalty, int oldWorkedTime, int oldShiftCount) = RangeCostCalculator.GetRangeCost(rangeFirstTrip, rangeLastTrip, oldIsHotelAfterTrip, driver, driverPath, info, false);
            oldPenalty += driver.GetContractTimePenalty(oldFullWorkedTime, false);
            oldPenalty += PenaltyHelper.GetShiftCountPenalty(oldFullShiftCount, false);
            double oldCost = oldCostWithoutPenalty + oldPenalty;

            // New range cost
            int driverNewWorkedTime = oldFullWorkedTime + newWorkedTime - oldWorkedTime;
            newPenalty += driver.GetContractTimePenalty(driverNewWorkedTime, true);
            int driverNewShiftCount = oldFullShiftCount + newShiftCount - oldShiftCount;
            newPenalty += PenaltyHelper.GetShiftCountPenalty(driverNewShiftCount, true);
            double newCost = newCostWithoutPenalty + newPenalty;

            // Diffs
            double costDiff = newCost - oldCost;
            double costWithoutPenaltyDiff = newCostWithoutPenalty - oldCostWithoutPenalty;
            double penaltyDiff = newPenalty - oldPenalty;
            int workedTimeDiff = newWorkedTime - oldWorkedTime;
            int shiftCountDiff = newShiftCount - oldShiftCount;

            return (costDiff, costWithoutPenaltyDiff, penaltyDiff, workedTimeDiff, shiftCountDiff);
        }
        public static (double, double, double, int, int) GetRangeCostDiffFromNewCosts(Trip rangeFirstTrip, Trip rangeLastTrip, DriverInfo oldDriverInfo, double newCostWithoutPenalty, double newPenalty, int newWorkedTime, int newShiftCount, Driver driver, List<Trip> driverPath, SaInfo info) {
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldDriverInfo.WorkedTime, oldDriverInfo.ShiftCount, newCostWithoutPenalty, newPenalty, newWorkedTime, newShiftCount, driver, driverPath, info);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class RangeCostDiffCalculator {
        public static SaDriverInfo GetRangeCostDiff(Trip rangeFirstTrip, Trip rangeLastTrip, SaDriverInfo oldDriverInfo, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            #if DEBUG
            if (AppConfig.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewNormal);
            }
            #endif

            SaDriverInfo newDriverInfo = RangeCostCalculator.GetRangeCost(rangeFirstTrip, rangeLastTrip, newIsHotelAfterTrip, driver, driverPath, info);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldDriverInfo, newDriverInfo, driver, driverPath, info);
        }

        public static SaDriverInfo GetRangeCostDiffWithUnassign(Trip rangeFirstTrip, Trip rangeLastTrip, SaDriverInfo oldDriverInfo, Trip unassignedTrip, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            #if DEBUG
            if (AppConfig.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewNormal);
            }
            #endif

            SaDriverInfo newDriverInfo = RangeCostCalculator.GetRangeCostWithUnassign(rangeFirstTrip, rangeLastTrip, unassignedTrip, newIsHotelAfterTrip, driver, driverPath, info);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldDriverInfo, newDriverInfo, driver, driverPath, info);
        }

        public static SaDriverInfo GetRangeCostDiffWithAssign(Trip rangeFirstTrip, Trip rangeLastTrip, SaDriverInfo oldDriverInfo, Trip assignedTrip, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            #if DEBUG
            if (AppConfig.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewNormal);
            }
            #endif

            SaDriverInfo newDriverInfo = RangeCostCalculator.GetRangeCostWithAssign(rangeFirstTrip, rangeLastTrip, assignedTrip, newIsHotelAfterTrip, driver, driverPath, info);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldDriverInfo, newDriverInfo, driver, driverPath, info);
        }

        public static SaDriverInfo GetRangeCostDiffWithSwap(Trip rangeFirstTrip, Trip rangeLastTrip, SaDriverInfo oldDriverInfo, Trip unassignedTrip, Trip assignedTrip, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            #if DEBUG
            if (AppConfig.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewNormal);
            }
            #endif

            SaDriverInfo newDriverInfo = RangeCostCalculator.GetRangeCostWithSwap(rangeFirstTrip, rangeLastTrip, unassignedTrip, assignedTrip, newIsHotelAfterTrip, driver, driverPath, info);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldDriverInfo, newDriverInfo, driver, driverPath, info);
        }

        public static SaDriverInfo GetRangeCostDiffFromNewCosts(Trip rangeFirstTrip, Trip rangeLastTrip, SaDriverInfo oldFullDriverInfo, SaDriverInfo newRangeDriverInfo, Driver driver, List<Trip> driverPath, SaInfo info) {
            // Old range cost
            #if DEBUG
            if (AppConfig.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.OldNormal);
            }
            #endif
            Func<Trip, bool> oldIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];
            SaDriverInfo oldRangeDriverInfo = RangeCostCalculator.GetRangeCost(rangeFirstTrip, rangeLastTrip, oldIsHotelAfterTrip, driver, driverPath, info);
            ProcessFullPathValues(oldRangeDriverInfo, oldFullDriverInfo, driver, info);

            // Finish new range cost
            #if DEBUG
            if (AppConfig.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewNormal);
            }
            #endif
            SaDriverInfo newFullDriverInfo = oldFullDriverInfo + newRangeDriverInfo - oldRangeDriverInfo;
            ProcessFullPathValues(newRangeDriverInfo, newFullDriverInfo, driver, info);

            // Full diffs
            SaDriverInfo rangeDriverInfoDiff = newRangeDriverInfo - oldRangeDriverInfo;
            return rangeDriverInfoDiff;
        }

        public static void ProcessFullPathValues(SaDriverInfo rangeDriverInfo, SaDriverInfo fullDriverInfo, Driver driver, SaInfo info) {
            // Chift count penalty
            rangeDriverInfo.PenaltyInfo.AddPossibleShiftCountViolation(fullDriverInfo.ShiftCount);
            rangeDriverInfo.Stats.Penalty = rangeDriverInfo.PenaltyInfo.GetPenalty();

            // Driver satisfaction
            rangeDriverInfo.Stats.DriverSatisfaction = driver.GetSatisfaction(fullDriverInfo);

            // Cost
            rangeDriverInfo.Stats.Cost = rangeDriverInfo.Stats.RawCost + rangeDriverInfo.Stats.Robustness + rangeDriverInfo.Stats.Penalty;
        }
    }
}

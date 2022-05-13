using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class RangeCostDiffCalculator {
        public static DriverInfo GetRangeCostDiff(Trip rangeFirstTrip, Trip rangeLastTrip, DriverInfo oldDriverInfo, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewNormal);
            DriverInfo newDriverInfo = RangeCostCalculator.GetRangeCost(rangeFirstTrip, rangeLastTrip, newIsHotelAfterTrip, driver, driverPath, info);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldDriverInfo, newDriverInfo, driver, driverPath, info);
        }

        public static DriverInfo GetRangeCostDiffWithUnassign(Trip rangeFirstTrip, Trip rangeLastTrip, DriverInfo oldDriverInfo, Trip unassignedTrip, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewNormal);
            DriverInfo newDriverInfo = RangeCostCalculator.GetRangeCostWithUnassign(rangeFirstTrip, rangeLastTrip, unassignedTrip, newIsHotelAfterTrip, driver, driverPath, info);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldDriverInfo, newDriverInfo, driver, driverPath, info);
        }

        public static DriverInfo GetRangeCostDiffWithAssign(Trip rangeFirstTrip, Trip rangeLastTrip, DriverInfo oldDriverInfo, Trip assignedTrip, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewNormal);
            DriverInfo newDriverInfo = RangeCostCalculator.GetRangeCostWithAssign(rangeFirstTrip, rangeLastTrip, assignedTrip, newIsHotelAfterTrip, driver, driverPath, info);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldDriverInfo, newDriverInfo, driver, driverPath, info);
        }

        public static DriverInfo GetRangeCostDiffWithSwap(Trip rangeFirstTrip, Trip rangeLastTrip, DriverInfo oldDriverInfo, Trip unassignedTrip, Trip assignedTrip, Func<Trip, bool> newIsHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewNormal);
            DriverInfo newDriverInfo = RangeCostCalculator.GetRangeCostWithSwap(rangeFirstTrip, rangeLastTrip, unassignedTrip, assignedTrip, newIsHotelAfterTrip, driver, driverPath, info);
            return GetRangeCostDiffFromNewCosts(rangeFirstTrip, rangeLastTrip, oldDriverInfo, newDriverInfo, driver, driverPath, info);
        }

        public static DriverInfo GetRangeCostDiffFromNewCosts(Trip rangeFirstTrip, Trip rangeLastTrip, DriverInfo oldFullDriverInfo, DriverInfo newRangeDriverInfo, Driver driver, List<Trip> driverPath, SaInfo info) {
            // Old range cost
            SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.OldNormal);
            Func<Trip, bool> oldIsHotelAfterTrip = (Trip trip) => info.IsHotelStayAfterTrip[trip.Index];
            DriverInfo oldRangeDriverInfo = RangeCostCalculator.GetRangeCost(rangeFirstTrip, rangeLastTrip, oldIsHotelAfterTrip, driver, driverPath, info);
            ProcessFullPathValues(oldRangeDriverInfo, oldFullDriverInfo, driver, info);

            // Finish new range cost
            DriverInfo newFullDriverInfo = oldFullDriverInfo + newRangeDriverInfo - oldRangeDriverInfo;
            ProcessFullPathValues(newRangeDriverInfo, newFullDriverInfo, driver, info);

            // Full diffs
            DriverInfo fullDriverInfoDiff = newFullDriverInfo - oldFullDriverInfo;

            return fullDriverInfoDiff;
        }

        public static void ProcessFullPathValues(DriverInfo rangeDriverInfo, DriverInfo fullDriverInfo, Driver driver, SaInfo info) {
            // Contract time and shift count penalties
            rangeDriverInfo.PenaltyInfo.AddPossibleContractTimeViolation(fullDriverInfo.WorkedTime, driver);
            rangeDriverInfo.PenaltyInfo.AddPossibleShiftCountViolation(fullDriverInfo.ShiftCount);
            rangeDriverInfo.Penalty = rangeDriverInfo.PenaltyInfo.GetPenalty();

            // Satisfaction
            rangeDriverInfo.DriverSatisfaction = driver.GetSatisfaction(fullDriverInfo);
            rangeDriverInfo.Satisfaction = rangeDriverInfo.DriverSatisfaction / info.Instance.InternalDrivers.Length;

            // Cost
            rangeDriverInfo.Cost = rangeDriverInfo.CostWithoutPenalty + rangeDriverInfo.Penalty;
        }
    }
}

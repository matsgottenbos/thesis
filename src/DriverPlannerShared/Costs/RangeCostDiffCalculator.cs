/*
 * Calculates cost differences for a range of activities for a driver
*/

using System;
using System.Collections.Generic;

namespace DriverPlannerShared {
    public static class RangeCostDiffCalculator {
        public static SaDriverInfo GetRangeCostDiff(Activity rangeFirstActivity, Activity rangeLastActivity, SaDriverInfo oldDriverInfo, Func<Activity, bool> newIsHotelAfterActivity, Driver driver, List<Activity> driverPath, SaInfo info) {
#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewNormal);
            }
#endif

            SaDriverInfo newDriverInfo = RangeCostCalculator.GetRangeCost(rangeFirstActivity, rangeLastActivity, newIsHotelAfterActivity, driver, driverPath, info);
            return GetRangeCostDiffFromNewCosts(rangeFirstActivity, rangeLastActivity, oldDriverInfo, newDriverInfo, driver, driverPath, info);
        }

        public static SaDriverInfo GetRangeCostDiffWithUnassign(Activity rangeFirstActivity, Activity rangeLastActivity, SaDriverInfo oldDriverInfo, Activity unassignedActivity, Func<Activity, bool> newIsHotelAfterActivity, Driver driver, List<Activity> driverPath, SaInfo info) {
#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewNormal);
            }
#endif

            SaDriverInfo newDriverInfo = RangeCostCalculator.GetRangeCostWithUnassign(rangeFirstActivity, rangeLastActivity, unassignedActivity, newIsHotelAfterActivity, driver, driverPath, info);
            return GetRangeCostDiffFromNewCosts(rangeFirstActivity, rangeLastActivity, oldDriverInfo, newDriverInfo, driver, driverPath, info);
        }

        public static SaDriverInfo GetRangeCostDiffWithAssign(Activity rangeFirstActivity, Activity rangeLastActivity, SaDriverInfo oldDriverInfo, Activity assignedActivity, Func<Activity, bool> newIsHotelAfterActivity, Driver driver, List<Activity> driverPath, SaInfo info) {
#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewNormal);
            }
#endif

            SaDriverInfo newDriverInfo = RangeCostCalculator.GetRangeCostWithAssign(rangeFirstActivity, rangeLastActivity, assignedActivity, newIsHotelAfterActivity, driver, driverPath, info);
            return GetRangeCostDiffFromNewCosts(rangeFirstActivity, rangeLastActivity, oldDriverInfo, newDriverInfo, driver, driverPath, info);
        }

        public static SaDriverInfo GetRangeCostDiffWithSwap(Activity rangeFirstActivity, Activity rangeLastActivity, SaDriverInfo oldDriverInfo, Activity unassignedActivity, Activity assignedActivity, Func<Activity, bool> newIsHotelAfterActivity, Driver driver, List<Activity> driverPath, SaInfo info) {
#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewNormal);
            }
#endif

            SaDriverInfo newDriverInfo = RangeCostCalculator.GetRangeCostWithSwap(rangeFirstActivity, rangeLastActivity, unassignedActivity, assignedActivity, newIsHotelAfterActivity, driver, driverPath, info);
            return GetRangeCostDiffFromNewCosts(rangeFirstActivity, rangeLastActivity, oldDriverInfo, newDriverInfo, driver, driverPath, info);
        }

        public static SaDriverInfo GetRangeCostDiffFromNewCosts(Activity rangeFirstActivity, Activity rangeLastActivity, SaDriverInfo oldFullDriverInfo, SaDriverInfo newRangeDriverInfo, Driver driver, List<Activity> driverPath, SaInfo info) {
            // Old range cost
#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.OldNormal);
            }
#endif
            Func<Activity, bool> oldIsHotelAfterActivity = (Activity activity) => info.IsHotelStayAfterActivity[activity.Index];
            SaDriverInfo oldRangeDriverInfo = RangeCostCalculator.GetRangeCost(rangeFirstActivity, rangeLastActivity, oldIsHotelAfterActivity, driver, driverPath, info);
            ProcessFullPathValues(oldRangeDriverInfo, oldFullDriverInfo, driver, info);

            // Finish new range cost
#if DEBUG
            if (DevConfig.DebugCheckOperations) {
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

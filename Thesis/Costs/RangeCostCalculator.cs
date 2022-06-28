using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class RangeCostCalculator {
        /** Get costs of part of a driver's path; penalty are computed with without worked time and shift count penalties */
        public static SaDriverInfo GetRangeCost(Activity rangeFirstActivity, Activity rangeLastActivity, Func<Activity, bool> isHotelAfterActivityFunc, Driver driver, List<Activity> driverPath, SaInfo info) {
            SaDriverInfo driverInfo = new SaDriverInfo(info.Instance);
            if (driverPath.Count == 0) return driverInfo;

            int rangeFirstPathIndex = info.DriverPathIndices[rangeFirstActivity.Index];
            int rangeLastPathIndex = info.DriverPathIndices[rangeLastActivity.Index];
            Activity shiftFirstActivity = rangeFirstActivity, parkingActivity = rangeFirstActivity, prevActivity = rangeFirstActivity, beforeHotelActivity = null;
            for (int pathActivityIndex = rangeFirstPathIndex + 1; pathActivityIndex <= rangeLastPathIndex; pathActivityIndex++) {
                Activity searchActivity = driverPath[pathActivityIndex];
                RangeCostActivityProcessor.ProcessDriverActivity(searchActivity, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivityFunc, driverInfo, driver, info, info.Instance);
            }
            Activity activityAfterRange = rangeLastPathIndex + 1 < driverPath.Count ? driverPath[rangeLastPathIndex + 1] : null;
            RangeCostActivityProcessor.ProcessDriverEndRange(activityAfterRange, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivityFunc, driverInfo, driver, info, info.Instance);
            return driverInfo;
        }

        /** Get costs of part of a driver's path where a activity is unassigned; penalty are computed with without worked time and shift count penalties */
        public static SaDriverInfo GetRangeCostWithUnassign(Activity rangeFirstActivity, Activity rangeLastActivity, Activity unassignedActivity, Func<Activity, bool> isHotelAfterActivityFunc, Driver driver, List<Activity> driverPath, SaInfo info) {
            SaDriverInfo driverInfo = new SaDriverInfo(info.Instance);
            int rangeFirstPathIndex = info.DriverPathIndices[rangeFirstActivity.Index];
            int rangeLastPathIndex = info.DriverPathIndices[rangeLastActivity.Index];
            Activity shiftFirstActivity = null, parkingActivity = null, prevActivity = null, beforeHotelActivity = null;
            for (int pathActivityIndex = rangeFirstPathIndex; pathActivityIndex <= rangeLastPathIndex; pathActivityIndex++) {
                Activity searchActivity = driverPath[pathActivityIndex];
                if (searchActivity == unassignedActivity) continue;

                if (shiftFirstActivity == null) {
                    shiftFirstActivity = parkingActivity = prevActivity = searchActivity;
                    continue;
                }

                RangeCostActivityProcessor.ProcessDriverActivity(searchActivity, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivityFunc, driverInfo, driver, info, info.Instance);
            }
            Activity activityAfterRange = rangeLastPathIndex + 1 < driverPath.Count ? driverPath[rangeLastPathIndex + 1] : null;
            RangeCostActivityProcessor.ProcessDriverEndRange(activityAfterRange, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivityFunc, driverInfo, driver, info, info.Instance);
            return driverInfo;
        }

        /** Get costs of part of a driver's path where a activity is assigned; penalty are computed with without worked time and shift count penalties */
        public static SaDriverInfo GetRangeCostWithAssign(Activity rangeFirstActivity, Activity rangeLastActivity, Activity assignedActivity, Func<Activity, bool> isHotelAfterActivityFunc, Driver driver, List<Activity> driverPath, SaInfo info) {
            SaDriverInfo driverInfo = new SaDriverInfo(info.Instance);
            Activity shiftFirstActivity = null, parkingActivity = null, prevActivity = null, beforeHotelActivity = null;

            if (driverPath.Count == 0) {
                // New path only contains assigned activity
                shiftFirstActivity = parkingActivity = prevActivity = assignedActivity;
                RangeCostActivityProcessor.ProcessDriverEndRange(null, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivityFunc, driverInfo, driver, info, info.Instance);
                return driverInfo;
            }

            int rangeFirstPathIndex = info.DriverPathIndices[rangeFirstActivity.Index];
            int rangeLastPathIndex = info.DriverPathIndices[rangeLastActivity.Index];

            // Process part before assigned activity
            int pathActivityIndex;
            for (pathActivityIndex = rangeFirstPathIndex; pathActivityIndex <= rangeLastPathIndex; pathActivityIndex++) {
                Activity searchActivity = driverPath[pathActivityIndex];
                if (searchActivity.Index > assignedActivity.Index) break;

                if (shiftFirstActivity == null) {
                    shiftFirstActivity = parkingActivity = prevActivity = searchActivity;
                    continue;
                }

                RangeCostActivityProcessor.ProcessDriverActivity(searchActivity, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivityFunc, driverInfo, driver, info, info.Instance);
            }

            // Process assigned activity
            if (shiftFirstActivity == null) {
                shiftFirstActivity = parkingActivity = prevActivity = assignedActivity;
            } else {
                RangeCostActivityProcessor.ProcessDriverActivity(assignedActivity, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivityFunc, driverInfo, driver, info, info.Instance);
            }

            // Process part after assigned activity
            for (; pathActivityIndex <= rangeLastPathIndex; pathActivityIndex++) {
                Activity searchActivity = driverPath[pathActivityIndex];
                RangeCostActivityProcessor.ProcessDriverActivity(searchActivity, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivityFunc, driverInfo, driver, info, info.Instance);
            }

            Activity activityAfterRange = rangeLastPathIndex + 1 < driverPath.Count ? driverPath[rangeLastPathIndex + 1] : null;
            RangeCostActivityProcessor.ProcessDriverEndRange(activityAfterRange, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivityFunc, driverInfo, driver, info, info.Instance);
            return driverInfo;
        }

        /** Get costs of part of a driver's path where a activity is unassigned and another assigned; penalty are computed with without worked time and shift count penalties */
        public static SaDriverInfo GetRangeCostWithSwap(Activity rangeFirstActivity, Activity rangeLastActivity, Activity unassignedActivity, Activity assignedActivity, Func<Activity, bool> isHotelAfterActivityFunc, Driver driver, List<Activity> driverPath, SaInfo info) {
            SaDriverInfo driverInfo = new SaDriverInfo(info.Instance);
            Activity shiftFirstActivity = null, parkingActivity = null, prevActivity = null, beforeHotelActivity = null;

            if (driverPath.Count == 0) {
                // New path only contains assigned activity
                RangeCostActivityProcessor.ProcessDriverActivity(assignedActivity, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivityFunc, driverInfo, driver, info, info.Instance);
                RangeCostActivityProcessor.ProcessDriverEndRange(null, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivityFunc, driverInfo, driver, info, info.Instance);
                return driverInfo;
            }

            int rangeFirstPathIndex = info.DriverPathIndices[rangeFirstActivity.Index];
            int rangeLastPathIndex = info.DriverPathIndices[rangeLastActivity.Index];

            // Process part before assigned activity
            int pathActivityIndex;
            for (pathActivityIndex = rangeFirstPathIndex; pathActivityIndex <= rangeLastPathIndex; pathActivityIndex++) {
                Activity searchActivity = driverPath[pathActivityIndex];
                if (searchActivity.Index > assignedActivity.Index) break;
                if (searchActivity == unassignedActivity) continue;

                if (shiftFirstActivity == null) {
                    shiftFirstActivity = parkingActivity = prevActivity = searchActivity;
                    continue;
                }

                RangeCostActivityProcessor.ProcessDriverActivity(searchActivity, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivityFunc, driverInfo, driver, info, info.Instance);
            }

            // Process assigned activity
            if (shiftFirstActivity == null) {
                shiftFirstActivity = parkingActivity = prevActivity = assignedActivity;
            } else {
                RangeCostActivityProcessor.ProcessDriverActivity(assignedActivity, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivityFunc, driverInfo, driver, info, info.Instance);
            }

            // Process part after assigned activity
            for (; pathActivityIndex <= rangeLastPathIndex; pathActivityIndex++) {
                Activity searchActivity = driverPath[pathActivityIndex];
                if (searchActivity == unassignedActivity) continue;
                RangeCostActivityProcessor.ProcessDriverActivity(searchActivity, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivityFunc, driverInfo, driver, info, info.Instance);
            }

            Activity activityAfterRange = rangeLastPathIndex + 1 < driverPath.Count ? driverPath[rangeLastPathIndex + 1] : null;
            RangeCostActivityProcessor.ProcessDriverEndRange(activityAfterRange, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivityFunc, driverInfo, driver, info, info.Instance);
            return driverInfo;
        }
    }
}

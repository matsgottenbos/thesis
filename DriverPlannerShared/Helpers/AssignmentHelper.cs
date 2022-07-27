/*
 * Helper methods for activity assignments
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public static class AssignmentHelper {
        public static int GetAssignedPathActivityIndexBefore(Activity assignedActivity, List<Activity> driverPath) {
            int pathActivityIndex;
            for (pathActivityIndex = 0; pathActivityIndex < driverPath.Count; pathActivityIndex++) {
                if (driverPath[pathActivityIndex].Index >= assignedActivity.Index) break;
            }
            return pathActivityIndex - 1;
        }

        /** Returns driver's first activity of shift, and last activity of previous shift */
        public static (Activity, Activity) GetShiftFirstActivityAndPrevShiftActivity(Activity activity, int pathActivityIndexBefore, List<Activity> driverPath, SaInfo info) {
            Activity shiftFirstActivity = activity;
            for (int pathActivityIndex = pathActivityIndexBefore; pathActivityIndex >= 0; pathActivityIndex--) {
                Activity searchActivity = driverPath[pathActivityIndex];
                if (info.Instance.AreSameShift(searchActivity, shiftFirstActivity)) {
                    shiftFirstActivity = searchActivity;
                } else {
                    return (shiftFirstActivity, searchActivity);
                }
            }
            return (shiftFirstActivity, null);
        }

        public static (Activity, Activity) GetShiftFirstActivityAndPrevShiftActivity(Activity activity, List<Activity> driverPath, SaInfo info) {
            int pathActivityIndexBefore = info.DriverPathIndices[activity.Index] - 1;
            return GetShiftFirstActivityAndPrevShiftActivity(activity, pathActivityIndexBefore, driverPath, info);
        }

        /** Returns driver's last activity of shift, and first activity of next shift */
        public static (Activity, Activity) GetShiftLastActivityAndNextShiftActivity(Activity activity, int pathActivityIndexAfter, List<Activity> driverPath, SaInfo info) {
            Activity shiftLastActivity = activity;
            for (int pathActivityIndex = pathActivityIndexAfter; pathActivityIndex < driverPath.Count; pathActivityIndex++) {
                Activity searchActivity = driverPath[pathActivityIndex];
                if (info.Instance.AreSameShift(shiftLastActivity, searchActivity)) {
                    shiftLastActivity = searchActivity;
                } else {
                    return (shiftLastActivity, searchActivity);
                }
            }
            return (shiftLastActivity, null);
        }

        public static (Activity, Activity) GetShiftLastActivityAndNextShiftActivity(Activity activity, List<Activity> driverPath, SaInfo info) {
            int pathActivityIndexAfter = info.DriverPathIndices[activity.Index] + 1;
            return GetShiftLastActivityAndNextShiftActivity(activity, pathActivityIndexAfter, driverPath, info);
        }
    }
}

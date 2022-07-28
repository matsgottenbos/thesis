/*
 * Calculates cost differences for changes to a single driver
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace DriverPlannerShared {
    public static class CostDiffCalculator {
        static string GetRangeString(Activity firstRelevantActivity, Activity lastRelevantActivity) {
            return string.Format("{0}--{1}", ToStringHelper.ActivityToIndexOrUnderscore(firstRelevantActivity), ToStringHelper.ActivityToIndexOrUnderscore(lastRelevantActivity));
        }

        static string GetNormalRangeInfo(Activity firstRelevantActivity, Activity lastRelevantActivity) {
            return "Relevant range: " + GetRangeString(firstRelevantActivity, lastRelevantActivity);
        }


        /* Operation cost diffs */

        public static SaDriverInfo GetUnassignDriverCostDiff(Activity unassignedActivity, Driver driver, SaDriverInfo oldDriverInfo, SaInfo info) {
#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Unassign activity {0} from driver {1}", unassignedActivity.Index, driver.GetId()), driver);
            }
#endif

            List<Activity> driverPath = info.DriverPaths[driver.AllDriversIndex];
            (Activity firstRelevantActivity, Activity lastRelevantActivity) = GetActivityRelevantRange(unassignedActivity, driverPath, info);
            Func<Activity, bool> newIsHotelAfterActivity = (Activity activity) => info.IsHotelStayAfterActivity[activity.Index];
            SaDriverInfo driverInfoDiff = RangeCostDiffCalculator.GetRangeCostDiffWithUnassign(firstRelevantActivity, lastRelevantActivity, oldDriverInfo, unassignedActivity, newIsHotelAfterActivity, driver, driverPath, info);

#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                string relevantRangeInfo = GetNormalRangeInfo(firstRelevantActivity, lastRelevantActivity);
                CheckErrors(driverInfoDiff, unassignedActivity, null, null, null, driver, info);
            }
#endif

            return driverInfoDiff;
        }

        public static SaDriverInfo GetAssignDriverCostDiff(Activity assignedActivity, Driver driver, SaDriverInfo oldDriverInfo, SaInfo info) {
#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Assign activity {0} to driver {1}", assignedActivity.Index, driver.GetId()), driver);
            }
#endif

            List<Activity> driverPath = info.DriverPaths[driver.AllDriversIndex];
            (Activity firstRelevantActivity, Activity lastRelevantActivity) = GetActivityRelevantRangeWithAssign(assignedActivity, driverPath, info);
            Func<Activity, bool> newIsHotelAfterActivity = (Activity activity) => info.IsHotelStayAfterActivity[activity.Index];
            SaDriverInfo driverInfoDiff = RangeCostDiffCalculator.GetRangeCostDiffWithAssign(firstRelevantActivity, lastRelevantActivity, oldDriverInfo, assignedActivity, newIsHotelAfterActivity, driver, driverPath, info);

#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                string relevantRangeInfo = GetNormalRangeInfo(firstRelevantActivity, lastRelevantActivity);
                CheckErrors(driverInfoDiff, null, assignedActivity, null, null, driver, info);
            }
#endif

            return driverInfoDiff;
        }

        public static SaDriverInfo GetSwapDriverCostDiff(Activity unassignedActivity, Activity assignedActivity, Driver driver, SaDriverInfo oldDriverInfo, SaInfo info) {
#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Unassign activity {0} from and assign activity {1} to driver {2}", unassignedActivity.Index, assignedActivity.Index, driver.GetId()), driver);
            }
#endif

            List<Activity> driverPath = info.DriverPaths[driver.AllDriversIndex];
            (Activity unassignFirstRelevantActivity, Activity unassignLastRelevantActivity) = GetActivityRelevantRange(unassignedActivity, driverPath, info);
            (Activity assignFirstRelevantActivity, Activity assignLastRelevantActivity) = GetActivityRelevantRangeWithAssign(assignedActivity, driverPath, info);

#if DEBUG
            string unassignRangeString = null, assignRangeString = null;
            if (DevConfig.DebugCheckOperations) {
                unassignRangeString = GetRangeString(unassignFirstRelevantActivity, unassignLastRelevantActivity);
                assignRangeString = GetRangeString(assignFirstRelevantActivity, assignLastRelevantActivity);
            }
#endif

            if (unassignLastRelevantActivity.Index >= assignFirstRelevantActivity.Index) {
                // Overlap, so calculate diff together
                Activity combinedFirstRelevantActivity = unassignFirstRelevantActivity.Index < assignFirstRelevantActivity.Index ? unassignFirstRelevantActivity : assignFirstRelevantActivity;
                Activity combinedLastRelevantActivity = unassignLastRelevantActivity.Index > assignLastRelevantActivity.Index ? unassignLastRelevantActivity : assignLastRelevantActivity;
                Func<Activity, bool> combinedNewIsDriverActivity = (Activity activity) => info.Assignment[activity.Index] == driver && activity != unassignedActivity || activity == assignedActivity;
                Func<Activity, bool> combinedNewIsHotelAfterActivity = (Activity activity) => info.IsHotelStayAfterActivity[activity.Index];
                SaDriverInfo driverInfoDiff = RangeCostDiffCalculator.GetRangeCostDiffWithSwap(combinedFirstRelevantActivity, combinedLastRelevantActivity, oldDriverInfo, unassignedActivity, assignedActivity, combinedNewIsHotelAfterActivity, driver, driverPath, info);

#if DEBUG
                if (DevConfig.DebugCheckOperations) {
                    string combinedRangeString = GetRangeString(combinedFirstRelevantActivity, combinedLastRelevantActivity);
                    string relevantRangeInfo = string.Format("Unassign relevant range: {0}; Assign relevant range: {1}; Combined relevant range: {2}", unassignRangeString, assignRangeString, combinedRangeString);
                    CheckErrors(driverInfoDiff, unassignedActivity, assignedActivity, null, null, driver, info);
                }
#endif

                return driverInfoDiff;
            } else {
                // No overlap, so calculate diffs separately
                // Unassign diff
                Func<Activity, bool> unassignNewIsHotelAfterActivity = (Activity activity) => info.IsHotelStayAfterActivity[activity.Index];
                SaDriverInfo unassignDriverInfoDiff = RangeCostDiffCalculator.GetRangeCostDiffWithUnassign(unassignFirstRelevantActivity, unassignLastRelevantActivity, oldDriverInfo, unassignedActivity, unassignNewIsHotelAfterActivity, driver, driverPath, info);

                // Assign diff
                Func<Activity, bool> assignNewIsHotelAfterActivity = (Activity activity) => info.IsHotelStayAfterActivity[activity.Index];
                SaDriverInfo driverInfoAfterUnassign = oldDriverInfo + unassignDriverInfoDiff;
                SaDriverInfo assignDriverInfoDiff = RangeCostDiffCalculator.GetRangeCostDiffWithAssign(assignFirstRelevantActivity, assignLastRelevantActivity, driverInfoAfterUnassign, assignedActivity, assignNewIsHotelAfterActivity, driver, driverPath, info);

                // Total diff
                SaDriverInfo driverInfoDiff = unassignDriverInfoDiff + assignDriverInfoDiff;

#if DEBUG
                if (DevConfig.DebugCheckOperations) {
                    string relevantRangeInfo = string.Format("Unassign relevant range: {0}; Assign relevant range: {1}; Calculated separately", unassignRangeString, assignRangeString);
                    CheckErrors(driverInfoDiff, unassignedActivity, assignedActivity, null, null, driver, info);
                }
#endif

                return driverInfoDiff;
            }
        }

        public static SaDriverInfo GetAddHotelDriverCostDiff(Activity addedHotelActivity, Driver driver, SaDriverInfo oldDriverInfo, SaInfo info) {
#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Add hotel after {0} for driver {1}", addedHotelActivity.Index, driver.GetId()), driver);
            }
#endif

            List<Activity> driverPath = info.DriverPaths[driver.AllDriversIndex];
            (Activity firstRelevantActivity, Activity lastRelevantActivity) = GetActivityRelevantRange(addedHotelActivity, driverPath, info);
            Func<Activity, bool> newIsHotelAfterActivity = (Activity activity) => info.IsHotelStayAfterActivity[activity.Index] || activity == addedHotelActivity;
            SaDriverInfo driverInfoDiff = RangeCostDiffCalculator.GetRangeCostDiff(firstRelevantActivity, lastRelevantActivity, oldDriverInfo, newIsHotelAfterActivity, driver, driverPath, info);

#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                string relevantRangeInfo = GetNormalRangeInfo(firstRelevantActivity, lastRelevantActivity);
                CheckErrors(driverInfoDiff, null, null, addedHotelActivity, null, driver, info);
            }
#endif

            return driverInfoDiff;
        }

        public static SaDriverInfo GetRemoveHotelDriverCostDiff(Activity removedHotelActivity, Driver driver, SaDriverInfo oldDriverInfo, SaInfo info) {
#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Remove hotel after {0} for driver {1}", removedHotelActivity.Index, driver.GetId()), driver);
            }
#endif

            List<Activity> driverPath = info.DriverPaths[driver.AllDriversIndex];
            (Activity firstRelevantActivity, Activity lastRelevantActivity) = GetActivityRelevantRange(removedHotelActivity, driverPath, info);
            Func<Activity, bool> newIsHotelAfterActivity = (Activity activity) => info.IsHotelStayAfterActivity[activity.Index] && activity != removedHotelActivity;
            SaDriverInfo driverInfoDiff = RangeCostDiffCalculator.GetRangeCostDiff(firstRelevantActivity, lastRelevantActivity, oldDriverInfo, newIsHotelAfterActivity, driver, driverPath, info);

#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                string relevantRangeInfo = GetNormalRangeInfo(firstRelevantActivity, lastRelevantActivity);
                CheckErrors(driverInfoDiff, null, null, null, removedHotelActivity, driver, info);
            }
#endif

            return driverInfoDiff;
        }


        /* Relevant range */

        static (Activity, Activity) GetActivityRelevantRange(Activity activity, List<Activity> driverPath, SaInfo info) {
            if (driverPath.Count == 0) return (activity, activity);
            (Activity shiftFirstActivity, Activity prevShiftLastActivity) = AssignmentHelper.GetShiftFirstActivityAndPrevShiftActivity(activity, driverPath, info);
            (Activity shiftLastActivity, Activity nextShiftFirstActivity) = AssignmentHelper.GetShiftLastActivityAndNextShiftActivity(activity, driverPath, info);
            return GetActivityRelevantRange(activity, shiftFirstActivity, prevShiftLastActivity, shiftLastActivity, nextShiftFirstActivity, driverPath, info);
        }

        static (Activity, Activity) GetActivityRelevantRangeWithAssign(Activity assignedActivity, List<Activity> driverPath, SaInfo info) {
            if (driverPath.Count == 0) return (null, null);

            int pathActivityIndex = AssignmentHelper.GetAssignedPathActivityIndexBefore(assignedActivity, driverPath);
            (Activity shiftFirstActivity, Activity prevShiftLastActivity) = AssignmentHelper.GetShiftFirstActivityAndPrevShiftActivity(assignedActivity, pathActivityIndex, driverPath, info);
            (Activity shiftLastActivity, Activity nextShiftFirstActivity) = AssignmentHelper.GetShiftLastActivityAndNextShiftActivity(assignedActivity, pathActivityIndex + 1, driverPath, info);

            (Activity firstRelevantActivity, Activity lastRelevantActivity) = GetActivityRelevantRange(assignedActivity, shiftFirstActivity, prevShiftLastActivity, shiftLastActivity, nextShiftFirstActivity, driverPath, info);

            // Ensure first and last activitys are not the assigned activity; this can only happen if before the first or after the last activity in the path, respectively
            if (firstRelevantActivity == assignedActivity) firstRelevantActivity = driverPath[0];
            else if (lastRelevantActivity == assignedActivity) lastRelevantActivity = driverPath[driverPath.Count - 1];

            return (firstRelevantActivity, lastRelevantActivity);
        }

        static (Activity, Activity) GetActivityRelevantRange(Activity activity, Activity shiftFirstActivity, Activity prevShiftLastActivity, Activity shiftLastActivity, Activity nextShiftFirstActivity, List<Activity> driverPath, SaInfo info) {
            // If this activity is the first in shift, first relevant activity is the first activity connected to the *previous* shift by hotel stays
            // Else, first relevant activity is the first activity connected to the *current* shift by hotel stays
            bool isFirstActivityInShift = shiftFirstActivity == activity;
            if (prevShiftLastActivity != null) {
                if (isFirstActivityInShift) {
                    (shiftFirstActivity, prevShiftLastActivity) = AssignmentHelper.GetShiftFirstActivityAndPrevShiftActivity(prevShiftLastActivity, driverPath, info);
                }
                while (prevShiftLastActivity != null && info.IsHotelStayAfterActivity[prevShiftLastActivity.Index]) {
                    (shiftFirstActivity, prevShiftLastActivity) = AssignmentHelper.GetShiftFirstActivityAndPrevShiftActivity(prevShiftLastActivity, driverPath, info);
                }
            }

            // If this activity is the first or last in shift, last relevant activity is the last activity connected to the next shift by hotel stays
            // Else, last relevant activity is last activity of shift
            if ((isFirstActivityInShift || shiftLastActivity == activity) && nextShiftFirstActivity != null) {
                (shiftLastActivity, nextShiftFirstActivity) = AssignmentHelper.GetShiftLastActivityAndNextShiftActivity(nextShiftFirstActivity, driverPath, info);
                while (nextShiftFirstActivity != null && info.IsHotelStayAfterActivity[shiftLastActivity.Index]) {
                    (shiftLastActivity, nextShiftFirstActivity) = AssignmentHelper.GetShiftLastActivityAndNextShiftActivity(nextShiftFirstActivity, driverPath, info);
                }
            }

            // Return first and last activity in range, as well as activity before and after range
            return (shiftFirstActivity, shiftLastActivity);
        }


        /* Debugging */

        static void CheckErrors(SaDriverInfo operationDriverInfoDiff, Activity unassignedActivity, Activity assignedActivity, Activity addedHotel, Activity removedHotel, Driver driver, SaInfo info) {
            List<Activity> oldDriverPath = info.DriverPaths[driver.AllDriversIndex];

            // Old operation info
            SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.OldNormal);
            SaDriverInfo oldOperationDriverInfo = info.DriverInfos[driver.AllDriversIndex];
            SaDebugger.GetCurrentStageInfo().SetDriverInfo(oldOperationDriverInfo);

            // New operation info
            SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewNormal);
            SaDriverInfo newOperationDriverInfo = oldOperationDriverInfo + operationDriverInfoDiff;
            SaDebugger.GetCurrentStageInfo().SetDriverInfo(newOperationDriverInfo);

            // Old checked info
            SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.OldChecked);
            SaDriverInfo oldCheckedDriverInfo = TotalCostCalculator.GetDriverInfo(oldDriverPath, info.IsHotelStayAfterActivity, driver, info);
            SaDebugger.GetCurrentStageInfo().SetDriverInfo(oldCheckedDriverInfo);

            // Get driver path after
            List<Activity> newDriverPath = oldDriverPath.Copy();
            if (unassignedActivity != null) {
                int removedCount = newDriverPath.RemoveAll(searchActivity => searchActivity.Index == unassignedActivity.Index);
                if (removedCount != 1) throw new Exception("Error removing activity from driver path");
            }
            if (assignedActivity != null) {
                newDriverPath.Add(assignedActivity);
                newDriverPath = newDriverPath.OrderBy(searchActivity => searchActivity.Index).ToList();
            }

            // Get hotel stays after
            bool[] newIsHotelStayAfterActivity = info.IsHotelStayAfterActivity.Copy();
            if (addedHotel != null) newIsHotelStayAfterActivity[addedHotel.Index] = true;
            if (removedHotel != null) newIsHotelStayAfterActivity[removedHotel.Index] = false;

            // New checked info
            SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewChecked);
            SaDriverInfo newCheckedDriverInfo = TotalCostCalculator.GetDriverInfo(newDriverPath, newIsHotelStayAfterActivity, driver, info);
            SaDebugger.GetCurrentStageInfo().SetDriverInfo(newCheckedDriverInfo);

            // Check for errors
            SaDebugger.GetCurrentOperationPart().CheckDriverErrors();
        }
    }
}

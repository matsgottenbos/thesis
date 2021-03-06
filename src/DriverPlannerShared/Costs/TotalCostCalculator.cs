/*
 * Calculates total cost of an assignment
*/

namespace DriverPlannerShared {
    public static class TotalCostCalculator {
        /** Get assignment cost */
        public static void ProcessAssignmentCost(SaInfo info) {
            info.TotalInfo = new SaTotalInfo();
            info.DriverInfos = new SaDriverInfo[info.Instance.AllDrivers.Length];
            for (int driverIndex = 0; driverIndex < info.Instance.AllDrivers.Length; driverIndex++) {
                List<Activity> driverPath = info.DriverPaths[driverIndex];
                Driver driver = info.Instance.AllDrivers[driverIndex];
                SaDriverInfo driverInfo = GetDriverInfo(driverPath, info.IsHotelStayAfterActivity, driver, info);

                info.DriverInfos[driverIndex] = driverInfo;
                info.TotalInfo.AddDriverInfo(driverInfo);
            }
            info.TotalInfo.Stats.SatisfactionScore = SatisfactionScoreCalculator.GetSatisfactionScore(info);

            info.ExternalDriverTypeInfos = new SaExternalDriverTypeInfo[info.Instance.ExternalDriverTypes.Length];
            for (int externalDriverTypeIndex = 0; externalDriverTypeIndex < info.Instance.ExternalDriverTypes.Length; externalDriverTypeIndex++) {
                SaExternalDriverTypeInfo externalDriverTypeInfo = GetExternalDriverTypeInfo(info.DriverInfos, externalDriverTypeIndex, info);
                info.ExternalDriverTypeInfos[externalDriverTypeIndex] = externalDriverTypeInfo;
                info.TotalInfo.AddExternalDriverTypeInfo(externalDriverTypeInfo);
            }
        }

        public static SaDriverInfo GetDriverInfo(List<Activity> driverPath, bool[] isHotelStayAfterActivity, Driver driver, SaInfo info) {
            SaDriverInfo driverInfo = new SaDriverInfo(info.Instance);

            if (driverPath.Count > 0) {
                Func<Activity, bool> isHotelAfterActivity = (Activity activity) => isHotelStayAfterActivity[activity.Index];
                Activity shiftFirstActivity = driverPath[0];
                Activity parkingActivity = shiftFirstActivity;
                Activity prevActivity = shiftFirstActivity;
                Activity beforeHotelActivity = null;
                for (int pathActivityIndex = 1; pathActivityIndex < driverPath.Count; pathActivityIndex++) {
                    Activity searchActivity = driverPath[pathActivityIndex];
                    RangeCostActivityProcessor.ProcessDriverActivity(searchActivity, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivity, driverInfo, driver, info, info.Instance);
                }
                RangeCostActivityProcessor.ProcessDriverEndRange(null, ref shiftFirstActivity, ref parkingActivity, ref prevActivity, ref beforeHotelActivity, isHotelAfterActivity, driverInfo, driver, info, info.Instance);
            }

            RangeCostDiffCalculator.ProcessFullPathValues(driverInfo, driverInfo, driver, info);
            return driverInfo;
        }

        public static SaExternalDriverTypeInfo GetExternalDriverTypeInfo(SaDriverInfo[] driverInfos, int externalDriverTypeIndex, SaInfo info) {
            ExternalDriverType externalDriverTypes = info.Instance.ExternalDriverTypes[externalDriverTypeIndex];
            ExternalDriver[] externalDriversOfType = info.Instance.ExternalDriversByType[externalDriverTypeIndex];
            int shiftCount = 0;
            for (int externalDriverOfTypeIndex = 0; externalDriverOfTypeIndex < externalDriversOfType.Length; externalDriverOfTypeIndex++) {
                ExternalDriver externalDriver = externalDriversOfType[externalDriverOfTypeIndex];
                shiftCount += driverInfos[externalDriver.AllDriversIndex].ShiftCount;
            }

            SaExternalDriverTypeInfo externalDriverTypeInfo = new SaExternalDriverTypeInfo() {
                ExternalShiftCount = shiftCount,
            };
            externalDriverTypeInfo.AddPotentialShiftCountViolation(shiftCount, externalDriverTypes);

            return externalDriverTypeInfo;
        }
    }
}

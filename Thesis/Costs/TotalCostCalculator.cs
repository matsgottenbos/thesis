using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class TotalCostCalculator {
        /** Get assignment cost */
        public static void ProcessAssignmentCost(SaInfo info) {
            info.TotalInfo = new SaTotalInfo();
            info.DriverInfos = new SaDriverInfo[info.Instance.AllDrivers.Length];
            for (int driverIndex = 0; driverIndex < info.Instance.AllDrivers.Length; driverIndex++) {
                List<Trip> driverPath = info.DriverPaths[driverIndex];
                Driver driver = info.Instance.AllDrivers[driverIndex];
                SaDriverInfo driverInfo = GetDriverInfo(driverPath, info.IsHotelStayAfterTrip, driver, info);

                info.DriverInfos[driverIndex] = driverInfo;
                info.TotalInfo.AddDriverInfo(driverInfo);
            }
            info.TotalInfo.Stats.SatisfactionScore = SatisfactionCalculator.GetSatisfactionScore(info);

            info.ExternalDriverTypeInfos = new SaExternalDriverTypeInfo[info.Instance.ExternalDriverTypes.Length];
            for (int externalDriverTypeIndex = 0; externalDriverTypeIndex < info.Instance.ExternalDriverTypes.Length; externalDriverTypeIndex++) {
                SaExternalDriverTypeInfo externalDriverTypeInfo = GetExternalDriverTypeInfo(info.DriverInfos, externalDriverTypeIndex, info);
                info.ExternalDriverTypeInfos[externalDriverTypeIndex] = externalDriverTypeInfo;
                info.TotalInfo.AddExternalDriverTypeInfo(externalDriverTypeInfo);
            }
        }

        public static SaDriverInfo GetDriverInfo(List<Trip> driverPath, bool[] isHotelStayAfterTrip, Driver driver, SaInfo info) {
            SaDriverInfo driverInfo = new SaDriverInfo(info.Instance);

            if (driverPath.Count > 0) {
                Func<Trip, bool> isHotelAfterTrip = (Trip trip) => isHotelStayAfterTrip[trip.Index];
                Trip shiftFirstTrip = driverPath[0];
                Trip parkingTrip = shiftFirstTrip;
                Trip prevTrip = shiftFirstTrip;
                Trip beforeHotelTrip = null;
                for (int pathTripIndex = 1; pathTripIndex < driverPath.Count; pathTripIndex++) {
                    Trip searchTrip = driverPath[pathTripIndex];
                    RangeCostTripProcessor.ProcessDriverTrip(searchTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, info.Instance);
                }
                RangeCostTripProcessor.ProcessDriverEndRange(null, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, info.Instance);
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

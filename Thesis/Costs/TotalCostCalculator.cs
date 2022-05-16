using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class TotalCostCalculator {
        /** Get assignment cost */
        public static (DriverInfo, DriverInfo[]) GetAssignmentCost(SaInfo info) {
            DriverInfo assignmentInfo = new DriverInfo();
            DriverInfo[] driverInfos = new DriverInfo[info.Instance.AllDrivers.Length];
            for (int driverIndex = 0; driverIndex < info.Instance.AllDrivers.Length; driverIndex++) {
                List<Trip> driverPath = info.DriverPaths[driverIndex];
                Driver driver = info.Instance.AllDrivers[driverIndex];
                DriverInfo driverInfo = GetDriverPathCost(driverPath, info.IsHotelStayAfterTrip, driver, info);

                assignmentInfo += driverInfo;
                driverInfos[driverIndex] = driverInfo;
            }

            return (assignmentInfo, driverInfos);
        }

        public static DriverInfo GetDriverPathCost(List<Trip> driverPath, bool[] isHotelStayAfterTrip, Driver driver, SaInfo info) {
            DriverInfo driverInfo = new DriverInfo();
            if (driverPath.Count == 0) return driverInfo;

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
            RangeCostDiffCalculator.ProcessFullPathValues(driverInfo, driverInfo, driver, info);
            return driverInfo;
        }
        public static DriverInfo GetDriverPathCost(List<Trip> driverPath, Driver driver, SaInfo info) {
            return GetDriverPathCost(driverPath, info.IsHotelStayAfterTrip, driver, info);
        }
    }
}

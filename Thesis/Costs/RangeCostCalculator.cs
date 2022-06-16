using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class RangeCostCalculator {
        /** Get costs of part of a driver's path; penalty are computed with without worked time and shift count penalties */
        public static SaDriverInfo GetRangeCost(Trip rangeFirstTrip, Trip rangeLastTrip, Func<Trip, bool> isHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            SaDriverInfo driverInfo = new SaDriverInfo(info.Instance);
            if (driverPath.Count == 0) return driverInfo;

            int rangeFirstPathIndex = info.DriverPathIndices[rangeFirstTrip.Index];
            int rangeLastPathIndex = info.DriverPathIndices[rangeLastTrip.Index];
            Trip shiftFirstTrip = rangeFirstTrip, parkingTrip = rangeFirstTrip, prevTrip = rangeFirstTrip, beforeHotelTrip = null;
            for (int pathTripIndex = rangeFirstPathIndex + 1; pathTripIndex <= rangeLastPathIndex; pathTripIndex++) {
                Trip searchTrip = driverPath[pathTripIndex];
                RangeCostTripProcessor.ProcessDriverTrip(searchTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, info.Instance);
            }
            Trip tripAfterRange = rangeLastPathIndex + 1 < driverPath.Count ? driverPath[rangeLastPathIndex + 1] : null;
            RangeCostTripProcessor.ProcessDriverEndRange(tripAfterRange, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, info.Instance);
            return driverInfo;
        }

        /** Get costs of part of a driver's path where a trip is unassigned; penalty are computed with without worked time and shift count penalties */
        public static SaDriverInfo GetRangeCostWithUnassign(Trip rangeFirstTrip, Trip rangeLastTrip, Trip unassignedTrip, Func<Trip, bool> isHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            SaDriverInfo driverInfo = new SaDriverInfo(info.Instance);
            int rangeFirstPathIndex = info.DriverPathIndices[rangeFirstTrip.Index];
            int rangeLastPathIndex = info.DriverPathIndices[rangeLastTrip.Index];
            Trip shiftFirstTrip = null, parkingTrip = null, prevTrip = null, beforeHotelTrip = null;
            for (int pathTripIndex = rangeFirstPathIndex; pathTripIndex <= rangeLastPathIndex; pathTripIndex++) {
                Trip searchTrip = driverPath[pathTripIndex];
                if (searchTrip == unassignedTrip) continue;

                if (shiftFirstTrip == null) {
                    shiftFirstTrip = parkingTrip = prevTrip = searchTrip;
                    continue;
                }

                RangeCostTripProcessor.ProcessDriverTrip(searchTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, info.Instance);
            }
            Trip tripAfterRange = rangeLastPathIndex + 1 < driverPath.Count ? driverPath[rangeLastPathIndex + 1] : null;
            RangeCostTripProcessor.ProcessDriverEndRange(tripAfterRange, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, info.Instance);
            return driverInfo;
        }

        /** Get costs of part of a driver's path where a trip is assigned; penalty are computed with without worked time and shift count penalties */
        public static SaDriverInfo GetRangeCostWithAssign(Trip rangeFirstTrip, Trip rangeLastTrip, Trip assignedTrip, Func<Trip, bool> isHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            SaDriverInfo driverInfo = new SaDriverInfo(info.Instance);
            Trip shiftFirstTrip = null, parkingTrip = null, prevTrip = null, beforeHotelTrip = null;

            if (driverPath.Count == 0) {
                // New path only contains assigned trip
                shiftFirstTrip = parkingTrip = prevTrip = assignedTrip;
                RangeCostTripProcessor.ProcessDriverEndRange(null, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, info.Instance);
                return driverInfo;
            }

            int rangeFirstPathIndex = info.DriverPathIndices[rangeFirstTrip.Index];
            int rangeLastPathIndex = info.DriverPathIndices[rangeLastTrip.Index];

            // Process part before assigned trip
            int pathTripIndex;
            for (pathTripIndex = rangeFirstPathIndex; pathTripIndex <= rangeLastPathIndex; pathTripIndex++) {
                Trip searchTrip = driverPath[pathTripIndex];
                if (searchTrip.Index > assignedTrip.Index) break;

                if (shiftFirstTrip == null) {
                    shiftFirstTrip = parkingTrip = prevTrip = searchTrip;
                    continue;
                }

                RangeCostTripProcessor.ProcessDriverTrip(searchTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, info.Instance);
            }

            // Process assigned trip
            if (shiftFirstTrip == null) {
                shiftFirstTrip = parkingTrip = prevTrip = assignedTrip;
            } else {
                RangeCostTripProcessor.ProcessDriverTrip(assignedTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, info.Instance);
            }

            // Process part after assigned trip
            for (; pathTripIndex <= rangeLastPathIndex; pathTripIndex++) {
                Trip searchTrip = driverPath[pathTripIndex];
                RangeCostTripProcessor.ProcessDriverTrip(searchTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, info.Instance);
            }

            Trip tripAfterRange = rangeLastPathIndex + 1 < driverPath.Count ? driverPath[rangeLastPathIndex + 1] : null;
            RangeCostTripProcessor.ProcessDriverEndRange(tripAfterRange, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, info.Instance);
            return driverInfo;
        }

        /** Get costs of part of a driver's path where a trip is unassigned and another assigned; penalty are computed with without worked time and shift count penalties */
        public static SaDriverInfo GetRangeCostWithSwap(Trip rangeFirstTrip, Trip rangeLastTrip, Trip unassignedTrip, Trip assignedTrip, Func<Trip, bool> isHotelAfterTrip, Driver driver, List<Trip> driverPath, SaInfo info) {
            SaDriverInfo driverInfo = new SaDriverInfo(info.Instance);
            Trip shiftFirstTrip = null, parkingTrip = null, prevTrip = null, beforeHotelTrip = null;

            if (driverPath.Count == 0) {
                // New path only contains assigned trip
                RangeCostTripProcessor.ProcessDriverTrip(assignedTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, info.Instance);
                RangeCostTripProcessor.ProcessDriverEndRange(null, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, info.Instance);
                return driverInfo;
            }

            int rangeFirstPathIndex = info.DriverPathIndices[rangeFirstTrip.Index];
            int rangeLastPathIndex = info.DriverPathIndices[rangeLastTrip.Index];

            // Process part before assigned trip
            int pathTripIndex;
            for (pathTripIndex = rangeFirstPathIndex; pathTripIndex <= rangeLastPathIndex; pathTripIndex++) {
                Trip searchTrip = driverPath[pathTripIndex];
                if (searchTrip.Index > assignedTrip.Index) break;
                if (searchTrip == unassignedTrip) continue;

                if (shiftFirstTrip == null) {
                    shiftFirstTrip = parkingTrip = prevTrip = searchTrip;
                    continue;
                }

                RangeCostTripProcessor.ProcessDriverTrip(searchTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, info.Instance);
            }

            // Process assigned trip
            if (shiftFirstTrip == null) {
                shiftFirstTrip = parkingTrip = prevTrip = assignedTrip;
            } else {
                RangeCostTripProcessor.ProcessDriverTrip(assignedTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, info.Instance);
            }

            // Process part after assigned trip
            for (; pathTripIndex <= rangeLastPathIndex; pathTripIndex++) {
                Trip searchTrip = driverPath[pathTripIndex];
                if (searchTrip == unassignedTrip) continue;
                RangeCostTripProcessor.ProcessDriverTrip(searchTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, info.Instance);
            }

            Trip tripAfterRange = rangeLastPathIndex + 1 < driverPath.Count ? driverPath[rangeLastPathIndex + 1] : null;
            RangeCostTripProcessor.ProcessDriverEndRange(tripAfterRange, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, driverInfo, driver, info, info.Instance);
            return driverInfo;
        }
    }
}

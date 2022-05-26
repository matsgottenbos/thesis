using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class SatisfactionCalculator {
        public static double GetDriverSatisfaction(DriverInfo driverInfo, InternalDriver driver) {
            // Determine duplicate route count
            int duplicateRouteCount = 0;
            for (int sharedRouteIndex = 0; sharedRouteIndex < driverInfo.SharedRouteCounts.Length; sharedRouteIndex++) {
                int count = driverInfo.SharedRouteCounts[sharedRouteIndex];
                if (count > 1) {
                    duplicateRouteCount += count - 1;
                }
            }

            // Determine driver satisfaction
            double driverSatisfaction = 0;
            driverSatisfaction += Config.SatCriteriumHotels.GetSatisfaction(driverInfo.HotelCount, driver);
            driverSatisfaction += Config.SatCriteriumNightShifts.GetSatisfaction(driverInfo.NightShiftCount, driver);
            driverSatisfaction += Config.SatCriteriumWeekendShifts.GetSatisfaction(driverInfo.WeekendShiftCount, driver);
            driverSatisfaction += Config.SatCriteriumTravelTime.GetSatisfaction(driverInfo.TravelTime, driver);
            driverSatisfaction += Config.SatCriteriumDuplicateRoutes.GetSatisfaction(duplicateRouteCount, driver);
            driverSatisfaction += Config.SatCriteriumConsecutiveFreeDays.GetSatisfaction((driverInfo.SingleFreeDays, driverInfo.DoubleFreeDays), driver);
            return driverSatisfaction;
        }

        public static double GetSatisfactionScore(SaInfo info) {
            // Average satisfaction
            double averageDriverSatisfaction = info.TotalInfo.DriverSatisfaction / info.Instance.InternalDrivers.Length;

            // Minimum driver satisfaction
            double minDriverSatisfaction = double.MaxValue;
            for (int driverIndex = 0; driverIndex < info.Instance.InternalDrivers.Length; driverIndex++) {
                DriverInfo driverInfo = info.DriverInfos[driverIndex];
                if (driverInfo.DriverSatisfaction < minDriverSatisfaction) minDriverSatisfaction = driverInfo.DriverSatisfaction;
            }

            // Total satisfaction
            double totalSatisfactionDiff = (averageDriverSatisfaction + minDriverSatisfaction) / 2;
            return totalSatisfactionDiff;
        }

        public static double GetSatisfactionScoreDiff(DriverInfo totalInfoDiff, Driver driver1, DriverInfo driver1InfoDiff, Driver driver2, DriverInfo driver2InfoDiff, SaInfo info) {
            // Average satisfaction
            double averageDriverSatisfactionDiff = totalInfoDiff.DriverSatisfaction / info.Instance.InternalDrivers.Length;

            // Minimum driver satisfaction
            double oldMinDriverSatisfaction = double.MaxValue;
            double newMinDriverSatisfaction = double.MaxValue;
            for (int driverIndex = 0; driverIndex < info.Instance.InternalDrivers.Length; driverIndex++) {
                DriverInfo oldDriverInfo = info.DriverInfos[driverIndex];

                // Get new minimum
                double newDriverSatisfaction = oldDriverInfo.DriverSatisfaction;
                if (driverIndex == driver1.AllDriversIndex) newDriverSatisfaction += driver1InfoDiff.DriverSatisfaction;
                else if (driver2 != null && driverIndex == driver2.AllDriversIndex) newDriverSatisfaction += driver2InfoDiff.DriverSatisfaction;

                // Update minimums
                if (oldDriverInfo.DriverSatisfaction < oldMinDriverSatisfaction) oldMinDriverSatisfaction = oldDriverInfo.DriverSatisfaction;
                if (newDriverSatisfaction < newMinDriverSatisfaction) newMinDriverSatisfaction = newDriverSatisfaction;
            }
            double minDriverSatisfactionDiff = newMinDriverSatisfaction - oldMinDriverSatisfaction;

            // Total satisfaction
            double totalSatisfactionDiff = (averageDriverSatisfactionDiff + minDriverSatisfactionDiff) / 2;
            return totalSatisfactionDiff;
        }
    }
}

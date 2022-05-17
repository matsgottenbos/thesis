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
    }
}

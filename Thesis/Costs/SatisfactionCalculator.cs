using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class SatisfactionCalculator {
        public static double GetDriverSatisfaction(DriverInfo driverInfo, bool debugIsNew) {
            double satisfaction = 0;

            // Hotels
            satisfaction += Config.SatCriteriumHotels.GetSatisfaction(driverInfo.HotelCount);

            // Night shifts
            satisfaction += Config.SatCriteriumNightShifts.GetSatisfaction(driverInfo.NightShiftCount);

            // Weekend shifts
            satisfaction += Config.SatCriteriumWeekendShifts.GetSatisfaction(driverInfo.WeekendShiftCount);

            return satisfaction;
        }
    }
}

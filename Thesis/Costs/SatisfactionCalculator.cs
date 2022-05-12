using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class SatisfactionCalculator {
        public static double GetDriverSatisfaction(DriverInfo driverInfo) {
            double satisfaction = 0;
            satisfaction += Config.SatCriteriumHotels.GetSatisfaction(driverInfo.HotelCount);
            satisfaction += Config.SatCriteriumNightShifts.GetSatisfaction(driverInfo.NightShiftCount);
            satisfaction += Config.SatCriteriumWeekendShifts.GetSatisfaction(driverInfo.WeekendShiftCount);
            satisfaction += Config.SatCriteriumTravelTime.GetSatisfaction(driverInfo.TravelTime);
            return satisfaction;
        }
    }
}

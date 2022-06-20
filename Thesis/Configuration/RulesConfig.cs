using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class RulesConfig {
        // Shift constraints
        public const int DriverMaxShiftCount = 5; // The maximum number of shifts a driver can have per week
        public const int NormalShiftMaxLengthWithTravel = 12 * 60; // Maximum length of a shift, including travel
        public const int NormalShiftMaxLengthWithoutTravel = 10 * 60; // Maximum length of a shift, excluding travel
        public const int NightShiftMaxLengthWithTravel = 10 * 60; // Maximum length of a night shift, including travel
        public const int NightShiftMaxLengthWithoutTravel = 8 * 60; // Maximum length of a night shift, excluding travel
        public const int NormalShiftMinRestTime = 11 * 60; // Minimum required resting time after a non-night shift
        public const int NightShiftMinRestTime = 14 * 60; // Minimum required resting time after a night shift
        public const int HotelMaxRestTime = 24 * 60; // Maximum allowed resting time during a hotel stay
        public const int HotelExtraTravelTime = 30; // Additional travel time between two shifts when there is an hotel stay; this travel time is equally split between the shift before and after
        public const int SingleFreeDayMinRestTime = 24 * 60; // Minimum required resting time between two shifts to count as a single free day
        public const int DoubleFreeDayMinRestTime = 48 * 60; // Minimum required resting time between two shifts to count as two consecutive free days

        // Night/weekend shifts
        public static readonly Func<int, int, bool> IsNightShiftByLawFunc = (int drivingTimeAtNight, int drivingTime) => drivingTimeAtNight >= 60; // Function determining whether a shift is a night shift, according to labour laws
        public static readonly Func<int, int, bool> IsNightShiftByCompanyRulesFunc = (int drivingTimeAtNight, int drivingTime) => (float)drivingTimeAtNight / drivingTime >= 0.5; // Function determining whether a shift is a night shift, according to company rules
        public static readonly Func<int, int, bool> IsWeekendShiftByCompanyRulesFunc = (int drivingTimeInWeekend, int drivingTime) => (float)drivingTimeInWeekend / drivingTime >= 0.5; // Function determining whether a shift is a weekend shift, according to company rules
        // TODO: configure holidays

        // Weekend/non-weekend parts of the week
        public static readonly TimePart[] WeekPartsForWeekend = new TimePart[] {
            new TimePart(0, true), // Still weekend at Monday 0:00 (start of timeframe)
            new TimePart(6 * 60, false), // Weekend ends Monday 6:00
            new TimePart(4 * MiscConfig.DayLength + 18 * 60, true), // Weekend starts Friday 18:00
            new TimePart(7 * MiscConfig.DayLength + 6 * 60, false), // Weekend ends Monday 6:00
        };

        // Day/night parts of the day
        public static readonly TimePart[] DayPartsForNight = new TimePart[] {
            new TimePart(0, true), // Still night at 0:00 (start of day)
            new TimePart(6 * 60, false), // Night ends 6:00
            new TimePart(23 * 60, true), // Night starts 23:00
        };

        // Satisfaction
        public static readonly RangeSatisfactionCriterium SatCriteriumHotels = new RangeSatisfactionCriterium(4, 0, 0.2f);
        public static readonly RangeSatisfactionCriterium SatCriteriumNightShifts = new RangeSatisfactionCriterium(5, 0, 0.1f);
        public static readonly RangeSatisfactionCriterium SatCriteriumWeekendShifts = new RangeSatisfactionCriterium(2, 0, 0.1f);
        public static readonly RangeSatisfactionCriterium SatCriteriumTravelTime = new RangeSatisfactionCriterium(30 * 60, 0, 0.1f);
        public static readonly RangeSatisfactionCriterium SatCriteriumDuplicateRoutes = new RangeSatisfactionCriterium(10, 0, 0.2f);
        public static readonly ConsecutiveFreeDaysCriterium SatCriteriumConsecutiveFreeDays = new ConsecutiveFreeDaysCriterium(0.1f);
        public static readonly TargetSatisfactionCriterium SatCriteriumContractTime = new TargetSatisfactionCriterium(driver => driver.ContractTime, driver => 0.3f * driver.ContractTime, 0.2f);

        // Robustness
        public const float RobustnessCostFactorSameDuty = 0f; // Added cost for each expected conflict due to delays, if the conflict is between trips of the same duty
        public const float RobustnessCostFactorSameProject = 1000f; // Added cost for each expected conflict due to delays, if the conflict is between trips of different duties but of the same project
        public const float RobustnessCostFactorDifferentProject = 2000f; // Added cost for each expected conflict due to delays, if the conflict is between trips of different duties and projects
        public const float TripDelayProbability = 0.275f; // Chance that a trip has a delay
        public static readonly Func<int, double> TripMeanDelayFunc = (int plannedDuration) => plannedDuration * plannedDuration / 5561 + 0.123 * plannedDuration + 37.38; // Trip mean delay by planned duration: p^2/5571 + 0.123p + 37.38
        public static readonly Func<double, double> TripDelayGammaDistributionAlphaFunc = (double meanDelay) => meanDelay * meanDelay / 3879; // Alpha parameter of trip delay gamma distribution, by mean delay: p^2/3879
        public static readonly Func<double, double> TripDelayGammaDistributionBetaFunc = (double meanDelay) => meanDelay / 3879; // Beta parameter of trip delay gamma distribution, by mean delay: p/3879
        public static readonly Func<int, int> TravelDelayExpectedFunc = (int plannedTravelTime) => plannedTravelTime / 10 + 15; // Expected travel delay of 10% + 15 minutes
    }
}

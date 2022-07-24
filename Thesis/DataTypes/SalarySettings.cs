using System;

namespace Thesis {
    abstract class SalarySettings {
        public int DriverTypeIndex;
        public readonly SalaryRateBlock[] WeekdaySalaryRates;
        public SalaryRateBlock[] ProcessedSalaryRates;
        public readonly float WeekendSalaryRate;
        public readonly int MinPaidShiftTime; // The minimum amount of worked time that is paid per shift, for an internal driver

        public SalarySettings(SalaryRateBlock[] weekdaySalaryRates, float weekendSalaryRate, int minPaidShiftTime) {
            WeekdaySalaryRates = weekdaySalaryRates;
            WeekendSalaryRate = weekendSalaryRate;
            MinPaidShiftTime = minPaidShiftTime;
            DriverTypeIndex = -1;
        }


        /* Should be set during processing */

        public void Init(int salaryInfoIndex, SalaryRateBlock[] processedSalaryRates) {
            DriverTypeIndex = salaryInfoIndex;
            ProcessedSalaryRates = processedSalaryRates;
        }
    }

    class InternalSalarySettings : SalarySettings {
        public readonly float TravelTimeSalaryRate; // Travel compensation per minute
        public readonly int UnpaidTravelTimePerShift; // For each shift, only travel time longer than this is paid

        public InternalSalarySettings(SalaryRateBlock[] weekdaySalaryRates, float weekendSalaryRate, float travelTimeSalaryRate, int minPaidShiftTime, int unpaidTravelTimePerShift) : base(weekdaySalaryRates, weekendSalaryRate, minPaidShiftTime) {
            TravelTimeSalaryRate = travelTimeSalaryRate;
            UnpaidTravelTimePerShift = unpaidTravelTimePerShift;
        }

        public static InternalSalarySettings CreateByHours(SalaryRateBlock[] weekdaySalaryRates, float weekendHourlySalaryRate, float travelTimeHourlySalaryRate, float minPaidShiftTimeHours, float unpaidTravelTimePerShiftHours) {
            return new InternalSalarySettings(
                weekdaySalaryRates,
                weekendHourlySalaryRate / DevConfig.HourLength,
                travelTimeHourlySalaryRate / DevConfig.HourLength,
                (int)Math.Round(minPaidShiftTimeHours * DevConfig.HourLength),
                (int)Math.Round(unpaidTravelTimePerShiftHours * DevConfig.HourLength)
            );
        }
    }

    class ExternalSalarySettings : SalarySettings {
        public readonly float TravelDistanceSalaryRate; // Travel compensation per kilometer
        public readonly int UnpaidTravelDistancePerShift; // For each shift, only travel distance longer than this is paid

        public ExternalSalarySettings(SalaryRateBlock[] weekdaySalaryRates, float weekendSalaryRate, float travelDistanceSalaryRate, int minPaidShiftTime, int unpaidTravelDistancePerShift) : base(weekdaySalaryRates, weekendSalaryRate, minPaidShiftTime) {
            TravelDistanceSalaryRate = travelDistanceSalaryRate;
            UnpaidTravelDistancePerShift = unpaidTravelDistancePerShift;
        }

        public static ExternalSalarySettings CreateByHours(SalaryRateBlock[] weekdaySalaryRates, float weekendHourlySalaryRate, float travelDistanceSalaryRate, float minPaidShiftTimeHours, int unpaidTravelDistancePerShift) {
            return new ExternalSalarySettings(
                weekdaySalaryRates,
                weekendHourlySalaryRate / DevConfig.HourLength,
                travelDistanceSalaryRate,
                (int)Math.Round(minPaidShiftTimeHours * DevConfig.HourLength),
                unpaidTravelDistancePerShift
            );
        }
    }
}

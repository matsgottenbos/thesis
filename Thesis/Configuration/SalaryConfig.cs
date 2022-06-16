using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class SalaryConfig {
        // Hotels (TODO: move into SalarySettings?)
        public const float HotelCosts = 130f;

        // Salary rates for driver types
        public static readonly SalarySettings InternalNationalSalaryInfo = SalarySettings.CreateByHours(
            new SalaryRateBlock[] { // Weekday salary rates
                SalaryRateBlock.CreateByHours(0, 55, true), // Night 0-4, continuing hourly rate of 55
                SalaryRateBlock.CreateByHours(4, 55), // Night 4-6, hourly rate of 55
                SalaryRateBlock.CreateByHours(6, 50), // Morning 6-8, hourly rate of 50
                SalaryRateBlock.CreateByHours(8, 45), // Day 8-19, hourly rate of 45
                SalaryRateBlock.CreateByHours(19, 50), // Evening 19-23, hourly rate of 50
                SalaryRateBlock.CreateByHours(23, 55, true), // Night 23-0, continuing hourly rate of 55
            },
            55, // Weekend rate
            45, // Travel rate
            6, // Minimum paid shift time (hours)
            1 // Unpaid travel time per shift (hours)
        );
        public static readonly SalarySettings InternalInternationalSalaryInfo = SalarySettings.CreateByHours(
            new SalaryRateBlock[] { // Weekday salary rates
                SalaryRateBlock.CreateByHours(0, 55, true), // Night 0-4, continuing hourly rate of 55
                SalaryRateBlock.CreateByHours(4, 55), // Night 4-6, hourly rate of 55
                SalaryRateBlock.CreateByHours(6, 50), // Morning 6-8, hourly rate of 50
                SalaryRateBlock.CreateByHours(8, 45), // Day 8-19, hourly rate of 45
                SalaryRateBlock.CreateByHours(19, 50), // Evening 19-23, hourly rate of 50
                SalaryRateBlock.CreateByHours(23, 55, true), // Night 23-0, continuing hourly rate of 55
            },
            60, // Weekend rate
            50, // Travel rate
            6, // Minimum paid shift time (hours)
            1 // Unpaid travel time per shift (hours)
        );
        public static readonly SalarySettings ExternalNationalSalaryInfo = SalarySettings.CreateByHours(
            new SalaryRateBlock[] {
                SalaryRateBlock.CreateByHours(0, 75), // Night 0-6, hourly rate of 75
                SalaryRateBlock.CreateByHours(6, 70), // Morning 6-7, hourly rate of 70
                SalaryRateBlock.CreateByHours(7, 65), // Day 7-18, hourly rate of 65
                SalaryRateBlock.CreateByHours(18, 70), // Evening 18-23, hourly rate of 70
                SalaryRateBlock.CreateByHours(23, 75), // Night 23-0, hourly rate of 75
            },
            75, // Weekend rate
            30, // Travel rate
            8, // Minimum paid shift time (hours)
            1 // Unpaid travel time per shift (hours)
        );
        public static readonly SalarySettings ExternalInternationalSalaryInfo = SalarySettings.CreateByHours(
            new SalaryRateBlock[] {
                SalaryRateBlock.CreateByHours(0, 80), // Night 0-6, hourly rate of 80
                SalaryRateBlock.CreateByHours(6, 75), // Morning 6-7, hourly rate of 75
                SalaryRateBlock.CreateByHours(7, 70), // Day 7-18, hourly rate of 70
                SalaryRateBlock.CreateByHours(18, 75), // Evening 18-23, hourly rate of 75
                SalaryRateBlock.CreateByHours(23, 80), // Night 23-0, hourly rate of 80
            },
            80, // Weekend rate
            30, // Travel rate
            8, // Minimum paid shift time (hours)
            1 // Unpaid travel time per shift (hours)
        );
    }

    class SalarySettings {
        public int DriverTypeIndex;
        public readonly SalaryRateBlock[] WeekdaySalaryRates;
        public readonly float WeekendSalaryRate, TravelSalaryRate;
        public readonly int MinPaidShiftTime; // The minimum amount of worked time that is paid per shift, for an internal driver
        public readonly int UnpaidTravelTimePerShift; // For each shift, only travel time longer than this is paid

        public SalarySettings(SalaryRateBlock[] weekdaySalaryRates, float weekendSalaryRate, float travelSalaryRate, int minPaidShiftTime, int unpaidTravelTimePerShift) {
            WeekdaySalaryRates = weekdaySalaryRates;
            WeekendSalaryRate = weekendSalaryRate;
            TravelSalaryRate = travelSalaryRate;
            MinPaidShiftTime = minPaidShiftTime;
            UnpaidTravelTimePerShift = unpaidTravelTimePerShift;
            DriverTypeIndex = -1;
        }

        // Index should be set during processing
        public void SetDriverTypeIndex(int salaryInfoIndex) {
            DriverTypeIndex = salaryInfoIndex;
        }

        public static SalarySettings CreateByHours(SalaryRateBlock[] weekdaySalaryRates, float weekendHourlySalaryRate, float travelHourlySalaryRate, float minPaidShiftTimeHours, float unpaidTravelTimePerShiftHours) {
            return new SalarySettings(
                weekdaySalaryRates,
                weekendHourlySalaryRate / MiscConfig.HourLength,
                travelHourlySalaryRate / MiscConfig.HourLength,
                (int)Math.Round(minPaidShiftTimeHours * MiscConfig.HourLength),
                (int)Math.Round(unpaidTravelTimePerShiftHours * MiscConfig.HourLength)
            );
        }
    }

    class ExternalDriverTypeSettings {
        public readonly string CompanyName;
        public readonly bool IsInternational;
        public readonly int MinShiftCount, MaxShiftCount;

        public ExternalDriverTypeSettings(string companyName, bool isInternational, int minShiftCount, int maxShiftCount) {
            CompanyName = companyName;
            IsInternational = isInternational;
            MinShiftCount = minShiftCount;
            MaxShiftCount = maxShiftCount;
        }
    }

    class TimePart {
        public readonly int StartTime;
        public readonly bool IsSelected;

        public TimePart(int startTime, bool isWeekend) {
            StartTime = startTime;
            IsSelected = isWeekend;
        }
    }

    class SalaryRateBlock {
        public readonly int StartTime;
        public readonly float SalaryRate;
        public readonly float ContinuingRate; // A shift starting in this rate block will use the continuing rate as a minimum for the entire shift

        public SalaryRateBlock(int startTime, float salaryRate, float continuingRate) {
            StartTime = startTime;
            SalaryRate = salaryRate;
            ContinuingRate = continuingRate;
        }

        public static SalaryRateBlock CreateByHours(float startTimeHours, float hourlySalaryRate, bool isContinuingRate = false) {
            return new SalaryRateBlock(
                (int)Math.Round(startTimeHours * MiscConfig.HourLength),
                hourlySalaryRate / MiscConfig.HourLength,
                isContinuingRate ? hourlySalaryRate / MiscConfig.HourLength : 0f
            );
        }
    }
}

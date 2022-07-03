using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class SalaryConfig {
        // Misc costs (TODO: move into SalarySettings?)
        public const float HotelCosts = 180f;
        public const float SharedCarCostsPerKilometer = 0.35f; // Additional costs for travel by pool car: includes intra-shift car travel, travel to pick up personal car, and travel to/from hotel

        // Salary rates for driver types
        public static readonly InternalSalarySettings InternalNationalSalaryInfo = InternalSalarySettings.CreateByHours(
            new SalaryRateBlock[] { // Weekday salary rates
                SalaryRateBlock.CreateByHours(0, 55, true), // Night 0-4, continuing hourly rate of 55
                SalaryRateBlock.CreateByHours(4, 55), // Night 4-6, hourly rate of 55
                SalaryRateBlock.CreateByHours(6, 50), // Morning 6-8, hourly rate of 50
                SalaryRateBlock.CreateByHours(8, 45), // Day 8-19, hourly rate of 45
                SalaryRateBlock.CreateByHours(19, 50), // Evening 19-23, hourly rate of 50
                SalaryRateBlock.CreateByHours(23, 55, true), // Night 23-0, continuing hourly rate of 55
            },
            55, // Weekend rate (per hour)
            45, // Travel time rate (per hour)
            6, // Minimum paid shift time (hours)
            1 // Unpaid travel time per shift (hours)
        );
        public static readonly InternalSalarySettings InternalInternationalSalaryInfo = InternalSalarySettings.CreateByHours(
            new SalaryRateBlock[] { // Weekday salary rates
                SalaryRateBlock.CreateByHours(0, 55, true), // Night 0-4, continuing hourly rate of 55
                SalaryRateBlock.CreateByHours(4, 55), // Night 4-6, hourly rate of 55
                SalaryRateBlock.CreateByHours(6, 50), // Morning 6-8, hourly rate of 50
                SalaryRateBlock.CreateByHours(8, 45), // Day 8-19, hourly rate of 45
                SalaryRateBlock.CreateByHours(19, 50), // Evening 19-23, hourly rate of 50
                SalaryRateBlock.CreateByHours(23, 55, true), // Night 23-0, continuing hourly rate of 55
            },
            60, // Weekend rate (per hour)
            50, // Travel time rate (per hour)
            6, // Minimum paid shift time (hours)
            1 // Unpaid travel time per shift (hours)
        );
        public static readonly ExternalSalarySettings ExternalNationalSalaryInfo = ExternalSalarySettings.CreateByHours(
            new SalaryRateBlock[] {
                SalaryRateBlock.CreateByHours(0, 75), // Night 0-6, hourly rate of 75
                SalaryRateBlock.CreateByHours(6, 70), // Morning 6-7, hourly rate of 70
                SalaryRateBlock.CreateByHours(7, 65), // Day 7-18, hourly rate of 65
                SalaryRateBlock.CreateByHours(18, 70), // Evening 18-23, hourly rate of 70
                SalaryRateBlock.CreateByHours(23, 75), // Night 23-0, hourly rate of 75
            },
            75, // Weekend rate (per hour)
            0.3f, // Travel distance rate (per kilometer)
            8, // Minimum paid shift time (hours)
            100 // Unpaid travel time per shift (hours)
        );
        public static readonly ExternalSalarySettings ExternalInternationalSalaryInfo = ExternalSalarySettings.CreateByHours(
            new SalaryRateBlock[] {
                SalaryRateBlock.CreateByHours(0, 80), // Night 0-6, hourly rate of 80
                SalaryRateBlock.CreateByHours(6, 75), // Morning 6-7, hourly rate of 75
                SalaryRateBlock.CreateByHours(7, 70), // Day 7-18, hourly rate of 70
                SalaryRateBlock.CreateByHours(18, 75), // Evening 18-23, hourly rate of 75
                SalaryRateBlock.CreateByHours(23, 80), // Night 23-0, hourly rate of 80
            },
            80, // Weekend rate (per hour)
            0.3f, // Travel distance rate (per kilometer)
            8, // Minimum paid shift time (hours)
            100 // Unpaid travel distance per shift (kilometers)
        );
    }
}

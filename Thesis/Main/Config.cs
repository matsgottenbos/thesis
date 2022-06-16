using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    enum DataSource {
        Generator,
        Excel,
        Odata,
    }

    static class Config {
        // App
        public const DataSource SelectedDataSource = DataSource.Excel;
        //public const DataSource SelectedDataSource = DataSource.Odata;

        // Shift constraints
        public const int DriverMaxShiftCount = 5; // The maximum number of shifts a driver can have per week
        public const int NormalShiftMaxLengthWithTravel = 12 * 60; // Maximum length of a shift, including travel
        public const int NormalShiftMaxLengthWithoutTravel = 10 * 60; // Maximum length of a shift, excluding travel
        public const int NightShiftMaxLengthWithTravel = 10 * 60; // Maximum length of a night shift, including travel
        public const int NightShiftMaxLengthWithoutTravel = 8 * 60; // Maximum length of a night shift, excluding travel
        public const int NormalShiftMinRestTime = 11 * 60; // Minimum required resting time after a non-night shift
        public const int NightShiftMinRestTime = 14 * 60; // Minimum required resting time after a night shift
        public const int SingleFreeDayMinRestTime = 24 * 60; // Minimum required resting time between two shifts to count as a single free day
        public const int DoubleFreeDayMinRestTime = 48 * 60; // Minimum required resting time between two shifts to count as two consecutive free days
        public const int ShiftWaitingTimeThreshold = 6 * 60; // Waiting times shorter than this count as the same trip; waiting time longer start a new shift

        // Time periods
        public const int HourLength = 24 * 60;
        public const int DayLength = 24 * HourLength;

        // Night/weekend shifts
        public static readonly Func<int, int, bool> IsNightShiftByLawFunc = (int drivingTimeAtNight, int drivingTime) => drivingTimeAtNight >= 60; // Function determining whether a shift is a night shift, according to labour laws
        public static readonly Func<int, int, bool> IsNightShiftByCompanyRulesFunc = (int drivingTimeAtNight, int drivingTime) => (float)drivingTimeAtNight / drivingTime >= 0.5; // Function determining whether a shift is a night shift, according to company rules
        public static readonly Func<int, int, bool> IsWeekendShiftByCompanyRulesFunc = (int drivingTimeInWeekend, int drivingTime) => (float)drivingTimeInWeekend / drivingTime >= 0.5; // Function determining whether a shift is a weekend shift, according to company rules
        // TODO: configure holidays

        // Weekend/non-weekend parts of the week
        public static readonly TimePart[] WeekPartsForWeekend = new TimePart[] {
            new TimePart(0, true), // Still weekend at Monday 0:00 (start of timeframe)
            new TimePart(6 * 60, false), // Weekend ends Monday 6:00
            new TimePart(4 * DayLength + 18 * 60, true), // Weekend starts Friday 18:00
            new TimePart(7 * DayLength + 6 * 60, false), // Weekend ends Monday 6:00
        };

        // Day/night parts of the day
        public static readonly TimePart[] DayPartsForNight = new TimePart[] {
            new TimePart(0, true), // Still night at 0:00 (start of day)
            new TimePart(6 * 60, false), // Night ends 6:00
            new TimePart(23 * 60, true), // Night starts 23:00
        };

        // Driver salaries
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

        public static readonly ExternalDriverTypeSettings[] ExternalDriverTypes = new ExternalDriverTypeSettings[] {
            new ExternalDriverTypeSettings("Machinext national", false, 8, 15),
            new ExternalDriverTypeSettings("Machinext international", true, 3, 4),
            new ExternalDriverTypeSettings("Aeterno national", false, 7, 12),
            new ExternalDriverTypeSettings("Aeterno international", true, 4, 7),
            new ExternalDriverTypeSettings("Spoorlutions national", false, 10, 18),
            new ExternalDriverTypeSettings("Logisticle national", false, 4, 6),
            new ExternalDriverTypeSettings("Logisticle international", true, 4, 6),
            // TODO: add Railflex and MOB?
        };

        // Hotels
        public const float HotelCosts = 130f;
        public const int HotelExtraTravelTime = 30;
        public const int HotelMaxRestTime = 24 * 60;

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
        public const float RobustnessCostFactorSameProject = 500f; // Added cost for each expected conflict due to delays, if the conflict is between trips of different duties but of the same project
        public const float RobustnessCostFactorDifferentProject = 1000f; // Added cost for each expected conflict due to delays, if the conflict is between trips of different duties and projects
        public const float TripDelayProbability = 0.275f; // Chance that a trip has a delay
        public static readonly Func<int, double> TripMeanDelayFunc = (int plannedDuration) => plannedDuration * plannedDuration / 5561 + 0.123 * plannedDuration + 37.38; // Trip mean delay by planned duration: p^2/5571 + 0.123p + 37.38
        public static readonly Func<double, double> TripDelayGammaDistributionAlphaFunc = (double meanDelay) => meanDelay * meanDelay / 3879; // Alpha parameter of trip delay gamma distribution, by mean delay: p^2/3879
        public static readonly Func<double, double> TripDelayGammaDistributionBetaFunc = (double meanDelay) => meanDelay / 3879; // Beta parameter of trip delay gamma distribution, by mean delay: p/3879
        public static readonly Func<int, int> TravelDelayExpectedFunc = (int plannedTravelTime) => plannedTravelTime / 10 + 15; // Expected travel delay of 10% + 15 minutes


        /* Excel importer */
        public static readonly string[] ExcelIncludedRailwayUndertakings = new string[] { "Rail Force One" };
        public static readonly string[] ExcelIncludedActivityDescriptions = new string[] { // Activity descriptions in English
            "8-uurs controle",
            "Aankomst controle",
            "Abschlussdienst",
            "Abstellung",
            "Daily Check locomotive",
            "Drive train",
            "Exchange staff",
            "Locomotive Exchange",
            "Locomotive movement",
            "Parking",
            "Shunting",
            "Terminal Process",
            "Vertrekcontrole (VKC)",
            "Vorbereitungsdienst",
            "Wagon technical inspection"
        };
        public static readonly string[] ExcelIncludedJobTitlesNational = new string[] { "Machinist VB nationaal", "Rangeerder" }; // TODO: rangeerders wel of niet meenemen?
        public static readonly string[] ExcelIncludedJobTitlesInternational = new string[] { "Machinist VB Internationaal NL-D" };
        public static readonly DateTime ExcelPlanningStartDate = new DateTime(2022, 5, 23);
        public static readonly DateTime ExcelPlanningNextDate = ExcelPlanningStartDate.AddDays(7);
        public const int ExcelInternalDriverContractTime = 40 * 60;
        public const int ExcelExternalDriverTypeCount = 5;
        public const int ExcelExternalDriverMinCountPerType = 5;
        public const int ExcelExternalDriverMaxCountPerType = 20;


        /* Generator */
        public const int GenMinStationTravelTime = 30;
        public const int GenMaxStationTravelTime = 3 * 60;
        public const float GenMinCarTravelTimeFactor = 0.5f;
        public const float GenMaxCarTravelTimeFactor = 0.8f;
        public const int GenMaxHomeTravelTime = 2 * 60;
        public const float GenTrackProficiencyProb = 0.7f;


        /* Simulated annealing */
        // SA parameters
        public const int SaIterationCount = 500000000;
        public const int SaCheckCostFrequency = 100000;
        public const int SaLogFrequency = 1000000;
        public const int SaParameterUpdateFrequency = 100000;
        public const float SaInitialTemperature = 5000f;
        public const float SaCycleMinInitialTemperature = 500f;
        public const float SaCycleMaxInitialTemperature = 7000f;
        public const float SaTemperatureReductionFactor = 0.99f;
        public const float SaEndCycleTemperature = 300f;
        public const float SaCycleMinSatisfactionFactor = 0f;
        public const float SaCycleMaxSatisfactionFactor = 0.5f;
        public const float ParetoFrontMinCostDiff = 500f; // Minmum cost different to consider two solutions to be separate points on the pareto front

        // Operation probabilities
        public const float AssignInternalProbCumulative = 0.3f;
        public const float AssignExternalProbCumulative = 0.6f;
        public const float SwapProbCumulative = 0.9999f;
        public const float ToggleHotelProbCumulative = 1f;

        // Penalties
        public const double PrecendenceViolationPenalty = 20000;
        public const double ShiftLengthViolationPenalty = 5000;
        public const double ShiftLengthViolationPenaltyPerMin = 5000 / 60f;
        public const double RestTimeViolationPenalty = 5000;
        public const double RestTimeViolationPenaltyPerMin = 5000 / 60f;
        public const double InternalShiftCountViolationPenaltyPerShift = 20000;
        public const double InvalidHotelPenalty = 20000;
        public const double ExternalShiftCountPenaltyPerShift = 20000;


        /* File structure */
        public static readonly string ProjectFolder = (Environment.Is64BitProcess ? Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName : Directory.GetParent(Environment.CurrentDirectory).Parent.FullName) + @"\"; // Path to the project root folder
        public static readonly string SolutionFolder = ProjectFolder + @"\..\"; // Path to the solution root folder
        public static readonly string DataFolder = Path.Combine(SolutionFolder, @"data\");
        public static readonly string OutputFolder = Path.Combine(SolutionFolder, @"output\");


        /* Misc */
        // Floating point imprecision
        public const float FloatingPointMargin = 0.0001f;
        public const int PercentageFactor = 100;

        // Debug
        public const bool DebugUseSeededSa = true;
        public const bool DebugCheckAndLogOperations = false;
        public const bool DebugSaLogCurrentSolution = true;
        public const bool DebugSaLogAdditionalInfo = true;
        public const bool DebugRunInspector = false;
        public const bool DebugRunJsonExporter = false;
        public const bool DebugRunDelaysExporter = false;
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
                weekendHourlySalaryRate / Config.HourLength,
                travelHourlySalaryRate / Config.HourLength,
                (int)Math.Round(minPaidShiftTimeHours * Config.HourLength),
                (int)Math.Round(unpaidTravelTimePerShiftHours * Config.HourLength)
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
                (int)Math.Round(startTimeHours * Config.HourLength),
                hourlySalaryRate / Config.HourLength,
                isContinuingRate ? hourlySalaryRate / Config.HourLength : 0f
            );
        }
    }
}

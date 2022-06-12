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
        //public const DataSource SelectedDataSource = DataSource.Generator;
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
        public const int DayLength = 24 * 60;

        // Night/weekend shifts
        public static readonly Func<int, int, bool> IsNightShiftByLawFunc = (int drivingTimeAtNight, int drivingTime) => drivingTimeAtNight >= 60; // Function determining whether a shift is a night shift, according to labour laws
        public static readonly Func<int, int, bool> IsNightShiftByCompanyRulesFunc = (int drivingTimeAtNight, int drivingTime) => (float)drivingTimeAtNight / drivingTime >= 0.5; // Function determining whether a shift is a night shift, according to company rules
        public static readonly Func<int, int, bool> IsWeekendShiftByCompanyRulesFunc = (int drivingTimeInWeekend, int drivingTime) => (float)drivingTimeInWeekend / drivingTime >= 0.5; // Function determining whether a shift is a weekend shift, according to company rules
        // TODO: configure holidays

        // Salary types during the week (weekend/non-weekend)
        public static readonly SalaryTypeInfo[] WeekSalaryTypes = new SalaryTypeInfo[] {
            new SalaryTypeInfo(0, true), // Still weekend at Monday 0:00 (start of timeframe)
            new SalaryTypeInfo(6 * 60, false), // Weekend ends Monday 6:00
            new SalaryTypeInfo(4 * DayLength + 18 * 60, true), // Weekend starts Friday 18:00
            new SalaryTypeInfo(7 * DayLength + 6 * 60, false), // Weekend ends Monday 6:00
        };

        // Internal driver salaries
        public static readonly SalaryRateInfo[] InternalDriverWeekdaySalaryRates = new SalaryRateInfo[] {
            new SalaryRateInfo(0 * 60,  60 / 60f, true, true), // Night 0-4: continuing hourly rate of 60
            new SalaryRateInfo(4 * 60,  60 / 60f, false, true), // Night 4-6: hourly rate of 60
            new SalaryRateInfo(6 * 60,  55 / 60f, false, false), // Morning 6-8, hourly rate of 55
            new SalaryRateInfo(8 * 60,  50 / 60f, false, false), // Day 8-19, hourly rate of 50
            new SalaryRateInfo(19 * 60, 55 / 60f, false, false), // Evening 19-23, hourly rate of 55
            new SalaryRateInfo(23 * 60, 60 / 60f, true, true), // Night 23-0, continuing hourly rate of 60
        };
        public const float InternalDriverWeekendSalaryRate = 60 / 60f;
        public const float InternalDriverTravelSalaryRate = 50 / 60f;
        public const int InternalDriverMinPaidShiftTime = 6 * 60; // The minimum amount of worked time that is paid per shift, for an internal driver
        public const int InternalDriverUnpaidTravelTimePerShift = 60;

        // External driver salaries
        public static readonly SalaryRateInfo[] ExternalDriverWeekdaySalaryRates = new SalaryRateInfo[] {
            new SalaryRateInfo(0 * 60,  80 / 60f, false), // Night 0-6: hourly rate of 80
            new SalaryRateInfo(6 * 60,  75 / 60f, false), // Morning 6-7, hourly rate of 75
            new SalaryRateInfo(7 * 60,  70 / 60f, false), // Day 7-18, hourly rate of 70
            new SalaryRateInfo(18 * 60, 75 / 60f, false), // Evening 18-23, hourly rate of 75
            new SalaryRateInfo(23 * 60, 80 / 60f, false), // Night 23-0, hourly rate of 80
        };
        public const float ExternalDriverWeekendSalaryRate = 80 / 60f;
        public const float ExternalDriverTravelSalaryRate = 70 / 60f;
        public const int ExternalDriverMinPaidShiftTime = 8 * 60; // The minimum amount of worked time that is paid per shift, for an external driver

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
        public const float RobustnessCostFactorSameProject = 1000f; // Added cost for each expected conflict due to delays, if the conflict is between trips of different duties but of the same project
        public const float RobustnessCostFactorDifferentProject = 2000f; // Added cost for each expected conflict due to delays, if the conflict is between trips of different duties and projects
        public const float TripDelayProbability = 0.275f; // Chance that a trip has a delay
        public static readonly Func<int, double> TripMeanDelayFunc = (int plannedDuration) => plannedDuration * plannedDuration / 5561 + 0.123 * plannedDuration + 37.38; // Trip mean delay by planned duration: p^2/5571 + 0.123p + 37.38
        public static readonly Func<double, double> TripDelayGammaDistributionAlphaFunc = (double meanDelay) => meanDelay * meanDelay / 3879; // Alpha parameter of trip delay gamma distribution, by mean delay: p^2/3879
        public static readonly Func<double, double> TripDelayGammaDistributionBetaFunc = (double meanDelay) => meanDelay / 3879; // Beta parameter of trip delay gamma distribution, by mean delay: p/3879
        public static readonly Func<int, int> TravelDelayExpectedFunc = (int plannedTravelTime) => plannedTravelTime / 10 + 15; // Expected travel delay of 10% + 15 minutes


        /* Excel importer */
        public static readonly string[] ExcelIncludedRailwayUndertakings = new string[] { "Rail Force One" };
        public static readonly DateTime ExcelPlanningStartDate = new DateTime(2022, 5, 23);
        public static readonly DateTime ExcelPlanningNextDate = ExcelPlanningStartDate.AddDays(7);
        public const int ExcelInternalDriverContractTime = 40 * 60;
        public const int ExcelExternalDriverTypeCount = 5;
        public const int ExcelExternalDriverMinCountPerType = 5;
        public const int ExcelExternalDriverMaxCountPerType = 20;


        /* Generator */
        // Counts
        public const int GenTimeframeLength = 2 * 24 * 60;
        public const int GenStationCount = 10;
        public const int GenTripCount = 30;
        public const int GenInternalDriverCount = 10;
        public const int GenExternaDriverTypeCount = 2;
        public const int GenExternalDriverMinCountPerType = 2;
        public const int GenExternalDriverMaxCountPerType = 5;

        // Travel times
        public const int GenMinStationTravelTime = 30;
        public const int GenMaxStationTravelTime = 4 * 60;
        public const float GenMinCarTravelTimeFactor = 0.5f;
        public const float GenMaxCarTravelTimeFactor = 0.8f;
        public const int GenMaxHomeTravelTime = 2 * 60;

        // Contract times
        public const int GenInternalDriverContractTime = GenTimeframeLength / 4;

        // Generator probabilities
        public const float GenTrackProficiencyProb = 0.9f;


        /* Simulated annealing */
        // SA parameters (generated)
        //public const int SaIterationCount = 10000000;
        //public const int SaCheckCostFrequency = 100000;
        //public const int SaLogFrequency = 1000000;
        //public const int SaParameterUpdateFrequency = 100000;
        //public const float SaInitialTemperature = 500f;
        //public const float SaCycleInitialTemperatureMin = 50f;
        //public const float SaCycleInitialTemperatureMax = 700f;
        //public const float SaTemperatureReductionFactor = 0.9f;
        //public const float SaEndCycleTemperature = 30f;

        // SA parameters (Excel)
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
        public const float AssignInternalProbCumulative = 0.5f;
        public const float AssignExternalProbCumulative = 0.6f;
        public const float SwapProbCumulative = 0.9999f;
        public const float ToggleHotelProbCumulative = 1f;

        // Penalties
        public const double PrecendenceViolationPenalty = 20000;
        public const double ShiftLengthViolationPenalty = 5000;
        public const double ShiftLengthViolationPenaltyPerMin = 5000 / 60f;
        public const double RestTimeViolationPenalty = 5000;
        public const double RestTimeViolationPenaltyPerMin = 5000 / 60f;
        public const double ShiftCountViolationPenaltyPerShift = 20000;
        public const double InvalidHotelPenalty = 20000;


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
        public const bool DebugSaLogCurrentSolution = false;
        public const bool DebugSaLogAdditionalInfo = false;
        public const bool DebugRunInspector = false;
        public const bool DebugRunJsonExporter = false;
        public const bool DebugRunDelaysExporter = false;
    }

    class SalaryTypeInfo {
        public readonly int StartTime;
        public readonly bool IsWeekend;

        public SalaryTypeInfo(int startTime, bool isWeekend) {
            StartTime = startTime;
            IsWeekend = isWeekend;
        }
    }

    class SalaryRateInfo {
        public readonly int StartTime;
        public readonly float SalaryRate;
        public readonly float ContinuingRate; // A shift starting in this rate block will use the continuing rate as a minimum for the entire shift
        public readonly bool? IsNight, IsWeekend; // IsNight should be filled in for internal salaries, IsWeekend is added automatically during processing

        public SalaryRateInfo(int startTime, float salaryRate, bool isContinuingRate, bool? isNight = null, bool? isWeekend = null) : this(startTime, salaryRate, isContinuingRate ? salaryRate : 0, isNight, isWeekend) { }

        public SalaryRateInfo(int startTime, float salaryRate, float continuingRate, bool? isNight, bool? isWeekend) {
            StartTime = startTime;
            SalaryRate = salaryRate;
            ContinuingRate = continuingRate;
            IsNight = isNight;
            IsWeekend = isWeekend;
        }
    }
}

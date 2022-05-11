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

        // Shifts
        public const int MaxShiftLengthWithTravel = 12 * 60; // Maximum length of a shift, including travel
        public const int MaxShiftLengthWithoutTravel = 10 * 60; // Maximum length of a shift, excluding travel
        public const int MinRestTime = 11 * 60; // Minimum required resting time between two shifts
        public const int ShiftWaitingTimeThreshold = 6 * 60; // Waiting times shorter than this count as the same trip; waiting time longer start a new shift
        public const int BetweenShiftsMaxStartTimeDiff = 36 * 60; // The maximum difference in start times considered when checking rest time between different shifts
        public const int DriverMaxShiftCount = 5; // The maximum number of shifts a driver can have per week

        // Time periods
        public const int DayLength = 24 * 60;
        public const int NightStartTimeInDay = 23 * 60;
        public const int NightEndTimeInDay = 6 * 60;
        public const int NightShiftNightTimeThreshold = 60; // Minimum amount of time during night for a shift to be considered a night shift
        public const int WeekendStartTime = 5 * DayLength;
        public const int WeekendEndTime = 7 * DayLength;
        // TODO: configure holidays

        // Internal driver salaries
        public static readonly SalaryRateInfo[] InternalDriverWeekdaySalaryRates = new SalaryRateInfo[] {
            new SalaryRateInfo(0 * 60,  60 / 60f), // Night 0-6: hourly rate of 60
            new SalaryRateInfo(6 * 60,  55 / 60f), // Morning 6-7, hourly rate of 55
            new SalaryRateInfo(7 * 60,  50 / 60f), // Day 7-18, hourly rate of 50
            new SalaryRateInfo(18 * 60, 55 / 60f), // Evening 18-23, hourly rate of 55
            new SalaryRateInfo(23 * 60, 60 / 60f), // Night 23-6, hourly rate of 60
        };
        public const float InternalDriverWeekendSalaryRate = 60 / 60f;
        public const float InternalDriverTravelSalaryRate = 50 / 60f;
        public const int InternalDriverMinPaidShiftTime = 6 * 60; // The minimum amount of worked time that is paid per shift, for an internal driver
        public const int InternalDriverUnpaidTravelTimePerShift = 60;

        // External driver salaries
        public static readonly SalaryRateInfo[] ExternalDriverWeekdaySalaryRates = new SalaryRateInfo[] {
            new SalaryRateInfo(0 * 60,  80 / 60f), // Night 0-6: hourly rate of 80
            new SalaryRateInfo(6 * 60,  75 / 60f), // Morning 6-7, hourly rate of 75
            new SalaryRateInfo(7 * 60,  70 / 60f), // Day 7-18, hourly rate of 70
            new SalaryRateInfo(18 * 60, 75 / 60f), // Evening 18-23, hourly rate of 75
            new SalaryRateInfo(23 * 60, 80 / 60f), // Night 23-6, hourly rate of 80
        };
        public const float ExternalDriverWeekendSalaryRate = 80 / 60f;
        public const float ExternalDriverTravelSalaryRate = 70 / 60f;
        public const int ExternalDriverMinPaidShiftTime = 8 * 60; // The minimum amount of worked time that is paid per shift, for an external driver

        // Hotels
        public const float HotelCosts = 130f;
        public const int HotelExtraTravelTime = 30;
        public const int HotelMaxRestTime = 24 * 60;

        // Contract time deviations
        public const float MinContractTimeFraction = 0.8f;
        public const float MaxContractTimeFraction = 1.2f;

        // Satisfaction
        public static readonly IntSatisfactionCriterium SatCriteriumHotels = new IntSatisfactionCriterium(4, 0, 0.5f);
        public static readonly FloatSatisfactionCriterium SatCriteriumNightShifts = new FloatSatisfactionCriterium(5f, 0f, 0.25f);
        public static readonly FloatSatisfactionCriterium SatCriteriumWeekendShifts = new FloatSatisfactionCriterium(2f, 0f, 0.25f);


        /* Excel importer */
        public static readonly DateTime ExcelPlanningStartDate = new DateTime(2022, 1, 8);
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
        public const float SaCycleMaxSatisfactionFactor = 1f;

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
        public const double ContractTimeViolationPenalty = 5000;
        public const double ContractTimeViolationPenaltyPerMin = 5000 / 60f;
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
        public const bool DebugCheckAndLogOperations = false;
        public const bool DebugSaLogCurrentSolution = false;
        public const bool DebugSaLogAdditionalInfo = false;
        public const bool DebugRunInspector = false;
        public const bool DebugRunJsonExporter = false;
        public const bool DebugRunOdataTest = false;
        public const bool DebugUseSeededSa = true;
    }

    class SalaryRateInfo {
        public readonly int StartTime;
        public readonly float SalaryRate;

        public SalaryRateInfo(int startTime, float salaryRate) {
            StartTime = startTime;
            SalaryRate = salaryRate;
        }
    }
}

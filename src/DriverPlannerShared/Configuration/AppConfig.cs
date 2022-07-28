using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;

namespace DriverPlannerShared {
    public static class AppConfig {
        /* App settings */
        /// <summary>Starting date of the time period to plan for.</summary>
        public static DateTime PlanningStartDate { get; private set; }
        /// <summary>Ending date of the time period to plan for.</summary>
        public static DateTime PlanningEndDate { get; private set; }
        /// <summary>Number of iterations to run the simulated annealing algorithm for.</summary>
        public static long SaIterationCount { get; private set; }
        /// <summary>Filter activities on these values of RailwayUntertaking.</summary>
        public static string[] IncludedRailwayUndertakings { get; private set; }
        /// <summary>Filter activities on these values of ActivityDescriptionEN.</summary>
        public static string[] IncludedActivityTypes { get; private set; }
        /// <summary>Driver assignments in past data are considered internal for these company names.</summary>
        public static string[] InternalDriverCompanyNames { get; private set; }
        /// <summary>API key to use for requests to the Google Maps API.</summary>
        public static string OdataUsername { get; private set; }
        /// <summary>Password used to connect with the OData API of RailCube.</summary>
        public static string OdataPassword { get; private set; }
        /// <summary>API key to use for requests to the Google Maps API.</summary>
        public static string GoogleMapsApiKey { get; private set; }
        /// <summary>Maximum number of destinations allowed by the Google Maps API in a single request.</summary>
        public static int GoogleMapsMaxDestinationCountPerRequest { get; private set; }
        /// <summary>Some shift info in the program is rounded to this number of hours.</summary>
        public static int RoundedTimeStepSize { get; private set; }
        /// <summary>Number of processor threads to use for the simulated annealing algorithm. For the best performance, this number should be equal to the number of virtual cores in your computer.</summary>
        public static int ThreadCount { get; private set; }
        /// <summary>Local URL to run the UI HTTP server on.</summary>
        public static string UiHostUrl { get; private set; }

        public static void Init(XSSFWorkbook settingsBook) {
            ExcelSheet appSettingsSheet = new ExcelSheet("App", settingsBook);
            Dictionary<string, ICell> appSettingsCellDict = ConfigHandler.GetSettingsValueCellsAsDict(appSettingsSheet);

            PlanningStartDate = appSettingsSheet.GetDateValue(appSettingsCellDict["Planning window start date"]).Value;
            int planningWindowNumberOfDays = appSettingsSheet.GetIntValue(appSettingsCellDict["Planning window number of days"]).Value;
            PlanningEndDate = PlanningStartDate.AddDays(planningWindowNumberOfDays);
            SaIterationCount = ParseHelper.ParseLargeNumString(appSettingsSheet.GetStringValue(appSettingsCellDict["Algorithm iteration count"]));
            IncludedRailwayUndertakings = ParseHelper.SplitAndCleanDataStringList(appSettingsSheet.GetStringValue(appSettingsCellDict["Included railway undertakings"]));
            IncludedActivityTypes = ParseHelper.SplitAndCleanDataStringList(appSettingsSheet.GetStringValue(appSettingsCellDict["Included activity descriptions"]));
            InternalDriverCompanyNames = ParseHelper.SplitAndCleanDataStringList(appSettingsSheet.GetStringValue(appSettingsCellDict["Internal driver company names"]));
            OdataUsername = appSettingsSheet.GetStringValue(appSettingsCellDict["RailCube username"]);
            OdataPassword = appSettingsSheet.GetStringValue(appSettingsCellDict["RailCube password"]);
            GoogleMapsApiKey = appSettingsSheet.GetStringValue(appSettingsCellDict["Google Maps API key"]);
            GoogleMapsMaxDestinationCountPerRequest = appSettingsSheet.GetIntValue(appSettingsCellDict["Google Maps request max destinations"]).Value;
            RoundedTimeStepSize = ConfigHandler.HourToMinuteValue(appSettingsSheet.GetFloatValue(appSettingsCellDict["Rounded time step size"]).Value);
            ThreadCount = appSettingsSheet.GetIntValue(appSettingsCellDict["Number of threads"]).Value;
            UiHostUrl = appSettingsSheet.GetStringValue(appSettingsCellDict["UI host URL"]);
        }
    }
}

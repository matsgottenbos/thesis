using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class AppConfig {
        /// <summary>Planning window start date.</summary>
        public static DateTime PlanningStartDate;
        /// <summary>Planning window end date.</summary>
        public static DateTime PlanningNextDate;
        /// <summary>Number of iterations to run the simulated annealing algorithm for.</summary>
        public static long SaIterationCount;
        /// <summary>Filter activities on these values of RailwayUntertaking.</summary>
        public static string[] IncludedRailwayUndertakings;
        /// <summary>Filter activities on these values of ActivityDescriptionEN.</summary>
        public static string[] IncludedActivityTypes;
        /// <summary>Driver assignments in past data are considered internal for these company names.</summary>
        public static string[] InternalDriverCompanyNames;
        /// <summary>API key to use for requests to the Google Maps API.</summary>
        public static string GoogleMapsApiKey;
        /// <summary>Maximum number of destinations allowed by the Google Maps API in a single request.</summary>
        public static int GoogleMapsMaxDestinationCountPerRequest;
        /// <summary>Number of processor threads to use for the simulated annealing algorithm.</summary>
        public static int ThreadCount;
        /// <summary>Local URL to run the UI HTTP server on.</summary>
        public static string UiHostUrl;

        public static void Init(XSSFWorkbook settingsBook) {
            ExcelSheet appSettingsSheet = new ExcelSheet("App", settingsBook);
            Dictionary<string, ICell> appSettingsCellDict = ConfigHandler.GetSettingsValueCellsAsDict(appSettingsSheet);

            PlanningStartDate = ExcelSheet.GetDateValue(appSettingsCellDict["Planning window start date"]).Value;
            int planningWindowNumberOfDays = ExcelSheet.GetIntValue(appSettingsCellDict["Planning window number of days"]).Value;
            PlanningNextDate = PlanningStartDate.AddDays(planningWindowNumberOfDays);
            SaIterationCount = ParseHelper.ParseLargeNumString(ExcelSheet.GetStringValue(appSettingsCellDict["Algorithm iteration count"]));
            IncludedRailwayUndertakings = ParseHelper.SplitAndCleanDataStringList(ExcelSheet.GetStringValue(appSettingsCellDict["Included railway undertakings"]));
            IncludedActivityTypes = ParseHelper.SplitAndCleanDataStringList(ExcelSheet.GetStringValue(appSettingsCellDict["Included activity descriptions"]));
            InternalDriverCompanyNames = ParseHelper.SplitAndCleanDataStringList(ExcelSheet.GetStringValue(appSettingsCellDict["Internal driver company names"]));
            GoogleMapsApiKey = ExcelSheet.GetStringValue(appSettingsCellDict["Google Maps API key"]);
            GoogleMapsMaxDestinationCountPerRequest = ExcelSheet.GetIntValue(appSettingsCellDict["Google Maps request max destinations"]).Value;
            ThreadCount = ExcelSheet.GetIntValue(appSettingsCellDict["Number of threads"]).Value;
            UiHostUrl = ExcelSheet.GetStringValue(appSettingsCellDict["UI host URL"]);
        }
    }
}

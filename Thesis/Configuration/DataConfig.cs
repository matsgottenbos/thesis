using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class DataConfig {
        public static readonly DateTime ExcelPlanningStartDate = new DateTime(2022, 6, 27);
        public static readonly DateTime ExcelPlanningNextDate = ExcelPlanningStartDate.AddDays(7);


        /* Excel importer */
        public static readonly string[] ExcelIncludedRailwayUndertakings = new string[] { "Rail Force One" };
        public static readonly string[] ExcelInternalDriverCompanyNames = new string[] { "Rail Force One" };
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


        /* Generator */
        public const int GenMinStationTravelTime = 30;
        public const int GenMaxStationTravelTime = 3 * 60;
        public const float GenMinCarTravelTimeFactor = 0.5f;
        public const float GenMaxCarTravelTimeFactor = 0.8f;
        public const int GenMaxHomeTravelTime = 2 * 60;
        public const float GenTrackProficiencyProb = 1f;


        /* Google Maps */
        public const string GoogleMapsApiKey = "AIzaSyAnnCoTq3j55VQeQsTjxryHh4VYHyinoaA";
        public const int GoogleMapsMaxDestinationCountPerRequest = 25;
    }
}

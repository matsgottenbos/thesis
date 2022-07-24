using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    enum DataSource {
        Excel,
        Odata,
    }

    class DevConfig {
        // Data source
        public const DataSource SelectedDataSource = DataSource.Excel;
        //public const DataSource SelectedDataSource = DataSource.Odata;

        // Multithreading
        public const bool EnableMultithreading = true;

        // OData credentials
        public const string OdataUsername = "opsrsh01@rig";
        public const string OdataPassword = "Bu@maN2099a";

        // Time periods
        public const int HourLength = 60;
        public const int DayLength = 24 * HourLength;

        // Technical
        public const float FloatingPointMargin = 0.00001f;
        public const int PercentageFactor = 100;
        public const int RoundedTimeStepSize = 15;

        // File structure
        public static readonly string ProjectFolder = (Environment.Is64BitProcess ? Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName : Directory.GetParent(Environment.CurrentDirectory).Parent.FullName) + @"\"; // Path to the project root folder
        public static readonly string SolutionFolder = ProjectFolder + @"\..\"; // Path to the solution root folder
        public static readonly string InputFolder = Path.Combine(SolutionFolder, @"input\");
        public static readonly string IntermediateFolder = Path.Combine(SolutionFolder, @"intermediate\");
        public static readonly string OutputFolder = Path.Combine(SolutionFolder, @"output\");
        public static readonly string UiFolder = Path.Combine(SolutionFolder, @"ui\");

        // App modes
        public const bool DebugRunTravelTimeProcesssor = false;
        public const bool DebugRunUi = false;

        // Debug
        public const bool DebugUseSeededSa = false;
        public const bool DebugCheckOperations = false;
        public const bool DebugSaLogThreads = true;
        public const bool DebugSaLogCurrentSolution = false;
        public const bool DebugSaLogAdditionalInfo = false;
        public const bool DebugLogDataRepairs = false;
        public const bool DebugRunInspector = false;
        public const bool DebugRunJsonExporter = false;
        public const bool DebugRunDelaysExporter = false;
        public const bool DebugRunPastDataExporter = false;
    }
}

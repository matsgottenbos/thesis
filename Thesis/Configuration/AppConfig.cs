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

    static class AppConfig {
        // Data source
        public const DataSource SelectedDataSource = DataSource.Excel;
        //public const DataSource SelectedDataSource = DataSource.Odata;

        // Multithreading
        public const bool EnableMultithreading = true;
        public const int ThreadCount = 8;

        // Debug
        public const bool DebugUseSeededSa = true;
        public const bool DebugCheckOperations = false;
        public const bool DebugSaLogThreads = false;
        public const bool DebugSaLogCurrentSolution = false;
        public const bool DebugSaLogAdditionalInfo = false;
        public const bool DebugSaLogOperationStats = false;
        public const bool DebugLogDataRepairs = false;
        public const bool DebugRunInspector = false;
        public const bool DebugRunJsonExporter = false;
        public const bool DebugRunDelaysExporter = false;
        public const bool DebugRunTravelTimeProcesssor = false;
        public const bool DebugRunPastDataExporter = false;
        public const int DebugRunSaCount = 1;

        // File structure
        public static readonly string ProjectFolder = (Environment.Is64BitProcess ? Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName : Directory.GetParent(Environment.CurrentDirectory).Parent.FullName) + @"\"; // Path to the project root folder
        public static readonly string SolutionFolder = ProjectFolder + @"\..\"; // Path to the solution root folder
        public static readonly string InputFolder = Path.Combine(SolutionFolder, @"input\");
        public static readonly string IntermediateFolder = Path.Combine(SolutionFolder, @"intermediate\");
        public static readonly string OutputFolder = Path.Combine(SolutionFolder, @"output\");
    }
}

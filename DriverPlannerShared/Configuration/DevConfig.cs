namespace DriverPlannerShared {
    public enum DataSource {
        Excel,
        Odata,
    }

    public class DevConfig {
        /* Data source */
        // TODO: remove setting
        public const DataSource SelectedDataSource = DataSource.Odata;

        /* Time periods */
        /// <summary>Number of minutes in an hour.</summary>
        public const int HourLength = 60;
        /// <summary>Number of minutes in a day.</summary>
        public const int DayLength = 24 * HourLength;

        /* Technical */
        /// <summary>Amount of slack used when comparing two float values.</summary>
        public const float FloatingPointMargin = 0.00001f;
        /// <summary>Factor a fraction is multiplied with to get a percentage.</summary>
        public const int PercentageFactor = 100;

        /* File structure */
        /// <summary>Path to the solution root folder.</summary>
        public static readonly string SolutionFolder = (Environment.Is64BitProcess ? Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName : Directory.GetParent(Environment.CurrentDirectory).Parent.FullName) + @"\..\";
        /// <summary>Path to the input folder.</summary>
        public static readonly string InputFolder = Path.Combine(SolutionFolder, @"input\");
        /// <summary>Path to the intermediate folder.</summary>
        public static readonly string IntermediateFolder = Path.Combine(SolutionFolder, @"intermediate\");
        /// <summary>Path to the output folder.</summary>
        public static readonly string OutputFolder = Path.Combine(SolutionFolder, @"output\");
        /// <summary>Path to the UI folder.</summary>
        public static readonly string UiFolder = Path.Combine(SolutionFolder, @"ui\");

        /* Debugging settings */
        /// <summary>Whether to disable multithreading in the algorithm.</summary>
        public const bool DebugDisableMultithreading = false;
        /// <summary>Whether to throw exceptions instead of handling them and logging them to console.</summary>
        public const bool DebugThrowExceptions = true;
        /// <summary>Whether to use a fixed seed for the random objects of the application.</summary>
        public const bool DebugSeedRandomness = false;
        /// <summary>Whether to perform additional checks on the correctness of operation cost differences.</summary>
        public const bool DebugCheckOperations = false;
        /// <summary>Whether to log the progress of each individual thread.</summary>
        public const bool DebugLogThreads = false;
        /// <summary>Whether to include the assignment of the current solution in the progress logs.</summary>
        public const bool DebugLogCurrentSolutionAssignment = false;
        /// <summary>Whether to include additional info about the current solution the progress logs.</summary>
        public const bool DebugLogAdditionalInfo = false;
        /// <summary>Whether to log additional info about the repairs performed to the imported data.</summary>
        public const bool DebugLogDataRepairs = false;

        /* Debugging app modes */
        /// <summary>Whether to run the debug inspector app mode.</summary>
        public const bool DebugRunInspector = false;
        /// <summary>Whether to run the debug JSON exporter app mode.</summary>
        public const bool DebugRunJsonExporter = false;
        /// <summary>Whether to run the debug delays exporter app mode.</summary>
        public const bool DebugRunDelaysExporter = false;
        /// <summary>Whether to run the debug past data exporter app mode.</summary>
        public const bool DebugRunPastDataExporter = false;
    }
}

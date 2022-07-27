using DriverPlannerShared;
using System;
using System.Collections.Generic;

namespace DriverPlannerAlgorithm {
    class Program {
        static void Main() {
            ConfigHandler.InitAllConfigs();

            if (DevConfig.DebugThrowExceptions) {
                Run();
            } else {
                try {
                    Run();
                } catch (Exception exception) {
                    Console.WriteLine("\n*** Program exited with error ***\n{0}", exception.Message);
                }
            }

            Console.WriteLine("\nPress enter to exit");

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            for (int i = 0; i < 20; i++) GC.Collect();

            Console.ReadLine();
        }

        static void Run() {
            Console.WriteLine("Running program with start date {0} and end date {1} for {2} iterations", AppConfig.PlanningStartDate, AppConfig.PlanningEndDate, ToStringHelper.LargeNumToString(AppConfig.SaIterationCount));

            // Special app modes without instance data
            if (DevConfig.DebugRunDelaysExporter) {
                Console.WriteLine("\nRunning debug delays exporter");
                DebugDelaysExporter.Run();
                return;
            }

            Console.WriteLine("\nProccessing travel info...");
            TravelInfoExporter.DetermineAndExportAllTravelInfos();
            Console.WriteLine("Successfully processsed travel info");

            XorShiftRandom appRand = DevConfig.DebugSeedRandomness ? new XorShiftRandom(1) : new XorShiftRandom();
            Instance instance = GetInstance();

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            for (int i = 0; i < 5; i++) GC.Collect();

            // Special app modes with instance data
            if (DevConfig.DebugRunInspector) {
                Console.WriteLine("\nRunning debug inspector");
                new DebugInspector(instance);
                return;
            }
            if (DevConfig.DebugRunJsonExporter) {
                Console.WriteLine("\nRunning debug JSON exporter");
                new DebugJsonExporter(instance);
                return;
            }
            if (DevConfig.DebugRunPastDataExporter) {
                Console.WriteLine("\nRunning debug past data exporter");
                new DebugPastDataExporter(instance);
                return;
            }

            // Simulated annealing
            Console.WriteLine("\nStarting simulated annealing");
            AlgorithmMultithreadHandler saMultithreadHandler = new AlgorithmMultithreadHandler();
            saMultithreadHandler.Run(instance, appRand);

            Console.WriteLine("\n*** Program finished ***");
        }

        static Instance GetInstance() {
            Instance instance;
            switch (DevConfig.SelectedDataSource) {
                case DataSource.Excel:
                    Console.WriteLine("\nImporting data from Excel...");
                    instance = ExcelDataImporter.Import();
                    break;

                case DataSource.Odata:
                    Console.WriteLine("\nImporting data from RailCube...");
                    instance = OdataImporter.Import();
                    break;
            }
            Console.WriteLine("Data import complete");
            return instance;
        }
    }
}

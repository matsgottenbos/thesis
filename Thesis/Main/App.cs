using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class App {
        public App() {
            ConfigHandler.InitAllConfigs();

            if (DevConfig.DebugThrowExceptions) {
                Run(AppConfig.SaIterationCount, AppConfig.PlanningStartDate, AppConfig.PlanningNextDate);
                Console.WriteLine("\n*** Program finished ***");
            } else {
                try {
                    Run(AppConfig.SaIterationCount, AppConfig.PlanningStartDate, AppConfig.PlanningNextDate);
                    Console.WriteLine("\n*** Program finished ***");
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

        static void Run(long targetIterationCount, DateTime planningStartTime, DateTime planningEndTime) {
            // TODO: remove these arguments
            Console.WriteLine("Running program with start date {0} and end date {1} for {2} iterations", planningStartTime, planningEndTime, ParseHelper.LargeNumToString(targetIterationCount));

            // Special app modes without instance data
            if (DevConfig.DebugRunDelaysExporter) {
                Console.WriteLine("\nRunning debug delays exporter");
                DebugDelaysExporter.Run();
                return;
            }
            if (DevConfig.DebugRunUi) {
                Console.WriteLine("\nRunning UI");
                DebugUiHandler.Run();
                return;
            }

            Console.WriteLine("\nProccessing travel info...");
            TravelInfoExporter.DetermineAndExportAllTravelInfos();
            Console.WriteLine("Successfully processsed travel info");

            XorShiftRandom appRand = DevConfig.DebugUseSeededSa ? new XorShiftRandom(1) : new XorShiftRandom();
            Instance instance = GetInstance(planningStartTime, planningEndTime);

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
            SaMultithreadHandler saMultithreadHandler = new SaMultithreadHandler(targetIterationCount);
            saMultithreadHandler.Run(instance, appRand);
        }

        static Instance GetInstance(DateTime planningStartTime, DateTime planningEndTime) {
            Instance instance;
            switch (DevConfig.SelectedDataSource) {
                case DataSource.Excel:
                    Console.WriteLine("\nImporting data from Excel...");
                    instance = ExcelDataImporter.Import(planningStartTime, planningEndTime);
                    break;

                case DataSource.Odata:
                    Console.WriteLine("\nImporting data from RailCube...");
                    OdataImporter.Import();
                    throw new NotImplementedException();
                    break;
            }
            Console.WriteLine("Data import complete");
            return instance;
        }
    }
}

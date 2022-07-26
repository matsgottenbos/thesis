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

            Run(AppConfig.SaIterationCount, AppConfig.PlanningStartDate, AppConfig.PlanningNextDate);

            Console.WriteLine("\n*** Program finished ***");

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            for (int i = 0; i < 20; i++) GC.Collect();

            Console.ReadLine();
        }

        static void Run(long targetIterationCount, DateTime planningStartTime, DateTime planningEndTime) {
            Console.WriteLine("\nRunning program with start date {0} and end date {1} for {2} iterations", planningStartTime, planningEndTime, ParseHelper.LargeNumToString(targetIterationCount));

            // Special app modes without instance data
            if (DevConfig.DebugRunDelaysExporter) {
                Console.WriteLine("Running debug delays exporter");
                DebugDelaysExporter.Run();
                return;
            }
            if (DevConfig.DebugRunTravelTimeProcesssor) {
                Console.WriteLine("Running debug travel time exporter");
                TravelInfoExporter.DetermineAndExportAllTravelInfos();
                return;
            }
            if (DevConfig.DebugRunUi) {
                Console.WriteLine("Running UI");
                DebugUiHandler.Run();
                return;
            }

            XorShiftRandom appRand = DevConfig.DebugUseSeededSa ? new XorShiftRandom(1) : new XorShiftRandom();
            Instance instance = GetInstance(planningStartTime, planningEndTime);

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            for (int i = 0; i < 5; i++) GC.Collect();

            // Special app modes with instance data
            if (DevConfig.DebugRunInspector) {
                Console.WriteLine("Running debug inspector");
                new DebugInspector(instance);
                return;
            }
            if (DevConfig.DebugRunJsonExporter) {
                Console.WriteLine("Running debug JSON exporter");
                new DebugJsonExporter(instance);
                return;
            }
            if (DevConfig.DebugRunPastDataExporter) {
                Console.WriteLine("Running debug past data exporter");
                new DebugPastDataExporter(instance);
                return;
            }

            // Simulated annealing
            SaMultithreadHandler saMultithreadHandler = new SaMultithreadHandler(targetIterationCount);
            saMultithreadHandler.Run(instance, appRand);
        }

        static Instance GetInstance(DateTime planningStartTime, DateTime planningEndTime) {
            Instance instance;
            switch (DevConfig.SelectedDataSource) {
                case DataSource.Excel:
                    Console.WriteLine("Importing data from Excel...");
                    instance = ExcelDataImporter.Import(planningStartTime, planningEndTime);
                    break;

                case DataSource.Odata:
                    Console.WriteLine("Importing data from RailCube...");
                    OdataImporter.Import();
                    throw new NotImplementedException();
                    break;
            }
            Console.WriteLine("Data import complete");
            return instance;
        }
    }
}

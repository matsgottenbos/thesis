using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class App {
        public App() {
            Run(DataConfig.ExcelPlanningStartDate, DataConfig.ExcelPlanningNextDate);

            Console.WriteLine("\n*** Program finished ***");
            Console.ReadLine();
        }

        void Run(DateTime planningStartTime, DateTime planningEndTime) {
            Console.WriteLine("\nRunning program with start date {0} and end date {1}", planningStartTime, planningEndTime);

            // Special app modes without data
            if (AppConfig.DebugRunDelaysExporter) {
                Console.WriteLine("Running debug delays exporter");
                DebugDelaysExporter.Run();
                return;
            }
            if (AppConfig.DebugRunTravelTimeProcesssor) {
                Console.WriteLine("Running debug travel time exporter");
                TravelInfoExporter.DetermineAndExportAllTravelInfos();
                return;
            }

            XorShiftRandom appRand = AppConfig.DebugUseSeededSa ? new XorShiftRandom(1) : new XorShiftRandom();
            Instance instance = GetInstance(planningStartTime, planningEndTime);

            // Special app modes with data
            if (AppConfig.DebugRunInspector) {
                Console.WriteLine("Running debug inspector");
                new DebugInspector(instance);
                return;
            }
            if (AppConfig.DebugRunJsonExporter) {
                Console.WriteLine("Running debug JSON exporter");
                new DebugJsonExporter(instance);
                return;
            }
            if (AppConfig.DebugRunPastDataExporter) {
                Console.WriteLine("Running debug past data exporter");
                new DebugPastDataExporter(instance);
                return;
            }

            // Simulated annealing
            for (int i = 0; i < AppConfig.DebugRunSaCount; i++) {
                SaMultithreadHandler saMultithreadHandler = new SaMultithreadHandler();
                saMultithreadHandler.Run(instance, appRand);
            }
        }

        static Instance GetInstance(DateTime planningStartTime, DateTime planningEndTime) {
            Instance instance;
            switch (AppConfig.SelectedDataSource) {
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

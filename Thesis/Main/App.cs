using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class App {
        public App() {
            Run(SaConfig.SaIterationCount, DataConfig.ExcelPlanningStartDate, DataConfig.ExcelPlanningNextDate);

            // Instances
            DateTime instance1StartDate = new DateTime(2022, 6, 27);
            DateTime instance2StartDate = new DateTime(2022, 6, 20);
            DateTime instance3StartDate = new DateTime(2022, 6, 13);

            //// Instance 1 1B
            //Run(1000000000, instance1StartDate, instance1StartDate.AddDays(7));

            //// Instance 2 1B
            //Run(1000000000, instance2StartDate, instance2StartDate.AddDays(7));

            //// Instance 3 1B
            //Run(1000000000, instance3StartDate, instance3StartDate.AddDays(7));

            //// Instance 1 4B
            //Run(4000000000, instance1StartDate, instance1StartDate.AddDays(7));

            //// Instance 2 4B
            //Run(4000000000, instance2StartDate, instance2StartDate.AddDays(7));

            //// Instance 3 4B
            //Run(4000000000, instance3StartDate, instance3StartDate.AddDays(7));

            //// Instance 1 10B
            //Run(10000000000, instance1StartDate, instance1StartDate.AddDays(7));

            //// Instance 2 10B
            //Run(10000000000, instance2StartDate, instance2StartDate.AddDays(7));

            //// Instance 3 10B
            //Run(10000000000, instance3StartDate, instance3StartDate.AddDays(7));

            Console.WriteLine("\n*** Program finished ***");
            Console.ReadLine();
        }

        void Run(long targetIterationCount, DateTime planningStartTime, DateTime planningEndTime) {
            Console.WriteLine("\nRunning program with start date {0} and end date {1} for {2} iterations", planningStartTime, planningEndTime, ParseHelper.LargeNumToString(targetIterationCount));

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
                SaMultithreadHandler saMultithreadHandler = new SaMultithreadHandler(targetIterationCount);
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

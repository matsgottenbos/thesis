using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class App {
        public App() {
            Run();

            Console.WriteLine("\n*** Program finished ***");
            Console.ReadLine();
        }

        void Run() {
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

            Instance instance = GetInstance();

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
            SimulatedAnnealing simulatedAnnealing = new SimulatedAnnealing(instance);
            simulatedAnnealing.Run();
        }

        Instance GetInstance() {
            XorShiftRandom rand = AppConfig.DebugUseSeededSa ? new XorShiftRandom(1) : new XorShiftRandom();

            Instance instance;
            switch (AppConfig.SelectedDataSource) {
                case DataSource.Excel:
                    instance = ExcelDataImporter.Import(rand);
                    Console.WriteLine("Instance import from excel data complete");
                    break;

                case DataSource.Odata:
                    OdataImporter.Import();
                    throw new NotImplementedException();
                    break;
            }
            return instance;
        }
    }
}

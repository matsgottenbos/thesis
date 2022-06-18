using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class App {
        public App() {
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

            if (AppConfig.DebugRunInspector) {
                Console.WriteLine("Running debug inspector");
                new DebugInspector(instance);
            } else if (AppConfig.DebugRunJsonExporter) {
                Console.WriteLine("Running debug JSON exporter");
                new DebugJsonExporter(instance);
            } else if (AppConfig.DebugRunDelaysExporter) {
                Console.WriteLine("Running debug delays exporter");
                DebugDelaysExporter.Run();
            } else if (AppConfig.DebugRunTravelTimeProcesssor) {
                Console.WriteLine("Running debug travel time exporter");
                TravelInfoHandler.DetermineAndExportTravelInfo();
            } else {
                // Simulated annealing
                SimulatedAnnealing simulatedAnnealing = new SimulatedAnnealing(instance);
                simulatedAnnealing.Run();
            }

            Console.WriteLine("Program finished");
            Console.ReadLine();
        }
    }
}

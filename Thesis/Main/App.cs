using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class App {
        public App() {
            XorShiftRandom rand = Config.DebugUseSeededSa ? new XorShiftRandom(1) : new XorShiftRandom();

            Instance instance;
            switch (Config.SelectedDataSource) {
                case DataSource.Generator:
                    instance = DataGenerator.GenerateInstance(rand);
                    Console.WriteLine("Instance generation complete");
                    break;

                case DataSource.Excel:
                    instance = ExcelDataImporter.Import(rand);
                    Console.WriteLine("Instance import from excel data complete");
                    break;

                case DataSource.Odata:
                    OdataImporter.Import();
                    throw new NotImplementedException();
                    break;
            }

            // Run debug inspector if configured
            if (Config.DebugRunInspector) {
                new DebugInspector(instance);
                return;
            }

            // Run debug JSON exporter if configured
            if (Config.DebugRunJsonExporter) {
                new DebugJsonExporter(instance);
                return;
            }

            // Simulated annealing
            SimulatedAnnealing simulatedAnnealing = new SimulatedAnnealing(instance);
            simulatedAnnealing.Run();

            Console.ReadLine();
        }
    }
}

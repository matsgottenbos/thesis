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

            if (Config.DebugRunInspector) {
                Console.WriteLine("Running debug inspector");
                new DebugInspector(instance);
            } else if (Config.DebugRunJsonExporter) {
                Console.WriteLine("Running debug JSON exporter");
                new DebugJsonExporter(instance);
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

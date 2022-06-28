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

            XorShiftRandom appRand = AppConfig.DebugUseSeededSa ? new XorShiftRandom(1) : new XorShiftRandom();
            Instance instance = GetInstance(appRand);

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
            SaMultithreadHandler saMultithreadHandler = new SaMultithreadHandler();
            saMultithreadHandler.Run(instance, appRand);
        }

        static Instance GetInstance(XorShiftRandom appRand) {
            Instance instance;
            switch (AppConfig.SelectedDataSource) {
                case DataSource.Excel:
                    Console.WriteLine("Importing data from Excel...");
                    instance = ExcelDataImporter.Import(appRand);
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

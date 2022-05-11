﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class App {
        public App() {
            Random generatorRand = new Random(1);
            Random saRand = Config.DebugUseSeededSa ? generatorRand : new Random();
            XorShiftRandom saFastSand = Config.DebugUseSeededSa ? new XorShiftRandom(1) : new XorShiftRandom();

            Instance instance;
            switch (Config.SelectedDataSource) {
                case DataSource.Generator:
                    instance = DataGenerator.GenerateInstance(generatorRand);
                    Console.WriteLine("Instance generation complete");
                    break;

                case DataSource.Excel:
                    instance = ExcelDataImporter.Import(generatorRand);
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
            SimulatedAnnealing simulatedAnnealing = new SimulatedAnnealing(instance, saRand, saFastSand);
            SaInfo saSolution = simulatedAnnealing.Run();
            if (saSolution.Assignment == null) {
                Console.WriteLine("SA found no valid solution");
            } else {
                string saAssignmentStr = ParseHelper.AssignmentToString(saSolution.Assignment, saSolution);
                Console.WriteLine("SA cost: {0}  |  {1}", ParseHelper.ToString(saSolution.Cost), saAssignmentStr);
            }

            Console.ReadLine();
        }
    }
}

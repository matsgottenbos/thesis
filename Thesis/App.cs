using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class App {
        public App() {
            Random rand = Config.DebugUseSeededSa ? new Random(1) : new Random();

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

            // Debug inspector
            if (Config.DebugRunInspector) {
                new SaInspector(instance);
            }

            // Solve optimally
            if (Config.RunOptimalAlgorithm) {
                OptimalSolver optimalSolver = new OptimalSolver(instance);
                Solution optimalSolution = optimalSolver.Solve();

                string optimalAssignmentStr = "";
                if (optimalSolution == null) {
                    Console.ReadLine();
                    return;
                } else {
                    optimalAssignmentStr = string.Join(' ', optimalSolution.Assignment.Select(driver => driver.GetId()));
                    Console.WriteLine("Optimal cost: {0}  |  {1}", ParseHelper.ToString(optimalSolution.Cost), optimalAssignmentStr);
                }
            }

            // Simulated annealing
            if (Config.RunSimulatedAnnealing) {
                Random rand2;
                XorShiftRandom fastRand2;
                if (Config.DebugUseSeededSa) {
                    rand2 = new Random(1);
                    fastRand2 = new XorShiftRandom(1);
                } else {
                    rand2 = new Random();
                    fastRand2 = new XorShiftRandom();
                }

                SimulatedAnnealing simulatedAnnealing = new SimulatedAnnealing(instance, rand2, fastRand2);
                SaInfo saSolution = simulatedAnnealing.Run();
                if (saSolution.Assignment == null) {
                    Console.WriteLine("SA found no valid solution");
                } else {
                    string saAssignmentStr = ParseHelper.AssignmentToString(saSolution.Assignment, saSolution);
                    Console.WriteLine("SA cost: {0}  |  {1}", ParseHelper.ToString(saSolution.Cost), saAssignmentStr);
                }
            }

            Console.ReadLine();
        }
    }
}

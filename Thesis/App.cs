using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class App {
        public App() {
            //Random rand = new Random();
            Random rand = new Random(1);

            // Generate instance
            Generator generator = new Generator(rand);
            Instance instance = generator.GenerateInstance();
            Console.WriteLine("Instance generation complete");

            // Solve optimally
            OptimalSolver optimalSolver = new OptimalSolver(instance);
            Solution optimalSolution = optimalSolver.Solve();
            Console.WriteLine("Optimal cost: {0}", optimalSolution.Cost);

            // Determine lower bounds
            //LowerBoundCalculator lowerBoundCalculator = new LowerBoundCalculator(instance);
            //float lowerBound1 = lowerBoundCalculator.CalculateLowerBound1();
            //float lowerBound2 = lowerBoundCalculator.CalculateLowerBound2();
            //Console.WriteLine("Lower bound 1: {0}", lowerBound1);
            //Console.WriteLine("Lower bound 2: {0}", lowerBound2);

            // Simulated annealing
            SimulatedAnnealing simulatedAnnealing = new SimulatedAnnealing(instance, rand);
            (double saCost, Driver[] saSolution) = simulatedAnnealing.Run();
            Console.WriteLine("SA cost: {0}", saCost);
            Console.WriteLine("Optimal cost: {0}", optimalSolution.Cost);

            Console.ReadLine();
        }
    }
}

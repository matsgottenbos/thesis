using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class SimulatedAnnealing {
        readonly SaInfo info;

        public SimulatedAnnealing(Instance instance, Random rand, XorShiftRandom fastRand) {
            info = new SaInfo(instance, rand, fastRand);

            // Initialise variables
            info.IterationNum = 0;
            info.Temperature = Config.SaInitialTemperature;
            info.PenaltyFactor = Config.SaInitialPenaltyFactor;
            info.BestCost = double.MaxValue;
            info.IsHotelStayAfterTrip = new bool[instance.Trips.Length];
            info.ExternalDriverCountsByType = new int[instance.ExternalDriversByType.Length];

            // Create a random assignment
            info.Assignment = new Driver[instance.Trips.Length];
            int[] assignmentIndices = GetInitialAssignmentIndices();
            Trip[] driverLastTrips = new Trip[instance.AllDrivers.Length];
            for (int tripIndex = 0; tripIndex < assignmentIndices.Length; tripIndex++) {
                Trip trip = instance.Trips[tripIndex];
                int driverIndex = assignmentIndices[tripIndex];
                Driver driver = instance.AllDrivers[driverIndex];
                info.Assignment[tripIndex] = driver;
            }

            #if DEBUG
            // Initialise debugger
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.ResetIteration(info);
            }
            #endif

            // Get cost of initial assignment
            (info.Cost, info.CostWithoutPenalty,  info.BasePenalty, info.DriversWorkedTime) = TotalCostCalculator.GetAssignmentCost(info);

            #if DEBUG
            // Reset iteration in debugger after initial assignment cost
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.ResetIteration(info);
            }
            #endif
        }

        public (double, Driver[]) Run() {
            // Start stopwatch
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (info.IterationNum < Config.SaIterationCount) {
                int operationIndex = info.Rand.Next(5);
                AbstractOperation operation = operationIndex switch {
                    // Assign internal
                    0 => AssignInternalOperation.CreateRandom(info),
                    1 => AssignInternalOperation.CreateRandom(info),

                    // Assign existing external
                    2 => AssignExternalOperation.CreateRandom(info),

                    // Swap
                    3 => SwapOperation.CreateRandom(info),

                    // Hotel stay
                    4 => ToggleHotelOperation.CreateRandom(info),

                    _ => throw new Exception("Invalid operation index"),
                };

                (double costDiff, double costWithoutPenaltyDiff, double basePenaltyDiff) = operation.GetCostDiff();

                bool isAccepted = costDiff < 0 || info.FastRand.NextDouble() < Math.Exp(-costDiff / info.Temperature);
                if (isAccepted) {
                    operation.Execute();
                    info.Cost += costDiff;
                    info.CostWithoutPenalty += costWithoutPenaltyDiff;
                    info.BasePenalty += basePenaltyDiff;

                    if (info.Cost < info.BestCost && info.BasePenalty < 0.01) {
                        // Check cost to remove floating point imprecisions
                        (info.Cost, info.CostWithoutPenalty, info.BasePenalty, info.DriversWorkedTime) = TotalCostCalculator.GetAssignmentCost(info);

                        if (info.Cost < info.BestCost) {
                            info.BestCost = info.Cost;
                            info.BestAssignment = (Driver[])info.Assignment.Clone();
                        }
                    }
                }

                // Update iteration number
                info.IterationNum++;

                #if DEBUG
                // Set debugger to next iteration
                if (Config.DebugCheckAndLogOperations) {
                    SaDebugger.NextIteration(info);
                }
                #endif

                // Check cost to remove floating point imprecisions
                if (info.IterationNum % Config.SaCheckCostFrequency == 0) {
                    (info.Cost, info.CostWithoutPenalty, info.BasePenalty, info.DriversWorkedTime) = TotalCostCalculator.GetAssignmentCost(info);
                }

                // Log
                if (info.IterationNum % Config.SaLogFrequency == 0) {
                    string bestCostString = info.BestAssignment == null ? "" : ParseHelper.ToString(info.BestCost);
                    string penaltyString = info.BasePenalty > 0 ? ParseHelper.ToString(info.BasePenalty, "0") : "-";
                    string assignmentStr = info.BestAssignment == null ? "" : string.Join(' ', info.BestAssignment.Select(driver => driver.GetId()));
                    Console.WriteLine("# {0,4}    Best cost: {1,10}    Cost: {2,10}    Penalty: {3,6}    Temp: {4,5}    P.factor: {5,5}    Best sol.: {6}", ParseHelper.LargeNumToString(info.IterationNum), bestCostString, ParseHelper.ToString(info.CostWithoutPenalty), penaltyString, ParseHelper.ToString(info.Temperature, "0"), ParseHelper.ToString(info.PenaltyFactor, "0.00"), assignmentStr);
                }

                // Update temperature and penalty factor
                if (info.IterationNum % Config.SaParameterUpdateFrequency == 0) {
                    info.Temperature *= Config.SaTemperatureReductionFactor;
                    info.PenaltyFactor = Math.Min(1, info.PenaltyFactor + Config.SaPenaltyIncrement);
                    (info.Cost, info.CostWithoutPenalty, info.BasePenalty, info.DriversWorkedTime) = TotalCostCalculator.GetAssignmentCost(info);
                }

                #if DEBUG
                // Reset iteration in debugger after additional checks
                if (Config.DebugCheckAndLogOperations) {
                    SaDebugger.ResetIteration(info);
                }
                #endif
            }

            // Check cost to remove floating point imprecisions
            info.PenaltyFactor = 1;
            (info.BestCost, _, _, _) = TotalCostCalculator.GetAssignmentCost(info);

            stopwatch.Stop();
            float saDuration = stopwatch.ElapsedMilliseconds / 1000f;
            float saSpeed = Config.SaIterationCount / saDuration;
            Console.WriteLine("SA finished {0} iterations in {1} s  |  Speed: {2} iterations/s", ParseHelper.LargeNumToString(info.IterationNum), ParseHelper.ToString(saDuration), ParseHelper.LargeNumToString(saSpeed));

            return (info.BestCost, info.BestAssignment);
        }

        int[] GetInitialAssignmentIndices() {
            // Create an initial assignment with only internal drivers
            int[] assignmentIndices = new int[info.Instance.Trips.Length];
            for (int tripIndex = 0; tripIndex < info.Instance.Trips.Length; tripIndex++) {
                assignmentIndices[tripIndex] = info.Rand.Next(info.Instance.InternalDrivers.Length);
            }
            return assignmentIndices;
        }
    }

    
}

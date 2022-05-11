using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class SimulatedAnnealing {
        readonly SaInfo info, bestInfo;

        public SimulatedAnnealing(Instance instance, Random rand, XorShiftRandom fastRand) {
            // Initialise info
            info = new SaInfo(instance, rand, fastRand);
            info.IterationNum = 0;
            info.Temperature = Config.SaInitialTemperature;
            info.IsHotelStayAfterTrip = new bool[instance.Trips.Length];

            // Initialise best info
            bestInfo = new SaInfo(instance, rand, fastRand);
            bestInfo.IterationNum = -1;
            bestInfo.Temperature = -1;
            bestInfo.Cost = double.MaxValue;


            // Create a random assignment
            (info.Assignment, info.ExternalDriverCountsByType) = GetInitialAssignment();

            // Determine driver paths
            info.DriverPaths = new List<Trip>[info.Instance.AllDrivers.Length];
            info.DriverPathIndices = new int[info.Instance.Trips.Length];
            for (int driverIndex = 0; driverIndex < info.Instance.AllDrivers.Length; driverIndex++) {
                info.DriverPaths[driverIndex] = new List<Trip>();
            }
            for (int tripIndex = 0; tripIndex < info.Instance.Trips.Length; tripIndex++) {
                Trip trip = info.Instance.Trips[tripIndex];
                Driver driver = info.Assignment[tripIndex];
                List<Trip> driverPath = info.DriverPaths[driver.AllDriversIndex];
                info.DriverPathIndices[trip.Index] = driverPath.Count;
                driverPath.Add(trip);
            }

            #if DEBUG
            // Initialise debugger
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.ResetIteration(info);
            }
            #endif

            // Get cost of initial assignment
            (info.Cost, info.CostWithoutPenalty, info.Penalty, info.Satisfaction, info.DriverInfos, info.PenaltyInfo) = TotalCostCalculator.GetAssignmentCost(info);

            #if DEBUG
            // Reset iteration in debugger after initial assignment cost
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.ResetIteration(info);
            }
            #endif
        }

        public SaInfo Run() {
            Console.WriteLine("Starting simulated annealing");

            // Start stopwatch
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Log initial assignment
            LogIteration();

            while (info.IterationNum < Config.SaIterationCount) {
                // Pick a random operation based on the configured probabilities
                double operationDouble = info.FastRand.NextDouble();
                AbstractOperation operation;
                if (operationDouble < Config.AssignInternalProbCumulative) operation = AssignInternalOperation.CreateRandom(info);
                else if (operationDouble < Config.AssignExternalProbCumulative) operation = AssignExternalOperation.CreateRandom(info);
                else if (operationDouble < Config.SwapProbCumulative) operation = SwapOperation.CreateRandom(info);
                else operation = ToggleHotelOperation.CreateRandom(info);

                // TODO: WIP
                (double costDiff, double satisfactionDiff) = operation.GetCostDiff();
                double oldAdjustedCost = info.Cost * (1.2 - info.Satisfaction / 5f);
                double newAdjustedCost = (info.Cost + costDiff) * (1.2 - (info.Satisfaction + satisfactionDiff) / 5f);
                double adjustedCostDiff = newAdjustedCost - oldAdjustedCost;

                bool isAccepted = adjustedCostDiff < 0 || info.FastRand.NextDouble() < Math.Exp(-adjustedCostDiff / info.Temperature);
                if (isAccepted) {
                    operation.Execute();

                    if (info.Cost < bestInfo.Cost && info.Penalty < 0.01) {
                        // Check cost to remove floating point imprecisions
                        (info.Cost, info.CostWithoutPenalty, info.Penalty, info.Satisfaction, info.DriverInfos, info.PenaltyInfo) = TotalCostCalculator.GetAssignmentCost(info);
                        if (info.Penalty > 0.01) throw new Exception("New best solution is invalid");

                        if (info.Cost < bestInfo.Cost) {
                            bestInfo.Cost = info.Cost;
                            bestInfo.Satisfaction = info.Satisfaction;
                            bestInfo.Assignment = (Driver[])info.Assignment.Clone();
                            bestInfo.IsHotelStayAfterTrip = (bool[])info.IsHotelStayAfterTrip.Clone();
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

                // Log
                if (info.IterationNum % Config.SaLogFrequency == 0) {
                    // Check cost to remove floating point imprecisions
                    (info.Cost, info.CostWithoutPenalty, info.Penalty, info.Satisfaction, info.DriverInfos, info.PenaltyInfo) = TotalCostCalculator.GetAssignmentCost(info);

                    LogIteration();
                }

                // Update temperature and penalty factor
                if (info.IterationNum % Config.SaParameterUpdateFrequency == 0) {
                    info.Temperature *= Config.SaTemperatureReductionFactor;

                    // Check if we should end the cycle
                    if (info.Temperature <= Config.SaEndCycleTemperature) {
                        info.CycleNum++;
                        info.Temperature = (float)info.FastRand.NextDouble() * (Config.SaCycleInitialTemperatureMax - Config.SaCycleInitialTemperatureMin) + Config.SaCycleInitialTemperatureMin;
                    }

                    (info.Cost, info.CostWithoutPenalty, info.Penalty, info.Satisfaction, info.DriverInfos, info.PenaltyInfo) = TotalCostCalculator.GetAssignmentCost(info);
                }

                #if DEBUG
                // Reset iteration in debugger after additional checks
                if (Config.DebugCheckAndLogOperations) {
                    SaDebugger.ResetIteration(info);
                }
                #endif
            }

            // Check cost to remove floating point imprecisions
            if (bestInfo.Assignment != null) {
                (bestInfo.Cost, bestInfo.CostWithoutPenalty, bestInfo.Penalty, info.Satisfaction, info.DriverInfos, info.PenaltyInfo) = TotalCostCalculator.GetAssignmentCost(bestInfo);
                if (bestInfo.Penalty > 0.01) throw new Exception("Best solution is invalid");
                bestInfo.DriverInfos = info.DriverInfos;
                bestInfo.ExternalDriverCountsByType = info.ExternalDriverCountsByType;
                bestInfo.IterationNum = info.IterationNum;
            }

            stopwatch.Stop();
            float saDuration = stopwatch.ElapsedMilliseconds / 1000f;
            float saSpeed = Config.SaIterationCount / saDuration;
            Console.WriteLine("SA finished {0} iterations in {1} s  |  Speed: {2} iterations/s", ParseHelper.LargeNumToString(info.IterationNum), ParseHelper.ToString(saDuration), ParseHelper.LargeNumToString(saSpeed));

            return bestInfo;
        }

        void LogIteration() {
            string bestCostString = bestInfo.Assignment == null ? "" : string.Format("{0,10} ({1,3}%)", ParseHelper.ToString(bestInfo.Cost, "0.0"), ParseHelper.ToString(bestInfo.Satisfaction * 100, "0"));
            Console.WriteLine("# {0,4}    Cycle: {1,3}    Best cost: {2,17}    Cost: {3,10} ({4,3}%)    Temp: {5,5}    Penalty: {6,6}", ParseHelper.LargeNumToString(info.IterationNum), info.CycleNum, bestCostString, ParseHelper.ToString(info.CostWithoutPenalty, "0.0"), ParseHelper.ToString(info.Satisfaction * 100, "0"), ParseHelper.ToString(info.Temperature, "0"), ParseHelper.GetPenaltyString(info));

            if (Config.DebugSaLogAdditionalInfo) {
                Console.WriteLine("Worked times: {0}", ParseHelper.ToString(info.DriverInfos.Select(driver => driver.WorkedTime).ToArray()));
                Console.WriteLine("Shift counts: {0}", ParseHelper.ToString(info.DriverInfos.Select(driver => driver.ShiftCount).ToArray()));
            }

            if (Config.DebugSaLogCurrentSolution) {
                Console.WriteLine("Current solution: {0}", ParseHelper.AssignmentToString(info.Assignment, info));
            }

            if (bestInfo.Assignment != null) {
                Console.WriteLine("Best solution: {0}", ParseHelper.AssignmentToString(bestInfo.Assignment, bestInfo));
            }
        }

        (Driver[], int[]) GetInitialAssignment() {
            Driver[] assignment = new Driver[info.Instance.Trips.Length];
            List<Trip>[] driverPaths = new List<Trip>[info.Instance.AllDrivers.Length];
            for (int i = 0; i < driverPaths.Length; i++) driverPaths[i] = new List<Trip>();
            int[] externalDriverCountsByType = new int[info.Instance.ExternalDriversByType.Length];

            for (int tripIndex = 0; tripIndex < info.Instance.Trips.Length; tripIndex++) {
                Trip trip = info.Instance.Trips[tripIndex];

                // Greedily assign to random internal driver, if possible without precedence violations
                InternalDriver[] internalDriversRandomOrder = Copy(info.Instance.InternalDrivers);
                Shuffle(internalDriversRandomOrder);
                bool isDone = false;
                for (int shuffledInternalDriverIndex = 0; shuffledInternalDriverIndex < internalDriversRandomOrder.Length; shuffledInternalDriverIndex++) {
                    InternalDriver internalDriver = internalDriversRandomOrder[shuffledInternalDriverIndex];
                    List<Trip> driverPath = driverPaths[internalDriver.AllDriversIndex];

                    if (driverPath.Count == 0 || info.Instance.IsValidPrecedence(driverPath[^1], trip)) {
                        // We can add this trip to this driver without precedence violations
                        assignment[tripIndex] = internalDriver;
                        driverPath.Add(trip);
                        isDone = true;
                        break;
                    }
                }
                if (isDone) continue;

                // Greedily assign to random external driver, if possible without precedence violations
                ExternalDriver[][] externalDriverTypesRandomOrder = Copy(info.Instance.ExternalDriversByType);
                Shuffle(externalDriverTypesRandomOrder);
                for (int shuffledExternalDriverTypeIndex = 0; shuffledExternalDriverTypeIndex < externalDriverTypesRandomOrder.Length; shuffledExternalDriverTypeIndex++) {
                    ExternalDriver[] externalDriversInType = externalDriverTypesRandomOrder[shuffledExternalDriverTypeIndex];

                    // Assign to first possible driver in type
                    for (int externalDriverIndexInType = 0; externalDriverIndexInType < externalDriversInType.Length; externalDriverIndexInType++) {
                        ExternalDriver externalDriver = externalDriversInType[externalDriverIndexInType];
                        List<Trip> driverPath = driverPaths[externalDriver.AllDriversIndex];

                        if (driverPath.Count == 0 || info.Instance.IsValidPrecedence(driverPath[^1], trip)) {
                            // We can add this trip to this driver without precedence violations
                            assignment[tripIndex] = externalDriver;
                            driverPath.Add(trip);
                            externalDriverCountsByType[externalDriver.ExternalDriverTypeIndex]++;
                            isDone = true;
                            break;
                        }
                    }
                    if (isDone) break;
                }
                if (isDone) continue;

                // Assigning without precedence violations is impossible, so assign to random external driver
                int randomExternalDriverTypeIndex = info.FastRand.NextInt(info.Instance.ExternalDriversByType.Length);
                ExternalDriver[] externalDriversInRandomType = info.Instance.ExternalDriversByType[randomExternalDriverTypeIndex];
                int randomExternalDriverIndexInType = info.FastRand.NextInt(externalDriversInRandomType.Length);
                ExternalDriver randomExternalDriver = externalDriversInRandomType[randomExternalDriverIndexInType];
                List<Trip> randomDriverPath = driverPaths[randomExternalDriver.AllDriversIndex];
                assignment[tripIndex] = randomExternalDriver;
                randomDriverPath.Add(trip);
                externalDriverCountsByType[randomExternalDriver.ExternalDriverTypeIndex]++;
            }
            return (assignment, externalDriverCountsByType);
        }

        void Shuffle<T>(T[] array) {
            // Fisher–Yates shuffle
            int n = array.Length;
            while (n > 1) {
                int k = info.FastRand.NextInt(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }

        T[] Copy<T>(T[] array) {
            T[] copy = new T[array.Length];
            for (int i = 0; i < array.Length; i++) {
                copy[i] = array[i];
            }
            return copy;
        }
    }


}

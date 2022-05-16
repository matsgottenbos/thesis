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
        readonly SaInfo[] bestInfoBySatisfaction;

        public SimulatedAnnealing(Instance instance) {
            // Initialise info
            info = new SaInfo(instance);
            info.Temperature = Config.SaInitialTemperature;
            info.SatisfactionFactor = (float)info.Instance.Rand.NextDouble(Config.SaCycleMinSatisfactionFactor, Config.SaCycleMaxSatisfactionFactor);
            info.IsHotelStayAfterTrip = new bool[instance.Trips.Length];

            // Initialise best info
            bestInfoBySatisfaction = new SaInfo[Config.PercentageFactor];
            for (int i = 0; i < bestInfoBySatisfaction.Length; i++) {
                SaInfo initialBestInfo = new SaInfo(instance);
                initialBestInfo.TotalInfo = new DriverInfo() {
                    Cost = double.MaxValue,
                    Satisfaction = -1,
                };
                bestInfoBySatisfaction[i] = initialBestInfo;
            }

            // Create a random assignment
            (info.Assignment, info.ExternalDriverCountsByType) = GetInitialAssignment();
            info.ProcessDriverPaths();

            #if DEBUG
            // Initialise debugger
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.ResetIteration(info);
                SaDebugger.GetCurrentOperation().StartPart("Initial assignment", null);
                SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.OldChecked);
            }
            #endif

            // Get cost of initial assignment
            (info.TotalInfo, info.DriverInfos) = TotalCostCalculator.GetAssignmentCost(info);

            #if DEBUG
            // Reset iteration in debugger after initial assignment cost
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.ResetIteration(info);
            }
            #endif
        }

        public List<SaInfo> Run() {
            Console.WriteLine("Starting simulated annealing");

            // Start stopwatch
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Log initial assignment
            LogIteration();

            XorShiftRandom fastRand = info.Instance.Rand;

            while (info.IterationNum < Config.SaIterationCount) {
                // Pick a random operation based on the configured probabilities
                double operationDouble = fastRand.NextDouble();
                AbstractOperation operation;
                if (operationDouble < Config.AssignInternalProbCumulative) operation = AssignInternalOperation.CreateRandom(info);
                else if (operationDouble < Config.AssignExternalProbCumulative) operation = AssignExternalOperation.CreateRandom(info);
                else if (operationDouble < Config.SwapProbCumulative) operation = SwapOperation.CreateRandom(info);
                else operation = ToggleHotelOperation.CreateRandom(info);

                DriverInfo totalInfoDiff = operation.GetCostDiff();
                double oldAdjustedCost = GetAdjustedCost(info.TotalInfo.Cost, info.TotalInfo.Satisfaction, info.SatisfactionFactor);
                double newAdjustedCost = GetAdjustedCost(info.TotalInfo.Cost + totalInfoDiff.Cost, info.TotalInfo.Satisfaction + totalInfoDiff.Satisfaction, info.SatisfactionFactor);
                double adjustedCostDiff = newAdjustedCost - oldAdjustedCost;

                bool isAccepted = adjustedCostDiff < 0 || fastRand.NextDouble() < Math.Exp(-adjustedCostDiff / info.Temperature);
                if (isAccepted) {
                    operation.Execute();

                    int satisfactionLevel = (int)Math.Round(info.TotalInfo.Satisfaction * Config.PercentageFactor);
                    if (info.TotalInfo.Penalty < 0.01 && info.TotalInfo.Cost < bestInfoBySatisfaction[satisfactionLevel].TotalInfo.Cost) {
                        info.LastImprovementIteration = info.IterationNum;
                        info.HasImprovementSinceLog = true;

                        // Check cost to remove floating point imprecisions
                        (info.TotalInfo, info.DriverInfos) = TotalCostCalculator.GetAssignmentCost(info);

                        #if DEBUG
                        // Set debugger to next iteration
                        if (Config.DebugCheckAndLogOperations) {
                            if (info.TotalInfo.Penalty > 0.01) throw new Exception("New best solution is invalid");
                        }
                        #endif

                        // Store as the best solution for this satisfaction level
                        SaInfo bestInfo = info.CopyForBestInfo();
                        bestInfoBySatisfaction[satisfactionLevel] = bestInfo;

                        // Check if this solution also improves on best solutions for lower satisfaction levels
                        for (int searchSatisfactionLevel = satisfactionLevel - 1; searchSatisfactionLevel >= 0; searchSatisfactionLevel--) {
                            if (info.TotalInfo.Cost < bestInfoBySatisfaction[searchSatisfactionLevel].TotalInfo.Cost) {
                                bestInfoBySatisfaction[searchSatisfactionLevel] = bestInfo;
                            }
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
                    (info.TotalInfo, info.DriverInfos) = TotalCostCalculator.GetAssignmentCost(info);

                    LogIteration();
                }

                // Update temperature and penalty factor
                if (info.IterationNum % Config.SaParameterUpdateFrequency == 0) {
                    info.Temperature *= Config.SaTemperatureReductionFactor;

                    // Check if we should end the cycle
                    if (info.Temperature <= Config.SaEndCycleTemperature) {
                        info.CycleNum++;
                        info.Temperature = (float)fastRand.NextDouble(Config.SaCycleMinInitialTemperature, Config.SaCycleMaxInitialTemperature);
                        info.SatisfactionFactor = (float)fastRand.NextDouble(Config.SaCycleMinSatisfactionFactor, Config.SaCycleMaxSatisfactionFactor);
                    }

                    (info.TotalInfo, info.DriverInfos) = TotalCostCalculator.GetAssignmentCost(info);
                }

                #if DEBUG
                // Reset iteration in debugger after additional checks
                if (Config.DebugCheckAndLogOperations) {
                    SaDebugger.ResetIteration(info);
                }
                #endif
            }

            // Get Pareto-optimal front
            List<SaInfo> paretoFront = GetParetoFront(bestInfoBySatisfaction);

            stopwatch.Stop();
            float saDuration = stopwatch.ElapsedMilliseconds / 1000f;
            float saSpeed = Config.SaIterationCount / saDuration;
            Console.WriteLine("SA finished {0} iterations in {1} s  |  Speed: {2} iterations/s", ParseHelper.LargeNumToString(info.IterationNum), ParseHelper.ToString(saDuration), ParseHelper.LargeNumToString(saSpeed));

            if (paretoFront.Count == 0) {
                Console.WriteLine("SA found no valid solution");
            } else {
                Console.WriteLine("Pareto-optimal front: {0}", ParetoFrontToString(paretoFront));

                for (int i = 0; i < paretoFront.Count; i++) {
                    SaInfo paretoPoint = paretoFront[i];
                    Console.WriteLine("\nPoint {0}\n{1}", ParetoPointToString(paretoPoint), ParseHelper.AssignmentToString(paretoPoint));
                }
            }

            return paretoFront;
        }

        static double GetAdjustedCost(double cost, double satisfaction, float satisfactionFactor) {
            return cost * (1 + (1 - satisfaction) * satisfactionFactor);
        }

        void LogIteration() {
            // Get Pareto-optimal front
            List<SaInfo> paretoFront = GetParetoFront(bestInfoBySatisfaction);
            string paretoFrontStr;
            if (paretoFront.Count == 0) {
                paretoFrontStr = "No valid solutions";
            } else {
                paretoFrontStr = "Front: " + ParetoFrontToString(paretoFront);
            }

            string lastImprovementIterationStr = info.LastImprovementIteration.HasValue ? ParseHelper.LargeNumToString(info.LastImprovementIteration.Value, "0") : "-";
            string hasImprovementStr = info.HasImprovementSinceLog ? " !!!" : "";

            // Log basic info
            Console.WriteLine("# {0,4}    Improve: {1,4}    Cycle: {2,3}    Cost: {3,10} ({4,3}%)    Temp: {5,5}    Sat.f: {6,4}    Penalty: {7,-40}    {8}{9}", ParseHelper.LargeNumToString(info.IterationNum), lastImprovementIterationStr, info.CycleNum, ParseHelper.LargeNumToString(info.TotalInfo.CostWithoutPenalty, "0.0"), ParseHelper.ToString(info.TotalInfo.Satisfaction * 100, "0"), ParseHelper.ToString(info.Temperature, "0"), ParseHelper.ToString(info.SatisfactionFactor, "0.00"), ParseHelper.GetPenaltyString(info.TotalInfo), paretoFrontStr, hasImprovementStr);

            if (Config.DebugSaLogAdditionalInfo) {
                Console.WriteLine("Worked times: {0}", ParseHelper.ToString(info.DriverInfos.Select(driver => driver.WorkedTime).ToArray()));
                Console.WriteLine("Shift counts: {0}", ParseHelper.ToString(info.DriverInfos.Select(driver => driver.ShiftCount).ToArray()));
            }

            if (Config.DebugSaLogCurrentSolution) {
                Console.WriteLine("Current solution: {0}", ParseHelper.AssignmentToString(info));
            }

            //if (bestInfo.Assignment != null) {
            //    Console.WriteLine("Best solution: {0}", ParseHelper.AssignmentToString(bestInfo));
            //}

            info.HasImprovementSinceLog = false;
        }

        static List<SaInfo> GetParetoFront(SaInfo[] bestInfoBySatisfaction) {
            List<SaInfo> paretoFront = new List<SaInfo>();
            SaInfo bestOfPrevLevel = null;
            for (int satisfactionLevel = bestInfoBySatisfaction.Length - 1; satisfactionLevel >= 0; satisfactionLevel--) {
                SaInfo bestInfoOfLevel = bestInfoBySatisfaction[satisfactionLevel];
                if (bestInfoOfLevel.TotalInfo.Cost == double.MaxValue) continue;

                if (bestInfoOfLevel != bestOfPrevLevel) {
                    paretoFront.Add(bestInfoOfLevel);
                }
                bestOfPrevLevel = bestInfoOfLevel;
            }
            paretoFront.Reverse();
            return paretoFront;
        }

        static string ParetoFrontToString(List<SaInfo> paretoFront) {
            return string.Join(' ', paretoFront.Select(paretoPoint => ParetoPointToString(paretoPoint)));
        }

        static string ParetoPointToString(SaInfo paretoPoint) {
            return string.Format("({0}%: {1})", ParseHelper.ToString(paretoPoint.TotalInfo.Satisfaction * Config.PercentageFactor, "0"), ParseHelper.LargeNumToString(paretoPoint.TotalInfo.Cost, "0"));
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
                int randomExternalDriverTypeIndex = info.Instance.Rand.Next(info.Instance.ExternalDriversByType.Length);
                ExternalDriver[] externalDriversInRandomType = info.Instance.ExternalDriversByType[randomExternalDriverTypeIndex];
                int randomExternalDriverIndexInType = info.Instance.Rand.Next(externalDriversInRandomType.Length);
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
                int k = info.Instance.Rand.Next(n--);
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

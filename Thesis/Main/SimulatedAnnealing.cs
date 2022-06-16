using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
            info.Temperature = SaConfig.SaInitialTemperature;
            info.SatisfactionFactor = (float)info.Instance.Rand.NextDouble(SaConfig.SaCycleMinSatisfactionFactor, SaConfig.SaCycleMaxSatisfactionFactor);
            info.IsHotelStayAfterTrip = new bool[instance.Trips.Length];

            // Initialise best info
            bestInfoBySatisfaction = new SaInfo[MiscConfig.PercentageFactor];
            for (int i = 0; i < bestInfoBySatisfaction.Length; i++) {
                SaInfo initialBestInfo = new SaInfo(instance);
                initialBestInfo.TotalInfo = new SaTotalInfo() {
                    Stats = new SaStats() {
                        Cost = double.MaxValue,
                        SatisfactionScore = -1,
                    },
                };
                bestInfoBySatisfaction[i] = initialBestInfo;
            }

            // Create a random assignment
            info.Assignment = GetInitialAssignment();
            info.ProcessDriverPaths();

            #if DEBUG
            // Initialise debugger
            if (AppConfig.DebugCheckAndLogOperations) {
                SaDebugger.ResetIteration(info);
                SaDebugger.GetCurrentOperation().StartPart("Initial assignment", null);
                SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.OldChecked);
            }
            #endif

            // Get cost of initial assignment
            TotalCostCalculator.ProcessAssignmentCost(info);

            #if DEBUG
            // Reset iteration in debugger after initial assignment cost
            if (AppConfig.DebugCheckAndLogOperations) {
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
            LogIteration(stopwatch);

            XorShiftRandom fastRand = info.Instance.Rand;

            while (info.IterationNum < SaConfig.SaIterationCount) {
                // Pick a random operation based on the configured probabilities
                double operationDouble = fastRand.NextDouble();
                AbstractOperation operation;
                if (operationDouble < SaConfig.AssignInternalProbCumulative) operation = AssignInternalOperation.CreateRandom(info);
                else if (operationDouble < SaConfig.AssignExternalProbCumulative) operation = AssignExternalOperation.CreateRandom(info);
                else if (operationDouble < SaConfig.SwapProbCumulative) operation = SwapOperation.CreateRandom(info);
                else operation = ToggleHotelOperation.CreateRandom(info);

                SaTotalInfo totalInfoDiff = operation.GetCostDiff();
                double oldAdjustedCost = GetAdjustedCost(info.TotalInfo.Stats.Cost, info.TotalInfo.Stats.SatisfactionScore.Value, info.SatisfactionFactor);
                double newAdjustedCost = GetAdjustedCost(info.TotalInfo.Stats.Cost + totalInfoDiff.Stats.Cost, info.TotalInfo.Stats.SatisfactionScore.Value + totalInfoDiff.Stats.SatisfactionScore.Value, info.SatisfactionFactor);
                double adjustedCostDiff = newAdjustedCost - oldAdjustedCost;

                bool isAccepted = adjustedCostDiff < 0 || fastRand.NextDouble() < Math.Exp(-adjustedCostDiff / info.Temperature);
                if (isAccepted) {
                    operation.Execute();

                    int satisfactionLevel = (int)Math.Round(info.TotalInfo.Stats.SatisfactionScore.Value * MiscConfig.PercentageFactor);
                    if (info.TotalInfo.Stats.Penalty < 0.01 && info.TotalInfo.Stats.Cost < bestInfoBySatisfaction[satisfactionLevel].TotalInfo.Stats.Cost) {
                        info.LastImprovementIteration = info.IterationNum;
                        info.HasImprovementSinceLog = true;

                        // Check cost to remove floating point imprecisions
                        TotalCostCalculator.ProcessAssignmentCost(info);

                        #if DEBUG
                        // Set debugger to next iteration
                        if (AppConfig.DebugCheckAndLogOperations) {
                            if (info.TotalInfo.Stats.Penalty > 0.01) throw new Exception("New best solution is invalid");
                        }
                        #endif

                        // Store as the best solution for this satisfaction level
                        SaInfo bestInfo = info.CopyForBestInfo();
                        bestInfoBySatisfaction[satisfactionLevel] = bestInfo;

                        // Check if this solution also improves on best solutions for lower satisfaction levels
                        for (int searchSatisfactionLevel = satisfactionLevel - 1; searchSatisfactionLevel >= 0; searchSatisfactionLevel--) {
                            if (info.TotalInfo.Stats.Cost < bestInfoBySatisfaction[searchSatisfactionLevel].TotalInfo.Stats.Cost) {
                                bestInfoBySatisfaction[searchSatisfactionLevel] = bestInfo;
                            }
                        }
                    }
                }

                // Update iteration number
                info.IterationNum++;

                #if DEBUG
                // Set debugger to next iteration
                if (AppConfig.DebugCheckAndLogOperations) {
                    SaDebugger.NextIteration(info);
                }
                #endif

                // Log
                if (info.IterationNum % SaConfig.SaLogFrequency == 0) {
                    // Check cost to remove floating point imprecisions
                    TotalCostCalculator.ProcessAssignmentCost(info);

                    LogIteration(stopwatch);
                }

                // Update temperature and penalty factor
                if (info.IterationNum % SaConfig.SaParameterUpdateFrequency == 0) {
                    info.Temperature *= SaConfig.SaTemperatureReductionFactor;

                    // Check if we should end the cycle
                    if (info.Temperature <= SaConfig.SaEndCycleTemperature) {
                        info.CycleNum++;
                        info.Temperature = (float)fastRand.NextDouble(SaConfig.SaCycleMinInitialTemperature, SaConfig.SaCycleMaxInitialTemperature);
                        info.SatisfactionFactor = (float)fastRand.NextDouble(SaConfig.SaCycleMinSatisfactionFactor, SaConfig.SaCycleMaxSatisfactionFactor);
                    }

                    TotalCostCalculator.ProcessAssignmentCost(info);
                }

                #if DEBUG
                // Reset iteration in debugger after additional checks
                if (AppConfig.DebugCheckAndLogOperations) {
                    SaDebugger.ResetIteration(info);
                }
                #endif
            }

            stopwatch.Stop();
            float saDuration = stopwatch.ElapsedMilliseconds / 1000f;
            float saSpeed = SaConfig.SaIterationCount / saDuration;
            Console.WriteLine("SA finished {0} iterations in {1} s  |  Speed: {2} iterations/s", ParseHelper.LargeNumToString(info.IterationNum), ParseHelper.ToString(saDuration), ParseHelper.LargeNumToString(saSpeed));

            // Get Pareto-optimal front
            List<SaInfo> paretoFront = GetParetoFront(bestInfoBySatisfaction);

            // Perform all output
            PerformOutput(paretoFront);

            return paretoFront;
        }

        static void PerformOutput(List<SaInfo> paretoFront) {
            // Log summary to console
            using (StreamWriter consoleStreamWriter = new StreamWriter(Console.OpenStandardOutput())) {
                LogSummaryToStream(paretoFront, consoleStreamWriter);
            }

            // Create output subfolder
            string dateStr = DateTime.Now.ToString("yyyy-MM-dd-HH-mm");
            string outputSubfolderPath = Path.Combine(AppConfig.OutputFolder, dateStr);
            Directory.CreateDirectory(outputSubfolderPath);

            // Log summary to file
            using (StreamWriter summaryFileStreamWriter = new StreamWriter(Path.Combine(outputSubfolderPath, "summary.txt"))) {
                LogSummaryToStream(paretoFront, summaryFileStreamWriter);
            }

            // Log pareto front solutions to separate JSON files
            for (int i = 0; i < paretoFront.Count; i++) {
                SaInfo paretoPoint = paretoFront[i];
                paretoPoint.ProcessDriverPaths();
                TotalCostCalculator.ProcessAssignmentCost(paretoPoint);
                JsonHelper.ExportSolutionJson(outputSubfolderPath, paretoPoint);
            }
        }

        static void LogSummaryToStream(List<SaInfo> paretoFront, StreamWriter streamWriter) {
            if (paretoFront.Count == 0) {
                streamWriter.WriteLine("SA found no valid solution");
            } else {
                streamWriter.WriteLine("Pareto-optimal front: {0}", ParetoFrontToString(paretoFront));

                for (int i = 0; i < paretoFront.Count; i++) {
                    SaInfo paretoPoint = paretoFront[i];
                    streamWriter.WriteLine("\nPoint {0}\n{1}", ParetoPointToString(paretoPoint), ParseHelper.AssignmentToString(paretoPoint));
                }
            }
        }

        static double GetAdjustedCost(double cost, double satisfaction, float satisfactionFactor) {
            return cost * (1 + (1 - satisfaction) * satisfactionFactor);
        }

        void LogIteration(Stopwatch stopwatch) {
            // Get Pareto-optimal front
            List<SaInfo> paretoFront = GetParetoFront(bestInfoBySatisfaction);
            string paretoFrontStr;
            if (paretoFront.Count == 0) {
                paretoFrontStr = "No valid solutions";
            } else {
                paretoFrontStr = "Front: " + ParetoFrontToString(paretoFront);
            }

            float saDuration = stopwatch.ElapsedMilliseconds / 1000f;
            string speedStr = saDuration > 1 ? ParseHelper.LargeNumToString(info.IterationNum / saDuration, "0") + "/s" : "-";

            string lastImprovementIterationStr = info.LastImprovementIteration.HasValue ? ParseHelper.LargeNumToString(info.LastImprovementIteration.Value, "0") : "-";
            string hasImprovementStr = info.HasImprovementSinceLog ? " !!!" : "";

            double logCost = info.TotalInfo.Stats.RawCost + info.TotalInfo.Stats.Robustness;

            // Log basic info
            Console.WriteLine("# {0,4}    Last.impr: {1,4}    Speed: {2,6}    Cycle: {3,3}    Cost: {4,6} ({5,2}%)    Raw: {6,6}    Temp: {7,4}    Sat.f: {8,4}   Penalty: {9,-33}    {10}{11}", ParseHelper.LargeNumToString(info.IterationNum), lastImprovementIterationStr, speedStr, info.CycleNum, ParseHelper.LargeNumToString(logCost, "0.0"), ParseHelper.ToString(info.TotalInfo.Stats.SatisfactionScore.Value * 100, "0"), ParseHelper.LargeNumToString(info.TotalInfo.Stats.RawCost, "0.0"), ParseHelper.ToString(info.Temperature, "0"), ParseHelper.ToString(info.SatisfactionFactor, "0.00"), ParseHelper.GetPenaltyString(info.TotalInfo), paretoFrontStr, hasImprovementStr);

            if (AppConfig.DebugSaLogAdditionalInfo) {
                Console.WriteLine("Worked times: {0}", ParseHelper.ToString(info.DriverInfos.Select(driverInfo => driverInfo.WorkedTime).ToArray()));
                Console.WriteLine("Contract time factors: {0}", ParseHelper.ToString(info.Instance.InternalDrivers.Select(driver => (double)info.DriverInfos[driver.AllDriversIndex].WorkedTime / driver.ContractTime).ToArray()));
                Console.WriteLine("Shift counts: {0}", ParseHelper.ToString(info.DriverInfos.Select(driverInfo => driverInfo.ShiftCount).ToArray()));
                Console.WriteLine("External type shift counts: {0}", ParseHelper.ToString(info.ExternalDriverTypeInfos.Select(externalDriverTypeInfo => externalDriverTypeInfo.ExternalShiftCount).ToArray()));
            }

            if (AppConfig.DebugSaLogCurrentSolution) {
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
                if (bestInfoOfLevel.TotalInfo.Stats.Cost == double.MaxValue) continue;

                if (bestOfPrevLevel == null || bestInfoOfLevel.TotalInfo.Stats.Cost < bestOfPrevLevel.TotalInfo.Stats.Cost - SaConfig.ParetoFrontMinCostDiff) {
                    paretoFront.Add(bestInfoOfLevel);
                    bestOfPrevLevel = bestInfoOfLevel;
                }
            }
            paretoFront.Reverse();
            return paretoFront;
        }

        static string ParetoFrontToString(List<SaInfo> paretoFront) {
            return string.Join(" | ", paretoFront.Select(paretoPoint => ParetoPointToString(paretoPoint)));
        }

        static string ParetoPointToString(SaInfo paretoPoint) {
            return string.Format("{0}% {1}", ParseHelper.ToString(paretoPoint.TotalInfo.Stats.SatisfactionScore.Value * MiscConfig.PercentageFactor, "0"), ParseHelper.LargeNumToString(paretoPoint.TotalInfo.Stats.Cost, "0"));
        }

        Driver[] GetInitialAssignment() {
            Driver[] assignment = new Driver[info.Instance.Trips.Length];
            List<Trip>[] driverPaths = new List<Trip>[info.Instance.AllDrivers.Length];
            for (int i = 0; i < driverPaths.Length; i++) driverPaths[i] = new List<Trip>();

            for (int tripIndex = 0; tripIndex < info.Instance.Trips.Length; tripIndex++) {
                Trip trip = info.Instance.Trips[tripIndex];

                // Greedily assign to random internal driver, avoiding precedence violations
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

                // Greedily assign to random external driver, avoiding precedence violations
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
            }

            return assignment;
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

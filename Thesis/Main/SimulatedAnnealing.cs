using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Thesis {
    class SimulatedAnnealing {
        readonly SaInfo info;
        readonly SaInfo[] bestInfoBySatisfaction;
        readonly XorShiftRandom rand;
        readonly Action threadCallback;
        readonly CancellationToken cancellationToken;
        int[] debugOperationCounts, debugAcceptedOperationCounts;

        public SimulatedAnnealing(Instance instance, XorShiftRandom rand, SaInfo[] bestInfoBySatisfaction, Action threadCallback, CancellationToken cancellationToken) {
            this.rand = rand;
            this.bestInfoBySatisfaction = bestInfoBySatisfaction;
            this.threadCallback = threadCallback;
            this.cancellationToken = cancellationToken;

            // Initialise info
            info = new SaInfo(instance);
            info.Temperature = SaConfig.SaInitialTemperature;
            info.SatisfactionFactor = (float)this.rand.NextDouble(SaConfig.SaCycleMinSatisfactionFactor, SaConfig.SaCycleMaxSatisfactionFactor);
            info.IsHotelStayAfterActivity = new bool[instance.Activities.Length];

            // Initialise best info
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
            if (AppConfig.DebugCheckOperations) {
                SaDebugger.ResetIteration(info);
                SaDebugger.GetCurrentOperation().StartPart("Initial assignment", null);
                SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.OldChecked);
            }
            #endif

            // Get cost of initial assignment
            TotalCostCalculator.ProcessAssignmentCost(info);

            #if DEBUG
            // Reset iteration in debugger after initial assignment cost
            if (AppConfig.DebugCheckOperations) {
                SaDebugger.ResetIteration(info);
            }
            #endif
        }

        public void Run() {
            #if DEBUG
            if (AppConfig.DebugSaLogOperationStats) {
                debugOperationCounts = new int[4];
                debugAcceptedOperationCounts = new int[4];
            }
            #endif

            while (!cancellationToken.IsCancellationRequested) {
                // Pick a random operation based on the configured probabilities
                double operationDouble = rand.NextDouble();
                AbstractOperation operation;
                if (operationDouble < SaConfig.AssignInternalProbCumulative) operation = AssignInternalOperation.CreateRandom(info, rand);
                else if (operationDouble < SaConfig.AssignExternalProbCumulative) operation = AssignExternalOperation.CreateRandom(info, rand);
                else if (operationDouble < SaConfig.SwapProbCumulative) operation = SwapOperation.CreateRandom(info, rand);
                else operation = ToggleHotelOperation.CreateRandom(info, rand);

                #if DEBUG
                int operationTypeIndex;
                if (AppConfig.DebugSaLogOperationStats) {
                    if (operationDouble < SaConfig.AssignInternalProbCumulative) operationTypeIndex = 0;
                    else if (operationDouble < SaConfig.AssignExternalProbCumulative) operationTypeIndex = 1;
                    else if (operationDouble < SaConfig.SwapProbCumulative) operationTypeIndex = 2;
                    else operationTypeIndex = 3;
                    debugOperationCounts[operationTypeIndex]++;
                }
                #endif

                SaTotalInfo totalInfoDiff = operation.GetCostDiff();
                double oldAdjustedCost = GetAdjustedCost(info.TotalInfo.Stats.Cost, info.TotalInfo.Stats.SatisfactionScore.Value, info.SatisfactionFactor);
                double newAdjustedCost = GetAdjustedCost(info.TotalInfo.Stats.Cost + totalInfoDiff.Stats.Cost, info.TotalInfo.Stats.SatisfactionScore.Value + totalInfoDiff.Stats.SatisfactionScore.Value, info.SatisfactionFactor);
                double adjustedCostDiff = newAdjustedCost - oldAdjustedCost;

                bool isAccepted = adjustedCostDiff < 0 || rand.NextDouble() < Math.Exp(-adjustedCostDiff / info.Temperature);
                if (isAccepted) {
                    operation.Execute();

                    #if DEBUG
                    if (AppConfig.DebugSaLogOperationStats) {
                        debugAcceptedOperationCounts[operationTypeIndex]++;
                    }
                    #endif

                    int satisfactionLevel = (int)Math.Round(info.TotalInfo.Stats.SatisfactionScore.Value * MiscConfig.PercentageFactor);
                    if (info.TotalInfo.Stats.Penalty < 0.01 && info.TotalInfo.Stats.Cost < bestInfoBySatisfaction[satisfactionLevel].TotalInfo.Stats.Cost) {
                        info.LastImprovementIteration = info.IterationNum;
                        info.HasImprovementSinceLog = true;

                        // Check cost to remove floating point imprecisions
                        TotalCostCalculator.ProcessAssignmentCost(info);

                        #if DEBUG
                        if (AppConfig.DebugCheckOperations) {
                            if (info.TotalInfo.Stats.Penalty > 0.01) throw new Exception("New best solution is invalid");
                            if (Math.Abs(info.TotalInfo.Stats.SatisfactionScore.Value * MiscConfig.PercentageFactor - satisfactionLevel) > 0.6) throw new Exception("New best solution has incorrect satisfaction level");
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
                if (AppConfig.DebugCheckOperations) {
                    SaDebugger.NextIteration(info);
                }
                #endif

                // Callback
                if (info.IterationNum % SaConfig.SaThreadCallbackFrequency == 0) {
                    threadCallback();
                }

                // Update temperature and penalty factor
                if (info.IterationNum % SaConfig.SaParameterUpdateFrequency == 0) {
                    info.Temperature *= SaConfig.SaTemperatureReductionFactor;

                    // Check if we should end the cycle
                    if (info.Temperature <= SaConfig.SaEndCycleTemperature) {
                        info.CycleNum++;
                        info.Temperature = (float)rand.NextDouble(SaConfig.SaCycleMinInitialTemperature, SaConfig.SaCycleMaxInitialTemperature);
                        info.SatisfactionFactor = (float)rand.NextDouble(SaConfig.SaCycleMinSatisfactionFactor, SaConfig.SaCycleMaxSatisfactionFactor);
                    }

                    TotalCostCalculator.ProcessAssignmentCost(info);
                }

                #if DEBUG
                // Reset iteration in debugger after additional checks
                if (AppConfig.DebugCheckOperations) {
                    SaDebugger.ResetIteration(info);
                }
                #endif
            }
        }

        public static double GetAdjustedCost(double cost, double satisfaction, float satisfactionFactor) {
            return cost * (1 + (1 - satisfaction) * satisfactionFactor);
        }

        Driver[] GetInitialAssignment() {
            Driver[] assignment = new Driver[info.Instance.Activities.Length];
            List<Activity>[] driverPaths = new List<Activity>[info.Instance.AllDrivers.Length];
            for (int i = 0; i < driverPaths.Length; i++) driverPaths[i] = new List<Activity>();

            for (int activityIndex = 0; activityIndex < info.Instance.Activities.Length; activityIndex++) {
                Activity activity = info.Instance.Activities[activityIndex];

                // Greedily assign to random internal driver, avoiding overlap violations
                InternalDriver[] internalDriversRandomOrder = Copy(info.Instance.InternalDrivers);
                Shuffle(internalDriversRandomOrder);
                bool isDone = false;
                for (int shuffledInternalDriverIndex = 0; shuffledInternalDriverIndex < internalDriversRandomOrder.Length; shuffledInternalDriverIndex++) {
                    InternalDriver internalDriver = internalDriversRandomOrder[shuffledInternalDriverIndex];
                    List<Activity> driverPath = driverPaths[internalDriver.AllDriversIndex];

                    if (driverPath.Count == 0 || info.Instance.IsValidSuccession(driverPath[^1], activity)) {
                        // We can add this activity to this driver without overlap violations
                        assignment[activityIndex] = internalDriver;
                        driverPath.Add(activity);
                        isDone = true;
                        break;
                    }
                }
                if (isDone) continue;

                // Greedily assign to random external driver, avoiding overlap violations
                ExternalDriver[][] externalDriverTypesRandomOrder = Copy(info.Instance.ExternalDriversByType);
                Shuffle(externalDriverTypesRandomOrder);
                for (int shuffledExternalDriverTypeIndex = 0; shuffledExternalDriverTypeIndex < externalDriverTypesRandomOrder.Length; shuffledExternalDriverTypeIndex++) {
                    ExternalDriver[] externalDriversInType = externalDriverTypesRandomOrder[shuffledExternalDriverTypeIndex];

                    // Assign to first possible driver in type
                    for (int externalDriverIndexInType = 0; externalDriverIndexInType < externalDriversInType.Length; externalDriverIndexInType++) {
                        ExternalDriver externalDriver = externalDriversInType[externalDriverIndexInType];
                        List<Activity> driverPath = driverPaths[externalDriver.AllDriversIndex];

                        if (driverPath.Count == 0 || info.Instance.IsValidSuccession(driverPath[^1], activity)) {
                            // We can add this activity to this driver without overlap violations
                            assignment[activityIndex] = externalDriver;
                            driverPath.Add(activity);
                            isDone = true;
                            break;
                        }
                    }
                    if (isDone) break;
                }
                if (isDone) continue;

                // Assigning without overlap violations is impossible, so assign to random external driver
                int randomExternalDriverTypeIndex = rand.Next(info.Instance.ExternalDriversByType.Length);
                ExternalDriver[] externalDriversInRandomType = info.Instance.ExternalDriversByType[randomExternalDriverTypeIndex];
                int randomExternalDriverIndexInType = rand.Next(externalDriversInRandomType.Length);
                ExternalDriver randomExternalDriver = externalDriversInRandomType[randomExternalDriverIndexInType];
                List<Activity> randomDriverPath = driverPaths[randomExternalDriver.AllDriversIndex];
                assignment[activityIndex] = randomExternalDriver;
                randomDriverPath.Add(activity);
            }

            return assignment;
        }

        void Shuffle<T>(T[] array) {
            // Fisher–Yates shuffle
            int n = array.Length;
            while (n > 1) {
                int k = rand.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }

        static T[] Copy<T>(T[] array) {
            T[] copy = new T[array.Length];
            for (int i = 0; i < array.Length; i++) {
                copy[i] = array[i];
            }
            return copy;
        }





        /* Obsolete */

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
            Console.WriteLine("# {0,4}    Last.impr: {1,4}    Speed: {2,6}    Cycle: {3,2}    Cost: {4,6} ({5,2}%)    Raw: {6,6}    Temp: {7,5}    Sat.f: {8,4}   Penalty: {9,-33}    {10}{11}", ParseHelper.LargeNumToString(info.IterationNum), lastImprovementIterationStr, speedStr, info.CycleNum, ParseHelper.LargeNumToString(logCost, "0.0"), ParseHelper.ToString(info.TotalInfo.Stats.SatisfactionScore.Value * 100, "0"), ParseHelper.LargeNumToString(info.TotalInfo.Stats.RawCost, "0.0"), ParseHelper.LargeNumToString(info.Temperature, "0.0"), ParseHelper.ToString(info.SatisfactionFactor, "0.00"), ParseHelper.GetPenaltyString(info.TotalInfo), paretoFrontStr, hasImprovementStr);

            if (AppConfig.DebugSaLogAdditionalInfo) {
                Console.WriteLine("Worked times: {0}", ParseHelper.ToString(info.DriverInfos.Select(driverInfo => driverInfo.WorkedTime).ToArray()));
                Console.WriteLine("Contract time factors: {0}", ParseHelper.ToString(info.Instance.InternalDrivers.Select(driver => (double)info.DriverInfos[driver.AllDriversIndex].WorkedTime / driver.ContractTime).ToArray()));
                Console.WriteLine("Shift counts: {0}", ParseHelper.ToString(info.DriverInfos.Select(driverInfo => driverInfo.ShiftCount).ToArray()));
                Console.WriteLine("External type shift counts: {0}", ParseHelper.ToString(info.ExternalDriverTypeInfos.Select(externalDriverTypeInfo => externalDriverTypeInfo.ExternalShiftCount).ToArray()));
            }

            if (AppConfig.DebugSaLogCurrentSolution) {
                Console.WriteLine("Current solution: {0}", ParseHelper.AssignmentToString(info));
            }

            #if DEBUG
            if (AppConfig.DebugSaLogOperationStats) {
                string[] operationNames = new string[] { "Assign internal", "Assign external", "Swap", "Toggle hotel" };
                for (int i = 0; i < debugOperationCounts.Length; i++) {
                    string acceptancePercentageStr = debugOperationCounts[i] == 0 ? "0" : ParseHelper.ToString(100f * debugAcceptedOperationCounts[i] / debugOperationCounts[i], "0");
                    Console.Write("{0}: {1}/{2} ({3}%)", operationNames[i], ParseHelper.LargeNumToString(debugAcceptedOperationCounts[i]), ParseHelper.LargeNumToString(debugOperationCounts[i]), acceptancePercentageStr);
                    if (i + 1 < debugOperationCounts.Length) Console.Write("  |  ");
                }
                Console.WriteLine();
            }
            #endif

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
    }
}

/*
 * Runs a single thread of the simulated annealing algorithm
*/

using DriverPlannerShared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DriverPlannerAlgorithm {
    class AlgorithmThread {
        public SaInfo Info;
        public readonly SaInfo[] BestInfoBySatisfaction;
        readonly XorShiftRandom rand;
        readonly Action threadCallback;
        public int[] DebugOperationCounts, DebugAcceptedOperationCounts;

        public AlgorithmThread(Instance instance, XorShiftRandom rand, Action threadCallback) {
            this.rand = rand;
            this.threadCallback = threadCallback;

            // Initialise info with instance
            Info = new SaInfo(instance);

            // Initialise best info
            BestInfoBySatisfaction = new SaInfo[DevConfig.PercentageFactor];
            for (int i = 0; i < BestInfoBySatisfaction.Length; i++) {
                SaInfo initialBestInfo = new SaInfo(instance);
                initialBestInfo.TotalInfo = new SaTotalInfo() {
                    Stats = new SaStats() {
                        Cost = double.MaxValue,
                        SatisfactionScore = -1,
                    },
                };
                BestInfoBySatisfaction[i] = initialBestInfo;
            }
        }

        public void Run(CancellationToken cancellationToken) {
            // Initialise
            PerformFullReset();

            while (!cancellationToken.IsCancellationRequested) {
                // Pick a random operation based on the configured probabilities
                double operationDouble = rand.NextDouble();
                AbstractOperation operation;
                if (operationDouble < AlgorithmConfig.AssignInternalProbCumulative) operation = AssignInternalOperation.CreateRandom(Info, rand);
                else if (operationDouble < AlgorithmConfig.AssignExternalProbCumulative) operation = AssignExternalOperation.CreateRandom(Info, rand);
                else if (operationDouble < AlgorithmConfig.SwapProbCumulative) operation = SwapOperation.CreateRandom(Info, rand);
                else operation = ToggleHotelOperation.CreateRandom(Info, rand);

                // Determine cost difference of executing operation
                SaTotalInfo totalInfoDiff = operation.GetCostDiff();
                double oldAdjustedCost = GetAdjustedCost(Info.TotalInfo.Stats.Cost, Info.TotalInfo.Stats.SatisfactionScore.Value, Info.SatisfactionFactor);
                double newAdjustedCost = GetAdjustedCost(Info.TotalInfo.Stats.Cost + totalInfoDiff.Stats.Cost, Info.TotalInfo.Stats.SatisfactionScore.Value + totalInfoDiff.Stats.SatisfactionScore.Value, Info.SatisfactionFactor);
                double adjustedCostDiff = newAdjustedCost - oldAdjustedCost;

                // Execute operation according to the principles of simulated annealing: always if the cost decreases, or with a probability of e^(-diff / temp) otherwise
                bool isAccepted = adjustedCostDiff < 0 || rand.NextDouble() < Math.Exp(-adjustedCostDiff / Info.Temperature);
                if (isAccepted) {
                    ExecuteOperation(operation);
                }

                // Update parameters to move to the next iteration
                NextIteration();
            }
        }

        void ExecuteOperation(AbstractOperation operation) {
            operation.Execute();

            int satisfactionLevel = (int)Math.Round(Info.TotalInfo.Stats.SatisfactionScore.Value * DevConfig.PercentageFactor);
            if (Info.TotalInfo.Stats.Penalty < 0.01) {
                Info.HasHadFeasibleSolutionInCycle = true;

                if (Info.TotalInfo.Stats.Cost < BestInfoBySatisfaction[satisfactionLevel].TotalInfo.Stats.Cost) {
                    Info.LastImprovementIteration = Info.IterationNum;
                    Info.HasImprovementSinceLog = true;

                    // Check cost to remove floating point imprecisions
                    TotalCostCalculator.ProcessAssignmentCost(Info);

#if DEBUG
                    if (DevConfig.DebugCheckOperations) {
                        if (Info.TotalInfo.Stats.Penalty > 0.01) throw new Exception("New best solution is invalid");
                        if (Math.Abs(Info.TotalInfo.Stats.SatisfactionScore.Value * DevConfig.PercentageFactor - satisfactionLevel) > 0.6) throw new Exception("New best solution has incorrect satisfaction level");
                    }
#endif

                    // Store as the best solution for this satisfaction level
                    SaInfo bestInfo = Info.CopyForBestInfo();
                    BestInfoBySatisfaction[satisfactionLevel] = bestInfo;

                    // Check if this solution also improves on best solutions for lower satisfaction levels
                    for (int searchSatisfactionLevel = satisfactionLevel - 1; searchSatisfactionLevel >= 0; searchSatisfactionLevel--) {
                        if (Info.TotalInfo.Stats.Cost < BestInfoBySatisfaction[searchSatisfactionLevel].TotalInfo.Stats.Cost) {
                            BestInfoBySatisfaction[searchSatisfactionLevel] = bestInfo;
                        }
                    }
                }
            }
        }

        void NextIteration() {
            // Update iteration number
            Info.IterationNum++;

#if DEBUG
            // Set debugger to next iteration
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.NextIteration(Info);
            }
#endif

            // Callback
            if (Info.IterationNum % AlgorithmConfig.ThreadCallbackFrequency == 0) {
                threadCallback();
            }

            // Update temperature and penalty factor
            if (Info.IterationNum % AlgorithmConfig.TemperatureReductionFrequency == 0) {
                Info.Temperature *= AlgorithmConfig.TemperatureReductionFactor;

                // Check if we should end the cycle, either normally or early
                if (!Info.HasHadFeasibleSolutionInCycle && Info.Temperature <= AlgorithmConfig.EarlyEndCycleTemperature || Info.Temperature <= AlgorithmConfig.EndCycleTemperature) {
                    Info.HasHadFeasibleSolutionInCycle = false;

                    // Check if we should do a full reset
                    if (rand.NextDouble() < AlgorithmConfig.FullResetProb) {
                        // Full reset
                        PerformFullReset();
                    } else {
                        // Partial reset
                        Info.CycleNum++;
                        Info.SatisfactionFactor = (float)rand.NextDouble(AlgorithmConfig.CycleMinSatisfactionFactor, AlgorithmConfig.CycleMaxSatisfactionFactor);
                        Info.Temperature = (float)rand.NextDouble(AlgorithmConfig.CycleMinInitialTemperature, AlgorithmConfig.CycleMaxInitialTemperature);
                    }
                }

                TotalCostCalculator.ProcessAssignmentCost(Info);
            }

#if DEBUG
            // Reset iteration in debugger after additional checks
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.ResetIteration(Info);
            }
#endif
        }

        void PerformFullReset() {
            int oldCycleNum = Info == null ? 0 : Info.CycleNum;

            // Initialise info
            Info = new SaInfo(Info.Instance);
            Info.CycleNum = oldCycleNum + 1;
            Info.Temperature = AlgorithmConfig.InitialTemperature;
            Info.SatisfactionFactor = (float)rand.NextDouble(AlgorithmConfig.CycleMinSatisfactionFactor, AlgorithmConfig.CycleMaxSatisfactionFactor);
            Info.IsHotelStayAfterActivity = new bool[Info.Instance.Activities.Length];

            // Create a random initial assignment
            Info.Assignment = GenerateInitialAssignment();
            Info.ProcessDriverPaths();

#if DEBUG
            // Initialise debugger
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.ResetIteration(Info);
                SaDebugger.GetCurrentOperation().StartPart("Initial assignment", null);
                SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.OldChecked);
            }
#endif

            // Get cost of initial assignment
            TotalCostCalculator.ProcessAssignmentCost(Info);

#if DEBUG
            // Reset iteration in debugger after initial assignment cost
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.ResetIteration(Info);
            }
#endif
        }

        Driver[] GenerateInitialAssignment() {
            Driver[] assignment = new Driver[Info.Instance.Activities.Length];
            List<Activity>[] driverPaths = new List<Activity>[Info.Instance.AllDrivers.Length];
            for (int i = 0; i < driverPaths.Length; i++) driverPaths[i] = new List<Activity>();

            for (int activityIndex = 0; activityIndex < Info.Instance.Activities.Length; activityIndex++) {
                Activity activity = Info.Instance.Activities[activityIndex];

                // Greedily assign to random internal driver, avoiding overlap violations
                InternalDriver[] internalDriversRandomOrder = Copy(Info.Instance.InternalDrivers);
                Shuffle(internalDriversRandomOrder);
                bool isDone = false;
                for (int shuffledInternalDriverIndex = 0; shuffledInternalDriverIndex < internalDriversRandomOrder.Length; shuffledInternalDriverIndex++) {
                    InternalDriver internalDriver = internalDriversRandomOrder[shuffledInternalDriverIndex];
                    List<Activity> driverPath = driverPaths[internalDriver.AllDriversIndex];

                    if (driverPath.Count == 0 || Info.Instance.IsValidSuccession(driverPath.Last(), activity)) {
                        // We can add this activity to this driver without overlap violations
                        assignment[activityIndex] = internalDriver;
                        driverPath.Add(activity);
                        isDone = true;
                        break;
                    }
                }
                if (isDone) continue;

                // Greedily assign to random external driver, avoiding overlap violations
                ExternalDriver[][] externalDriverTypesRandomOrder = Copy(Info.Instance.ExternalDriversByType);
                Shuffle(externalDriverTypesRandomOrder);
                for (int shuffledExternalDriverTypeIndex = 0; shuffledExternalDriverTypeIndex < externalDriverTypesRandomOrder.Length; shuffledExternalDriverTypeIndex++) {
                    ExternalDriver[] externalDriversInType = externalDriverTypesRandomOrder[shuffledExternalDriverTypeIndex];

                    // Assign to first possible driver in type
                    for (int externalDriverIndexInType = 0; externalDriverIndexInType < externalDriversInType.Length; externalDriverIndexInType++) {
                        ExternalDriver externalDriver = externalDriversInType[externalDriverIndexInType];
                        List<Activity> driverPath = driverPaths[externalDriver.AllDriversIndex];

                        if (driverPath.Count == 0 || Info.Instance.IsValidSuccession(driverPath.Last(), activity)) {
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
                int randomExternalDriverTypeIndex = rand.Next(Info.Instance.ExternalDriversByType.Length);
                ExternalDriver[] externalDriversInRandomType = Info.Instance.ExternalDriversByType[randomExternalDriverTypeIndex];
                int randomExternalDriverIndexInType = rand.Next(externalDriversInRandomType.Length);
                ExternalDriver randomExternalDriver = externalDriversInRandomType[randomExternalDriverIndexInType];
                List<Activity> randomDriverPath = driverPaths[randomExternalDriver.AllDriversIndex];
                assignment[activityIndex] = randomExternalDriver;
                randomDriverPath.Add(activity);
            }

            return assignment;
        }

        public static double GetAdjustedCost(double cost, double satisfaction, float satisfactionFactor) {
            return cost * (1 + (1 - satisfaction) * satisfactionFactor);
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
    }
}

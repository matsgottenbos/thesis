using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Thesis {
    class SaMultithreadHandler {
        readonly SimulatedAnnealing[] saThreads;
        int totalIterationCount, totalIterationCountSinceLastLog;
        int? lastImprovementTotalIterationCount;
        string prevParetoFrontStr;
        readonly List<List<SaInfo>> paretoFrontsOverTime;
        readonly Stopwatch stopwatch;
        CancellationTokenSource[] threadCancellationTokens;

        public SaMultithreadHandler() {
            stopwatch = new Stopwatch();

            paretoFrontsOverTime = new List<List<SaInfo>>();

            int actualThreadCount = AppConfig.EnableMultithreading ? AppConfig.ThreadCount : 1;
            saThreads = new SimulatedAnnealing[actualThreadCount];
            threadCancellationTokens = new CancellationTokenSource[actualThreadCount];
        }

        public void Run(Instance instance, XorShiftRandom appRand) {
            Console.WriteLine("Starting simulated annealing");

            // Start stopwatch
            stopwatch.Start();

            if (!AppConfig.EnableMultithreading) {
                SimulatedAnnealing simulatedAnnealing = new SimulatedAnnealing(instance, appRand, HandleThreadCallback);
                saThreads[0] = simulatedAnnealing;

                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = cancellationTokenSource.Token;
                simulatedAnnealing.Run(cancellationToken);
                threadCancellationTokens[0] = cancellationTokenSource;
                return;
            }

            ManualResetEvent[] handles = new ManualResetEvent[AppConfig.ThreadCount];
            for (int threadIndex = 0; threadIndex < AppConfig.ThreadCount; threadIndex++) {
                ulong seed = appRand.NextUInt64();
                XorShiftRandom saRand = new XorShiftRandom(seed);

                SimulatedAnnealing simulatedAnnealing = new SimulatedAnnealing(instance, saRand, HandleThreadCallback);
                saThreads[threadIndex] = simulatedAnnealing;

                (CancellationTokenSource cts, ManualResetEvent handle) = ThreadHandler.ExecuteInThreadWithCancellation(saRand, (CancellationToken cancellationToken, XorShiftRandom threadRand) => {
                    simulatedAnnealing.Run(cancellationToken);
                });

                threadCancellationTokens[threadIndex] = cts;
                handles[threadIndex] = handle;
            }

            // Wait for the SA threads to exit
            WaitHandle.WaitAll(handles);

            stopwatch.Stop();
            float saDuration = stopwatch.ElapsedMilliseconds / 1000f;
            float saSpeed = SaConfig.SaIterationCount / saDuration;
            Console.WriteLine("SA finished {0} iterations in {1} s  |  Speed: {2} iterations/s", ParseHelper.LargeNumToString(totalIterationCount), ParseHelper.ToString(saDuration), ParseHelper.LargeNumToString(saSpeed));

            // Get Pareto-optimal front
            List<SaInfo> paretoFront = GetCombinedParetoFront();

            // Perform all output
            WriteOutputToFiles(paretoFront, paretoFrontsOverTime);
        }

        void HandleThreadCallback() {
            totalIterationCount += SaConfig.SaThreadCallbackFrequency;
            totalIterationCountSinceLastLog += SaConfig.SaThreadCallbackFrequency;

            if (totalIterationCount >= SaConfig.SaIterationCount) {
                for (int i = 0; i < threadCancellationTokens.Length; i++) {
                    threadCancellationTokens[i].Cancel();
                }
                return;
            }

            if (totalIterationCountSinceLastLog >= SaConfig.SaLogFrequency) {
                totalIterationCountSinceLastLog = 0;
                PerformLog();
            }
        }

        void PerformLog() {
            float saDuration = stopwatch.ElapsedMilliseconds / 1000f;
            string speedStr = saDuration > 1 ? ParseHelper.LargeNumToString(totalIterationCount / saDuration, "0") + "/s" : "-";

            List<SaInfo> paretoFront = GetCombinedParetoFront();
            paretoFrontsOverTime.Add(paretoFront);

            string paretoFrontStr;
            bool hasImprovementSinceLastLog;
            if (paretoFront.Count == 0) {
                paretoFrontStr = "No valid solutions";
                hasImprovementSinceLastLog = false;
            } else {
                paretoFrontStr = "Solutions: " + ParetoFrontToString(paretoFront);
                hasImprovementSinceLastLog = paretoFrontStr != prevParetoFrontStr;
            }

            if (hasImprovementSinceLastLog) {
                lastImprovementTotalIterationCount = totalIterationCount;
            }
            string lastImprovementIterationStr = lastImprovementTotalIterationCount.HasValue ? ParseHelper.LargeNumToString(lastImprovementTotalIterationCount.Value, "0") : "-";
            string hasImprovementStr = hasImprovementSinceLastLog ? " !!!" : "";

            Console.WriteLine("# {0,4}    Speed: {1,6}    Last impr.: {2,4}    {3}{4}", ParseHelper.LargeNumToString(totalIterationCount), speedStr, lastImprovementIterationStr, paretoFrontStr, hasImprovementStr);

            prevParetoFrontStr = paretoFrontStr;

            if (AppConfig.DebugSaLogThreads) {
                for (int threadIndex = 0; threadIndex < saThreads.Length; threadIndex++) {
                    PeformSaDebugLog(saThreads[threadIndex], threadIndex);
                }
            }
        }

        List<SaInfo> GetCombinedParetoFront() {
            List<SaInfo> paretoFront = new List<SaInfo>();
            SaInfo bestOfPrevLevel = null;
            for (int satisfactionLevel = MiscConfig.PercentageFactor - 1; satisfactionLevel >= 0; satisfactionLevel--) {
                // Get best info of all threads for this satisfaction level
                SaInfo bestInfoOfLevel = saThreads[0].BestInfoBySatisfaction[satisfactionLevel];
                for (int threadIndex = 1; threadIndex < saThreads.Length; threadIndex++) {
                    SaInfo threadBestInfoOfLevel = saThreads[threadIndex].BestInfoBySatisfaction[satisfactionLevel];
                    if (threadBestInfoOfLevel.TotalInfo.Stats.Cost < bestInfoOfLevel.TotalInfo.Stats.Cost) {
                        bestInfoOfLevel = threadBestInfoOfLevel;
                    }
                }

                if (bestInfoOfLevel.TotalInfo.Stats.Cost == double.MaxValue) continue;

                if (bestOfPrevLevel == null || bestInfoOfLevel.TotalInfo.Stats.Cost < bestOfPrevLevel.TotalInfo.Stats.Cost - SaConfig.ParetoFrontMinCostDiff) {
                    paretoFront.Add(bestInfoOfLevel);
                    bestOfPrevLevel = bestInfoOfLevel;
                }
            }
            paretoFront.Reverse();
            return paretoFront;
        }

        static string ParetoFrontToString(List<SaInfo> paretoFrontInfos) {
            return string.Join(" | ", paretoFrontInfos.Select(paretoPoint => ParetoPointToString(paretoPoint)));
        }

        static string ParetoPointToString(SaInfo paretoPointInfo) {
            return string.Format("{0}% {1}", ParseHelper.ToString(paretoPointInfo.TotalInfo.Stats.SatisfactionScore.Value * MiscConfig.PercentageFactor, "0"), ParseHelper.LargeNumToString(paretoPointInfo.TotalInfo.Stats.Cost, "0"));
        }

        static void WriteOutputToFiles(List<SaInfo> paretoFrontInfos, List<List<SaInfo>> paretoFrontsOverTime) {
            // Log summary to console
            using (StreamWriter consoleStreamWriter = new StreamWriter(Console.OpenStandardOutput())) {
                LogSummaryToStream(paretoFrontInfos, paretoFrontsOverTime, consoleStreamWriter);
            }

            // Create output subfolder
            string dateStr = DateTime.Now.ToString("yyyy-MM-dd-HH-mm");
            string outputSubfolderPath = Path.Combine(AppConfig.OutputFolder, dateStr);
            Directory.CreateDirectory(outputSubfolderPath);

            // Log summary to file
            using (StreamWriter summaryFileStreamWriter = new StreamWriter(Path.Combine(outputSubfolderPath, "summary.txt"))) {
                LogSummaryToStream(paretoFrontInfos, paretoFrontsOverTime, summaryFileStreamWriter);
            }

            // Log pareto front solutions to separate JSON files
            for (int i = 0; i < paretoFrontInfos.Count; i++) {
                SaInfo paretoPoint = paretoFrontInfos[i];
                paretoPoint.ProcessDriverPaths();
                TotalCostCalculator.ProcessAssignmentCost(paretoPoint);
                JsonAssignmentHelper.ExportAssignmentInfoJson(outputSubfolderPath, paretoPoint);
            }
        }

        static void LogSummaryToStream(List<SaInfo> paretoFront, List<List<SaInfo>> paretoFrontsOverTime, StreamWriter streamWriter) {
            if (paretoFront.Count == 0) {
                streamWriter.WriteLine("SA found no valid solution");
            } else {
                streamWriter.WriteLine("Pareto-optimal front: {0}", ParetoFrontToString(paretoFront));

                for (int i = 0; i < paretoFront.Count; i++) {
                    SaInfo paretoPoint = paretoFront[i];
                    streamWriter.WriteLine("\nPoint {0}\n{1}", ParetoPointToString(paretoPoint), ParseHelper.AssignmentToString(paretoPoint));
                }

                // Log progression of min-cost solutions
                streamWriter.WriteLine("\nMin cost progression:");
                streamWriter.WriteLine(string.Join(", ", paretoFrontsOverTime.Select(paretoFront => ParseHelper.ToString(paretoFront.Count > 0 ? paretoFront[0].TotalInfo.Stats.Cost : -1, "0"))));

                // Log progression of max-satisfaction solutions
                streamWriter.WriteLine("\nMax satisfaction progression:");
                streamWriter.WriteLine(string.Join(", ", paretoFrontsOverTime.Select(paretoFront => ParseHelper.ToString(paretoFront.Count > 0 ? paretoFront[^1].TotalInfo.Stats.SatisfactionScore.Value * MiscConfig.PercentageFactor : -1, "0.00"))));
            }
        }

        static void PeformSaDebugLog(SimulatedAnnealing saThread, int threadIndex) {
            // Get Pareto-optimal front
            List<SaInfo> paretoFront = GetSingleParetoFront(saThread.BestInfoBySatisfaction);
            string paretoFrontStr;
            if (paretoFront.Count == 0) {
                paretoFrontStr = "No valid solutions";
            } else {
                paretoFrontStr = "Front: " + ParetoFrontToString(paretoFront);
            }

            string lastImprovementIterationStr = saThread.Info.LastImprovementIteration.HasValue ? ParseHelper.LargeNumToString(saThread.Info.LastImprovementIteration.Value, "0") : "-";
            string hasImprovementStr = saThread.Info.HasImprovementSinceLog ? " !!!" : "";

            double logCost = saThread.Info.TotalInfo.Stats.RawCost + saThread.Info.TotalInfo.Stats.Robustness;

            // Log basic info
            Console.WriteLine("{0}:    # {1,4}    Last.impr: {2,4}    Cycle: {3,2}    Cost: {4,6} ({5,2}%)    Raw: {6,6}    Temp: {7,5}    Sat.f: {8,4}   Penalty: {9,-33}    {10}{11}", threadIndex, ParseHelper.LargeNumToString(saThread.Info.IterationNum), lastImprovementIterationStr, saThread.Info.CycleNum, ParseHelper.LargeNumToString(logCost, "0.0"), ParseHelper.ToString(saThread.Info.TotalInfo.Stats.SatisfactionScore.Value * 100, "0"), ParseHelper.LargeNumToString(saThread.Info.TotalInfo.Stats.RawCost, "0.0"), ParseHelper.LargeNumToString(saThread.Info.Temperature, "0.0"), ParseHelper.ToString(saThread.Info.SatisfactionFactor, "0.00"), ParseHelper.GetPenaltyString(saThread.Info.TotalInfo), paretoFrontStr, hasImprovementStr);

            if (AppConfig.DebugSaLogAdditionalInfo) {
                Console.WriteLine("Worked times: {0}", ParseHelper.ToString(saThread.Info.DriverInfos.Select(driverInfo => driverInfo.WorkedTime).ToArray()));
                Console.WriteLine("Contract time factors: {0}", ParseHelper.ToString(saThread.Info.Instance.InternalDrivers.Select(driver => (double)saThread.Info.DriverInfos[driver.AllDriversIndex].WorkedTime / driver.ContractTime).ToArray()));
                Console.WriteLine("Shift counts: {0}", ParseHelper.ToString(saThread.Info.DriverInfos.Select(driverInfo => driverInfo.ShiftCount).ToArray()));
                Console.WriteLine("External type shift counts: {0}", ParseHelper.ToString(saThread.Info.ExternalDriverTypeInfos.Select(externalDriverTypeInfo => externalDriverTypeInfo.ExternalShiftCount).ToArray()));
            }

            if (AppConfig.DebugSaLogCurrentSolution) {
                Console.WriteLine("Current solution: {0}", ParseHelper.AssignmentToString(saThread.Info));
            }

            #if DEBUG
            if (AppConfig.DebugSaLogOperationStats) {
                string[] operationNames = new string[] { "Assign internal", "Assign external", "Swap", "Toggle hotel" };
                for (int i = 0; i < saThread.DebugOperationCounts.Length; i++) {
                    string acceptancePercentageStr = saThread.DebugOperationCounts[i] == 0 ? "0" : ParseHelper.ToString(100f * saThread.DebugOperationCounts[i] / saThread.DebugOperationCounts[i], "0");
                    Console.Write("{0}: {1}/{2} ({3}%)", operationNames[i], ParseHelper.LargeNumToString(saThread.DebugOperationCounts[i]), ParseHelper.LargeNumToString(saThread.DebugOperationCounts[i]), acceptancePercentageStr);
                    if (i + 1 < saThread.DebugOperationCounts.Length) Console.Write("  |  ");
                }
                Console.WriteLine();
            }
            #endif

            saThread.Info.HasImprovementSinceLog = false;
        }

        static List<SaInfo> GetSingleParetoFront(SaInfo[] bestInfoBySatisfaction) {
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
    }
}

/*
 * Runs all threads of the simulated annealing algorithm
*/

using DriverPlannerShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace DriverPlannerAlgorithm {
    class AlgorithmMultithreadHandler {
        readonly AlgorithmThread[] saThreads;
        long totalIterationCount, totalIterationCountSinceLastLog;
        long? lastImprovementTotalIterationCount;
        string prevParetoFrontStr;
        readonly List<List<SaInfo>> paretoFrontsOverTime;
        readonly Stopwatch stopwatch;
        readonly CancellationTokenSource[] threadCancellationTokens;

        public AlgorithmMultithreadHandler() {
            stopwatch = new Stopwatch();

            paretoFrontsOverTime = new List<List<SaInfo>>();

            int actualThreadCount = DevConfig.DebugDisableMultithreading ? 1 : AppConfig.ThreadCount;
            saThreads = new AlgorithmThread[actualThreadCount];
            threadCancellationTokens = new CancellationTokenSource[actualThreadCount];
        }

        public void Run(Instance instance, XorShiftRandom appRand) {
            // Start stopwatch
            stopwatch.Start();

            if (DevConfig.DebugDisableMultithreading) {
                AlgorithmThread simulatedAnnealing = new AlgorithmThread(instance, appRand, HandleThreadCallback);
                saThreads[0] = simulatedAnnealing;

                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = cancellationTokenSource.Token;
                threadCancellationTokens[0] = cancellationTokenSource;
                simulatedAnnealing.Run(cancellationToken);
            } else {
                ManualResetEvent[] handles = new ManualResetEvent[AppConfig.ThreadCount];
                for (int threadIndex = 0; threadIndex < AppConfig.ThreadCount; threadIndex++) {
                    ulong seed = appRand.NextUInt64();
                    XorShiftRandom saRand = new XorShiftRandom(seed);

                    AlgorithmThread simulatedAnnealing = new AlgorithmThread(instance, saRand, HandleThreadCallback);
                    saThreads[threadIndex] = simulatedAnnealing;

                    (CancellationTokenSource cts, ManualResetEvent handle) = ThreadHandler.ExecuteInThreadWithCancellation(saRand, (CancellationToken cancellationToken, XorShiftRandom threadRand) => {
                        simulatedAnnealing.Run(cancellationToken);
                    });

                    threadCancellationTokens[threadIndex] = cts;
                    handles[threadIndex] = handle;
                }

                // Wait for the SA threads to exit
                WaitHandle.WaitAll(handles);
            }

            stopwatch.Stop();

            // Get Pareto-optimal front
            List<SaInfo> paretoFront = GetCombinedParetoFront();

            // Perform all output
            WriteOutputToFiles(paretoFront, paretoFrontsOverTime, AppConfig.SaIterationCount, stopwatch);
        }

        void HandleThreadCallback() {
            Interlocked.Add(ref totalIterationCount, AlgorithmConfig.ThreadCallbackFrequency);
            Interlocked.Add(ref totalIterationCountSinceLastLog, AlgorithmConfig.ThreadCallbackFrequency);

            if (totalIterationCount >= AppConfig.SaIterationCount) {
                lock (threadCancellationTokens) {
                    for (int i = 0; i < threadCancellationTokens.Length; i++) {
                        threadCancellationTokens[i].Cancel();
                    }
                }
                return;
            }

            if (totalIterationCountSinceLastLog >= AlgorithmConfig.LogFrequency) {
                totalIterationCountSinceLastLog = 0;
                PerformLog();
            }
        }

        void PerformLog() {
            float saDuration = stopwatch.ElapsedMilliseconds / 1000f;
            float speed = totalIterationCount / saDuration;
            string speedStr;
            if (saDuration <= 1) {
                speedStr = "-";
            } else if (speed > 1000000) {
                speedStr = ToStringHelper.LargeNumToString(speed, "0.000") + "/s";
            } else {
                speedStr = ToStringHelper.LargeNumToString(speed, "0") + "/s";
            }

            List<SaInfo> paretoFront = GetCombinedParetoFront();
            lock (paretoFrontsOverTime) {
                paretoFrontsOverTime.Add(paretoFront);
            }

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
            string lastImprovementIterationStr = lastImprovementTotalIterationCount.HasValue ? ToStringHelper.LargeNumToString(lastImprovementTotalIterationCount.Value, "0") : "-";
            string hasImprovementStr = hasImprovementSinceLastLog ? " !!!" : "";

            Console.WriteLine("# {0,4}    Speed: {1,6}    Last impr.: {2,4}    {3}{4}", ToStringHelper.LargeNumToString(totalIterationCount), speedStr, lastImprovementIterationStr, paretoFrontStr, hasImprovementStr);

            prevParetoFrontStr = paretoFrontStr;

            if (DevConfig.DebugLogThreads) {
                for (int threadIndex = 0; threadIndex < saThreads.Length; threadIndex++) {
                    PeformSaDebugLog(saThreads[threadIndex], threadIndex);
                }
            }
        }

        List<SaInfo> GetCombinedParetoFront() {
            List<SaInfo> paretoFront = new List<SaInfo>();
            SaInfo bestOfPrevLevel = null;
            for (int satisfactionLevel = DevConfig.PercentageFactor - 1; satisfactionLevel >= 0; satisfactionLevel--) {
                // Get best info of all threads for this satisfaction level
                SaInfo bestInfoOfLevel = saThreads[0].BestInfoBySatisfaction[satisfactionLevel];
                for (int threadIndex = 1; threadIndex < saThreads.Length; threadIndex++) {
                    SaInfo threadBestInfoOfLevel = saThreads[threadIndex].BestInfoBySatisfaction[satisfactionLevel];
                    if (threadBestInfoOfLevel.TotalInfo.Stats.Cost < bestInfoOfLevel.TotalInfo.Stats.Cost) {
                        bestInfoOfLevel = threadBestInfoOfLevel;
                    }
                }

                if (bestInfoOfLevel.TotalInfo.Stats.Cost == double.MaxValue) continue;

                if (bestOfPrevLevel == null || bestInfoOfLevel.TotalInfo.Stats.Cost < bestOfPrevLevel.TotalInfo.Stats.Cost - AlgorithmConfig.ParetoFrontMinCostDiff) {
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
            return string.Format("{0}% {1}", ToStringHelper.ToString(paretoPointInfo.TotalInfo.Stats.SatisfactionScore.Value * DevConfig.PercentageFactor, "0"), ToStringHelper.LargeNumToString(paretoPointInfo.TotalInfo.Stats.Cost, "0"));
        }

        static string FinalParetoFrontToString(List<SaInfo> paretoFrontInfos) {
            return string.Join(" | ", paretoFrontInfos.Select(paretoPoint => FinalParetoPointToString(paretoPoint)));
        }

        static string FinalParetoPointToString(SaInfo paretoPointInfo) {
            return string.Format("{0}% {1}", ToStringHelper.ToString(paretoPointInfo.TotalInfo.Stats.SatisfactionScore.Value * DevConfig.PercentageFactor, "0.00"), ToStringHelper.ToString(paretoPointInfo.TotalInfo.Stats.Cost, "0"));
        }

        static void WriteOutputToFiles(List<SaInfo> paretoFront, List<List<SaInfo>> paretoFrontsOverTime, long targetIterationCount, Stopwatch stopwatch) {
            // Log summary to console
            using (StreamWriter consoleStreamWriter = new StreamWriter(Console.OpenStandardOutput())) {
                LogSummaryToStream(paretoFront, paretoFrontsOverTime, targetIterationCount, stopwatch, consoleStreamWriter);
            }

            // Create output subfolder
            string dateStr = DateTime.Now.ToString("yyyy-MM-dd-HH-mm");
            string outputSubfolderPath = Path.Combine(DevConfig.OutputFolder, dateStr);
            Directory.CreateDirectory(outputSubfolderPath);

            // Log summary text file to file
            using (StreamWriter summaryFileStreamWriter = new StreamWriter(Path.Combine(outputSubfolderPath, "summary.txt"))) {
                LogSummaryToStream(paretoFront, paretoFrontsOverTime, targetIterationCount, stopwatch, summaryFileStreamWriter);
            }

            // Export JSON files
            JsonOutputHelper.ExportRunJsonFiles(outputSubfolderPath, paretoFront);
        }

        static void LogSummaryToStream(List<SaInfo> paretoFront, List<List<SaInfo>> paretoFrontsOverTime, long targetIterationCount, Stopwatch stopwatch, StreamWriter streamWriter) {
            float saDuration = stopwatch.ElapsedMilliseconds / 1000f;
            float saSpeed = targetIterationCount / saDuration;
            streamWriter.WriteLine("SA finished {0} iterations in {1} s  |  Speed: {2} iterations/s", ToStringHelper.LargeNumToString(targetIterationCount), ToStringHelper.ToString(saDuration), ToStringHelper.LargeNumToString(saSpeed));

            if (paretoFront.Count == 0) {
                streamWriter.WriteLine("SA found no valid solution");
            } else {
                streamWriter.WriteLine("Pareto-optimal front: {0}", FinalParetoFrontToString(paretoFront));

                for (int i = 0; i < paretoFront.Count; i++) {
                    SaInfo paretoPoint = paretoFront[i];
                    streamWriter.WriteLine("\nPoint {0}\n{1}", FinalParetoPointToString(paretoPoint), ToStringHelper.AssignmentToString(paretoPoint));
                }

                // Log progression of min-cost solutions
                streamWriter.WriteLine("\nMin cost progression:");
                streamWriter.WriteLine(string.Join(", ", paretoFrontsOverTime.Select(paretoFront => ToStringHelper.ToString(paretoFront.Count > 0 ? paretoFront[0].TotalInfo.Stats.Cost : -1, "0"))));

                // Log progression of max-satisfaction solutions
                streamWriter.WriteLine("\nMax satisfaction progression:");
                streamWriter.WriteLine(string.Join(", ", paretoFrontsOverTime.Select(paretoFront => ToStringHelper.ToString(paretoFront.Count > 0 ? paretoFront[^1].TotalInfo.Stats.SatisfactionScore.Value * DevConfig.PercentageFactor : -1, "0.00"))));
            }
        }

        static void PeformSaDebugLog(AlgorithmThread saThread, int threadIndex) {
            // Get Pareto-optimal front
            List<SaInfo> paretoFront = GetSingleParetoFront(saThread.BestInfoBySatisfaction);
            string paretoFrontStr;
            if (paretoFront.Count == 0) {
                paretoFrontStr = "No valid solutions";
            } else {
                paretoFrontStr = "Front: " + ParetoFrontToString(paretoFront);
            }

            lock (saThread.Info) {
                double logCost = saThread.Info.TotalInfo.Stats.RawCost + saThread.Info.TotalInfo.Stats.Robustness;

                string iterationNumStr = ToStringHelper.LargeNumToString(saThread.Info.IterationNum, "0.#");
                string lastImprovementIterationStr = saThread.Info.LastImprovementIteration.HasValue ? ToStringHelper.LargeNumToString(saThread.Info.LastImprovementIteration.Value, "0") : "-";
                string cycleNumStr = saThread.Info.CycleNum.ToString();
                string logCostStr = ToStringHelper.LargeNumToString(logCost, "0.0");
                string satisfactionScoreStr = saThread.Info.TotalInfo.Stats.SatisfactionScore.HasValue ? ToStringHelper.ToString(saThread.Info.TotalInfo.Stats.SatisfactionScore.Value * 100, "0") : "?";
                string rawCostStr = ToStringHelper.LargeNumToString(saThread.Info.TotalInfo.Stats.RawCost, "0.0");
                string temperatureStr = ToStringHelper.LargeNumToString(saThread.Info.Temperature, "0.0");
                string satisfactionFactorStr = ToStringHelper.ToString(saThread.Info.SatisfactionFactor, "0.00");
                string penaltyStr = ToStringHelper.GetPenaltyString(saThread.Info.TotalInfo);
                string hasImprovementStr = saThread.Info.HasImprovementSinceLog ? " !!!" : "";

                // Log basic info
                Console.WriteLine("{0,-2}:    # {1,6}    Last.impr: {2,4}    Cycle: {3,2}    Cost: {4,6} ({5,2}%)    Raw: {6,6}    Temp: {7,5}    Sat.f: {8,4}   Penalty: {9,-33}    {10}{11}", threadIndex, iterationNumStr, lastImprovementIterationStr, cycleNumStr, logCostStr, satisfactionScoreStr, rawCostStr, temperatureStr, satisfactionFactorStr, penaltyStr, paretoFrontStr, hasImprovementStr);

                if (DevConfig.DebugLogAdditionalInfo) {
                    Console.WriteLine("Worked times: {0}", ToStringHelper.ToString(saThread.Info.DriverInfos.Select(driverInfo => driverInfo.WorkedTime).ToArray()));
                    Console.WriteLine("Contract time factors: {0}", ToStringHelper.ToString(saThread.Info.Instance.InternalDrivers.Select(driver => (double)saThread.Info.DriverInfos[driver.AllDriversIndex].WorkedTime / driver.ContractTime).ToArray()));
                    Console.WriteLine("Shift counts: {0}", ToStringHelper.ToString(saThread.Info.DriverInfos.Select(driverInfo => driverInfo.ShiftCount).ToArray()));
                    Console.WriteLine("External type shift counts: {0}", ToStringHelper.ToString(saThread.Info.ExternalDriverTypeInfos.Select(externalDriverTypeInfo => externalDriverTypeInfo.ExternalShiftCount).ToArray()));
                }

                if (DevConfig.DebugLogCurrentSolutionAssignment) {
                    Console.WriteLine("Current solution: {0}", ToStringHelper.AssignmentToString(saThread.Info));
                }

                saThread.Info.HasImprovementSinceLog = false;
            }
        }

        static List<SaInfo> GetSingleParetoFront(SaInfo[] bestInfoBySatisfaction) {
            List<SaInfo> paretoFront = new List<SaInfo>();
            SaInfo bestOfPrevLevel = null;
            lock (bestInfoBySatisfaction) {
                for (int satisfactionLevel = bestInfoBySatisfaction.Length - 1; satisfactionLevel >= 0; satisfactionLevel--) {
                    SaInfo bestInfoOfLevel = bestInfoBySatisfaction[satisfactionLevel];
                    if (bestInfoOfLevel.TotalInfo.Stats.Cost == double.MaxValue) continue;

                    if (bestOfPrevLevel == null || bestInfoOfLevel.TotalInfo.Stats.Cost < bestOfPrevLevel.TotalInfo.Stats.Cost - AlgorithmConfig.ParetoFrontMinCostDiff) {
                        paretoFront.Add(bestInfoOfLevel);
                        bestOfPrevLevel = bestInfoOfLevel;
                    }
                }
            }
            paretoFront.Reverse();
            return paretoFront;
        }
    }
}

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
        readonly SaInfo[][] threadBestInfosBySatisfaction;
        int totalIterationCount, totalIterationCountSinceLastLog;
        readonly Stopwatch stopwatch;
        CancellationTokenSource[] threadCancellationTokens;

        public SaMultithreadHandler() {
            stopwatch = new Stopwatch();

            int actualThreadCount = AppConfig.EnableMultithreading ? AppConfig.ThreadCount : 1;
            threadBestInfosBySatisfaction = new SaInfo[actualThreadCount][];
            for (int i = 0; i < actualThreadCount; i++) {
                threadBestInfosBySatisfaction[i] = new SaInfo[MiscConfig.PercentageFactor];
            }
            threadCancellationTokens = new CancellationTokenSource[actualThreadCount];
        }

        public void Run(Instance instance, XorShiftRandom appRand) {
            Console.WriteLine("Starting simulated annealing");

            // Start stopwatch
            stopwatch.Start();

            if (!AppConfig.EnableMultithreading) {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = cancellationTokenSource.Token;
                threadCancellationTokens[0] = cancellationTokenSource;
                SimulatedAnnealing simulatedAnnealing = new SimulatedAnnealing(instance, appRand, threadBestInfosBySatisfaction[0], HandleThreadCallback, cancellationToken);
                simulatedAnnealing.Run();
                return;
            }

            ManualResetEvent[] handles = new ManualResetEvent[AppConfig.ThreadCount];
            for (int threadIndex = 0; threadIndex < AppConfig.ThreadCount; threadIndex++) {
                ulong seed = appRand.NextUInt64();
                XorShiftRandom saRand = new XorShiftRandom(seed);

                SaInfo[] bestInfoBySatisfaction = threadBestInfosBySatisfaction[threadIndex];
                (CancellationTokenSource cts, ManualResetEvent handle) = ThreadHandler.ExecuteInThreadWithCancellation(saRand, (CancellationToken cancellationToken, XorShiftRandom threadRand) => {
                    SimulatedAnnealing simulatedAnnealing = new SimulatedAnnealing(instance, saRand, bestInfoBySatisfaction, HandleThreadCallback, cancellationToken);
                    simulatedAnnealing.Run();
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
            WriteOutputToFiles(paretoFront);
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
            string paretoFrontStr;
            if (paretoFront.Count == 0) {
                paretoFrontStr = "No valid solutions";
            } else {
                paretoFrontStr = "Front: " + ParetoFrontToString(paretoFront);
            }
            Console.WriteLine("# {0,4}    Speed: {1,6}    {2}", ParseHelper.LargeNumToString(totalIterationCount), speedStr, paretoFrontStr);
        }

        List<SaInfo> GetCombinedParetoFront() {
            List<SaInfo> paretoFront = new List<SaInfo>();
            SaInfo bestOfPrevLevel = null;
            for (int satisfactionLevel = MiscConfig.PercentageFactor - 1; satisfactionLevel >= 0; satisfactionLevel--) {
                // Get best info of all threads for this satisfaction level
                SaInfo bestInfoOfLevel = threadBestInfosBySatisfaction[0][satisfactionLevel];
                for (int threadIndex = 1; threadIndex < threadBestInfosBySatisfaction.Length; threadIndex++) {
                    SaInfo threadBestInfoOfLevel = threadBestInfosBySatisfaction[threadIndex][satisfactionLevel];
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

        static string ParetoFrontToString(List<SaInfo> paretoFront) {
            return string.Join(" | ", paretoFront.Select(paretoPoint => ParetoPointToString(paretoPoint)));
        }

        static string ParetoPointToString(SaInfo paretoPoint) {
            return string.Format("{0}% {1}", ParseHelper.ToString(paretoPoint.TotalInfo.Stats.SatisfactionScore.Value * MiscConfig.PercentageFactor, "0"), ParseHelper.LargeNumToString(paretoPoint.TotalInfo.Stats.Cost, "0"));
        }

        static void WriteOutputToFiles(List<SaInfo> paretoFront) {
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
                JsonAssignmentHelper.ExportAssignmentInfoJson(outputSubfolderPath, paretoPoint);
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
    }
}

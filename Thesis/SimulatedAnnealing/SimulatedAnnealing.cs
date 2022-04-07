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
            info.PenaltyFactor = Config.SaInitialPenaltyFactor;
            info.IsHotelStayAfterTrip = new bool[instance.Trips.Length];

            // Initialise best info
            bestInfo = new SaInfo(instance, rand, fastRand);
            bestInfo.IterationNum = -1;
            bestInfo.Temperature = -1;
            bestInfo.PenaltyFactor = 1;
            bestInfo.Cost = double.MaxValue;


            // Create a random assignment
            (info.Assignment, info.ExternalDriverCountsByType) = GetInitialAssignment();

            #if DEBUG
            // Initialise debugger
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.ResetIteration(info);
            }
            #endif

            // Get cost of initial assignment
            (info.Cost, info.CostWithoutPenalty,  info.BasePenalty, info.DriversWorkedTime, _, _, _, _, _) = TotalCostCalculator.GetAssignmentCost(info);

            #if DEBUG
            // Reset iteration in debugger after initial assignment cost
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.ResetIteration(info);
            }
            #endif
        }

        public SaInfo Run() {
            // Start stopwatch
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (info.IterationNum < Config.SaIterationCount) {
                // Pick a random operation based on the configured probabilities
                double operationDouble = info.FastRand.NextDouble();
                AbstractOperation operation;
                if (operationDouble < Config.AssignInternalProbCumulative) operation = AssignInternalOperation.CreateRandom(info);
                else if (operationDouble < Config.AssignExternalProbCumulative) operation = AssignExternalOperation.CreateRandom(info);
                else if (operationDouble < Config.SwapProbCumulative) operation = SwapOperation.CreateRandom(info);
                else operation = ToggleHotelOperation.CreateRandom(info);

                (double costDiff, double costWithoutPenaltyDiff, double basePenaltyDiff) = operation.GetCostDiff();

                bool isAccepted = costDiff < 0 || info.FastRand.NextDouble() < Math.Exp(-costDiff / info.Temperature);
                if (isAccepted) {
                    operation.Execute();
                    info.Cost += costDiff;
                    info.CostWithoutPenalty += costWithoutPenaltyDiff;
                    info.BasePenalty += basePenaltyDiff;

                    if (info.Cost < bestInfo.Cost && info.BasePenalty < 0.01) {
                        // Check cost to remove floating point imprecisions
                        (info.Cost, info.CostWithoutPenalty, info.BasePenalty, info.DriversWorkedTime, _, _, _, _, _) = TotalCostCalculator.GetAssignmentCost(info);
                        if (info.BasePenalty > 0.01) throw new Exception("New best solution is invalid");

                        if (info.Cost < bestInfo.Cost) {
                            bestInfo.Cost = info.Cost;
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
                    int precedenceViolationCount, shiftLengthViolationCount, restTimeViolationCount, contractTimeViolationCount, invalidHotelCount;
                    (info.Cost, info.CostWithoutPenalty, info.BasePenalty, info.DriversWorkedTime, precedenceViolationCount, shiftLengthViolationCount, restTimeViolationCount, contractTimeViolationCount, invalidHotelCount) = TotalCostCalculator.GetAssignmentCost(info);

                    string penaltyString = "-";
                    if (info.BasePenalty > 0) {
                        List<string> penaltyTypes = new List<string>();
                        if (precedenceViolationCount > 0) penaltyTypes.Add("Pr " + precedenceViolationCount);
                        if (shiftLengthViolationCount > 0) penaltyTypes.Add("SL " + shiftLengthViolationCount);
                        if (restTimeViolationCount > 0) penaltyTypes.Add("RT " + restTimeViolationCount);
                        if (contractTimeViolationCount > 0) penaltyTypes.Add("CT " + contractTimeViolationCount);
                        if (invalidHotelCount > 0) penaltyTypes.Add("IH " + invalidHotelCount);
                        string penaltyTypesStr = string.Join(", ", penaltyTypes);

                        penaltyString = string.Format("{0} ({1})", ParseHelper.ToString(info.BasePenalty, "0"), penaltyTypesStr);
                    };

                    string bestCostString = bestInfo.Assignment == null ? "" : ParseHelper.ToString(bestInfo.Cost);
                    string bestAssignmentStr = bestInfo.Assignment == null ? "" : "\nBest sol.: " + ParseHelper.AssignmentToString(bestInfo.Assignment, bestInfo);
                    //string bestAssignmentStr = info.Assignment == null ? "" : ParseHelper.AssignmentToString(info.Assignment, info);
                    //string bestAssignmentStr = ParseHelper.ToString(info.DriversWorkedTime);
                    Console.WriteLine("# {0,4}    Best cost: {1,10}    Cost: {2,10}    Penalty: {3,6}    Temp: {4,5}    P.factor: {5,5}{6}", ParseHelper.LargeNumToString(info.IterationNum), bestCostString, ParseHelper.ToString(info.CostWithoutPenalty), penaltyString, ParseHelper.ToString(info.Temperature, "0"), ParseHelper.ToString(info.PenaltyFactor, "0.00"), bestAssignmentStr);
                }

                // Update temperature and penalty factor
                if (info.IterationNum % Config.SaParameterUpdateFrequency == 0) {
                    info.Temperature *= Config.SaTemperatureReductionFactor;
                    info.PenaltyFactor = Math.Min(1, info.PenaltyFactor + Config.SaPenaltyIncrement);
                    (info.Cost, info.CostWithoutPenalty, info.BasePenalty, info.DriversWorkedTime, _, _, _, _, _) = TotalCostCalculator.GetAssignmentCost(info);
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
                (bestInfo.Cost, bestInfo.CostWithoutPenalty, bestInfo.BasePenalty, bestInfo.DriversWorkedTime, _, _, _, _, _) = TotalCostCalculator.GetAssignmentCost(bestInfo);
                if (bestInfo.BasePenalty > 0.01) throw new Exception("Best solution is invalid");
                bestInfo.DriversWorkedTime = info.DriversWorkedTime;
                bestInfo.ExternalDriverCountsByType = info.ExternalDriverCountsByType;
                bestInfo.IterationNum = info.IterationNum;
            }

            stopwatch.Stop();
            float saDuration = stopwatch.ElapsedMilliseconds / 1000f;
            float saSpeed = Config.SaIterationCount / saDuration;
            Console.WriteLine("SA finished {0} iterations in {1} s  |  Speed: {2} iterations/s", ParseHelper.LargeNumToString(info.IterationNum), ParseHelper.ToString(saDuration), ParseHelper.LargeNumToString(saSpeed));

            return bestInfo;
        }

        (Driver[], int[]) GetInitialAssignment() {
            Driver[] assignment = new Driver[info.Instance.Trips.Length];
            int[] externalDriverCountsByType = new int[info.Instance.ExternalDriversByType.Length];
            for (int tripIndex = 0; tripIndex < info.Instance.Trips.Length; tripIndex++) {
                int driverIndex = info.Rand.Next(info.Instance.InternalDrivers.Length);
                assignment[tripIndex] = info.Instance.InternalDrivers[driverIndex];
            }
            return (assignment, externalDriverCountsByType);
        }

        //(Driver[], int[]) GetInitialAssignment() {
        //    Driver[] assignment = new Driver[info.Instance.Trips.Length];
        //    int[] externalDriverCountsByType = new int[info.Instance.ExternalDriversByType.Length];
        //    for (int tripIndex = 0; tripIndex < info.Instance.Trips.Length; tripIndex++) {
        //        int driverIndex = info.Rand.Next(info.Instance.InternalDrivers.Length + info.Instance.ExternalDriversByType.Length);
        //        if (driverIndex < info.Instance.InternalDrivers.Length) {
        //            // This is an internal driver
        //            assignment[tripIndex] = info.Instance.InternalDrivers[driverIndex];
        //        } else {
        //            // This is an external driver
        //            int externalDriverTypeIndex = driverIndex - info.Instance.InternalDrivers.Length;
        //            ExternalDriver[] externalDriversOfCurrentType = info.Instance.ExternalDriversByType[externalDriverTypeIndex];
        //            int currentCountOfType = externalDriverCountsByType[externalDriverTypeIndex];
        //            int maxNewIndexInTypeExclusive = Math.Min(currentCountOfType + 1, externalDriversOfCurrentType.Length);
        //            int newExternalDriverIndexInType = info.FastRand.NextInt(maxNewIndexInTypeExclusive);
        //            assignment[tripIndex] = externalDriversOfCurrentType[newExternalDriverIndexInType];
        //            externalDriverCountsByType[externalDriverTypeIndex] = Math.Max(externalDriverCountsByType[externalDriverTypeIndex], newExternalDriverIndexInType + 1);
        //        }
        //    }
        //    return (assignment, externalDriverCountsByType);
        //}
    }

    
}

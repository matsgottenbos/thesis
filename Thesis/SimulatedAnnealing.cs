﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class SimulatedAnnealing {
        readonly Instance instance;
        readonly Random rand;

        public SimulatedAnnealing(Instance instance, Random rand) {
            this.instance = instance;
            this.rand = rand;
        }

        public (double, Driver[]) Run() {
            int iterationNum = 0;
            float temperature = Config.SaInitialTemperature;
            float penaltyFactor = Config.SaInitialPenaltyFactor;

            int[] assignmentIndices = GetInitialAssignmentIndices(rand);
            (double cost, double costWithoutPenalty, double penaltyBase) = CostHelper.AssignmentCostWithPenalties(assignmentIndices, instance, penaltyFactor);
            Driver[] assignment = new Driver[assignmentIndices.Length];
            for (int tripIndex = 0; tripIndex < assignmentIndices.Length; tripIndex++) {
                assignment[tripIndex] = instance.Drivers[assignmentIndices[tripIndex]];
            }

            double bestCost = double.MaxValue;
            Driver[] bestAssignment = null;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (iterationNum < Config.SaIterationCount) {
                //int operationIndex = rand.Next(3);
                //Operation operation = operationIndex switch {
                //    0 => AssignTripOperation.CreateRandom(assignment, instance, rand),
                //    1 => UnassignTripOperation.CreateRandom(assignment, instance, rand),
                //    2 => ReassignTripOperation.CreateRandom(assignment, instance, rand),
                //    _ => throw new Exception(),
                //};

                AssignTripOperation operation = AssignTripOperation.CreateRandom(assignment, instance, penaltyFactor, rand);

                // Debug
                Driver[] debugAssignment = (Driver[])assignment.Clone();
                debugAssignment[operation.TripIndex] = operation.NewDriver;
                (double debugTotalCost, double debugCostWithoutPenalty, double debugPenaltyBase) = CostHelper.AssignmentCostWithPenalties(debugAssignment, instance, penaltyFactor);
                double debugTotalCostDiff = debugTotalCost - cost;
                double debugCostWithoutPenaltyDiff = debugCostWithoutPenalty - costWithoutPenalty;
                double debugPenaltyBaseDiff = debugPenaltyBase - penaltyBase;
                DebugCost[] debugDriverCostsOld = CostHelper.DebugCost(assignment, instance, penaltyFactor);
                DebugCost[] debugDriverCostsNew = CostHelper.DebugCost(debugAssignment, instance, penaltyFactor);
                DebugCost[] debugDriverCostDiffs = new DebugCost[Config.DriverCount];
                for (int i = 0; i < Config.DriverCount; i++) {
                    debugDriverCostDiffs[i] = debugDriverCostsNew[i] - debugDriverCostsOld[i];
                }
                DebugCost debugCostOld = DebugCost.CreateBySum(debugDriverCostsOld);
                DebugCost debugCostNew = DebugCost.CreateBySum(debugDriverCostsNew);
                DebugCost debugCostDiff = DebugCost.CreateBySum(debugDriverCostDiffs);
                if (Math.Abs(debugCostDiff.TotalCost - debugTotalCostDiff) > 1) throw new Exception();
                if (Math.Abs(debugCostDiff.DrivingCost + debugCostDiff.TravelCost - debugCostWithoutPenaltyDiff) > 1) throw new Exception();
                if (Math.Abs(debugCostDiff.WorkDayLengthPenaltyBase + debugCostDiff.PrecedencePenaltyBase - debugPenaltyBaseDiff) > 1) throw new Exception();

                //if (operation == null) continue;

                (double costDiff, double costWithoutPenaltyDiff, double penaltyBaseDiff) = operation.GetCostDiff(penaltyFactor, debugDriverCostDiffs, debugDriverCostsOld, debugDriverCostsNew);

                if (costDiff < 0 || rand.NextDouble() < Math.Exp(-costDiff / temperature)) {
                    operation.Execute();
                    cost += costDiff;
                    costWithoutPenalty += costWithoutPenaltyDiff;
                    penaltyBase += penaltyBaseDiff;

                    if (cost < -10) throw new Exception(string.Format("Negative cost: {0}", cost));
                    if (costWithoutPenalty < -10) throw new Exception(string.Format("Negative cost without penalty: {0}", costWithoutPenalty));
                    if (penaltyBase < -10) throw new Exception(string.Format("Negative penalty: {0}", penaltyBase));

                    if (cost < bestCost && penaltyBase < 0.1) {
                        // Check cost to remove floating point imprecisions
                        (cost, costWithoutPenalty, penaltyBase) = CostHelper.AssignmentCostWithPenalties(assignment, instance, penaltyFactor);

                        if (cost < bestCost) {
                            bestCost = cost;
                            bestAssignment = (Driver[])assignment.Clone();
                        }
                    }

                    // Debug
                    if (Math.Abs(debugTotalCost - cost) > 1) throw new Exception(string.Format("Incorrect cost {0} (should be {1})", cost, debugTotalCost));
                    if (Math.Abs(debugCostWithoutPenalty - costWithoutPenalty) > 1) throw new Exception(string.Format("Incorrect cost without penalty {0} (should be {1})", costWithoutPenalty, debugCostWithoutPenalty));
                    if (Math.Abs(debugPenaltyBase - penaltyBase) > 1) throw new Exception(string.Format("Incorrect penalty {0} (should be {1})", penaltyBase, debugPenaltyBase));
                }

                // Update iteration number
                iterationNum++;

                // Check cost to remove floating point imprecisions
                if (iterationNum % Config.SaCheckCostFrequency == 0) {
                    (cost, costWithoutPenalty, penaltyBase) = CostHelper.AssignmentCostWithPenalties(assignment, instance, penaltyFactor);
                }

                // Log
                if (iterationNum % Config.SaLogFrequency == 0) {
                    Console.WriteLine("#: {0}; Best cost: {1}; Cost: {2}; Penalty: {3}; Temp: {4}; Penalty: {5}", ToString(iterationNum), ToString(bestCost), ToString(costWithoutPenalty), ToString(penaltyBase), ToString(temperature), ToString(penaltyFactor));
                }

                // Update temperature and penalty factor
                if (iterationNum % Config.SaParameterUpdateFrequency == 0) {
                    temperature *= Config.SaTemperatureReductionFactor;
                    penaltyFactor = Math.Min(1, penaltyFactor * Config.SaPenaltyIncrementFactor);
                    (cost, costWithoutPenalty, penaltyBase) = CostHelper.AssignmentCostWithPenalties(assignment, instance, penaltyFactor);
                }
            }

            // Check cost to remove floating point imprecisions
            (bestCost, _, _) = CostHelper.AssignmentCostWithPenalties(bestAssignment, instance, 1f);

            Console.WriteLine("SA finished in {0} s", (float)stopwatch.ElapsedMilliseconds / 1000f);

            return (bestCost, bestAssignment);
        }

        string ToString(float num) {
            return Math.Round(num, 4).ToString(CultureInfo.InvariantCulture);
        }
        string ToString(double num) {
            return Math.Round(num, 4).ToString(CultureInfo.InvariantCulture);
        }

        int[] GetInitialAssignmentIndices(Random rand) {
            int[] assignmentIndices = new int[instance.Trips.Length];
            for (int tripIndex = 0; tripIndex < instance.Trips.Length; tripIndex++) {
                assignmentIndices[tripIndex] = rand.Next(instance.Drivers.Length);
            }
            return assignmentIndices;
        }
    }

    abstract class Operation {
        protected readonly Driver[] assignment;
        protected readonly Instance instance;
        protected readonly float penaltyFactor;

        public Operation(Driver[] assignment, Instance instance, float penaltyFactor) {
            this.assignment = assignment;
            this.instance = instance;
            this.penaltyFactor = penaltyFactor;
        }

        public abstract (double, double, double) GetCostDiff(float penaltyFactor, DebugCost[] debugDriverCostDiffs, DebugCost[] debugDriverCostsOld, DebugCost[] debugDriverCostsNew);
        public abstract void Execute();
    }

    class AssignTripOperation : Operation {
        public readonly int TripIndex;
        public readonly Driver NewDriver;

        public AssignTripOperation(int tripIndex, Driver newDriver, Driver[] assignment, Instance instance, float penaltyFactor) : base(assignment, instance, penaltyFactor) {
            TripIndex = tripIndex;
            NewDriver = newDriver;
        }

        public override (double, double, double) GetCostDiff(float penaltyFactor, DebugCost[] debugDriverCostDiffs, DebugCost[] debugDriverCostsOld, DebugCost[] debugDriverCostsNew) {
            Trip trip = instance.Trips[TripIndex];
            Driver oldDriver = assignment[TripIndex];

            (double oldDriverCostDiff, double oldDriverCostWithoutPenaltyDiff, double oldDriverPenaltyBaseDiff) = CostHelper.UnassignTripCostDiff(TripIndex, trip, oldDriver, assignment, instance, penaltyFactor, debugDriverCostDiffs[oldDriver.Index], debugDriverCostsOld[oldDriver.Index], debugDriverCostsNew[oldDriver.Index]);
            (double newDriverCostDiff, double newDriverCostWithoutPenaltyDiff, double newDriverPenaltyBaseDiff) = CostHelper.AssignTripCostDiff(TripIndex, trip, NewDriver, assignment, instance, penaltyFactor, debugDriverCostDiffs[NewDriver.Index], debugDriverCostsOld[NewDriver.Index], debugDriverCostsNew[NewDriver.Index]);

            return (oldDriverCostDiff + newDriverCostDiff, oldDriverCostWithoutPenaltyDiff + newDriverCostWithoutPenaltyDiff, oldDriverPenaltyBaseDiff + newDriverPenaltyBaseDiff);
        }

        public override void Execute() {
            assignment[TripIndex] = NewDriver;
        }

        public static AssignTripOperation CreateRandom(Driver[] assignment, Instance instance, float penaltyFactor, Random rand) {
            int tripIndex = rand.Next(assignment.Length);
            Driver oldDriver = assignment[tripIndex];

            // Select random driver that is not the current driver
            int newDriverIndex = rand.Next(instance.Drivers.Length - 1);
            if (newDriverIndex >= oldDriver.Index) newDriverIndex++;
            Driver newDriver = instance.Drivers[newDriverIndex];

            return new AssignTripOperation(tripIndex, newDriver, assignment, instance, penaltyFactor);
        }
    }
}

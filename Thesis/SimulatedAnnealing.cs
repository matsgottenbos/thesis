using System;
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
        readonly XorShiftRandom fastRand;

        public SimulatedAnnealing(Instance instance, Random rand, XorShiftRandom fastRand) {
            this.instance = instance;
            this.rand = rand;
            this.fastRand = fastRand;
        }

        public (double, Driver[]) Run() {
            int iterationNum = 0;
            float temperature = Config.SaInitialTemperature;
            float penaltyFactor = Config.SaInitialPenaltyFactor;

            // Create a random assignment
            Driver[] assignment = new Driver[instance.Trips.Length];
            int[] assignmentIndices = GetInitialAssignmentIndices(rand);
            Trip[] driverLastTrips = new Trip[instance.Drivers.Length];
            for (int tripIndex = 0; tripIndex < assignmentIndices.Length; tripIndex++) {
                Trip trip = instance.Trips[tripIndex];
                int driverIndex = assignmentIndices[tripIndex];
                Driver driver = instance.Drivers[driverIndex];
                assignment[tripIndex] = driver;
            }

            // Get cost of initial assignment
            (double cost, double costWithoutPenalty, double penaltyBase, double[] driversWorkedTime) = CostHelper.AssignmentCostWithPenalties(assignment, instance, penaltyFactor);

            // Initialise best solution variables
            double bestCost = double.MaxValue;
            Driver[] bestAssignment = null;

            // Start stopwatch
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Initialise two factors for fast random int generation
            double tripCountFactor = fastRand.GetIntFactor(assignment.Length);
            double tripCountMinusOneFactor = fastRand.GetIntFactor(assignment.Length - 1);
            double driverCountMinusOneFactor = fastRand.GetIntFactor(instance.Drivers.Length - 1);

            while (iterationNum < Config.SaIterationCount) {
                // Debug
                if (Config.DebugCheckAndLogOperations) Console.WriteLine("\n\n*** Iteration {0} ***", iterationNum);

                int operationIndex = rand.Next(2);
                Operation operation = operationIndex switch {
                    0 => AssignTripOperation.CreateRandom(assignment, driversWorkedTime, instance, penaltyFactor, fastRand, tripCountFactor, driverCountMinusOneFactor),
                    1 => SwapTripOperation.CreateRandom(assignment, driversWorkedTime, instance, penaltyFactor, fastRand, tripCountFactor, tripCountMinusOneFactor),
                    _ => throw new Exception(),
                };

                (double costDiff, double costWithoutPenaltyDiff, double penaltyBaseDiff) = operation.GetCostDiff(penaltyFactor);

                if (costDiff < 0 || fastRand.NextDouble() < Math.Exp(-costDiff / temperature)) {
                    // Debug
                    double debugCost1, debugCostWithoutPenalty1, debugPenaltyBase1;
                    double[] debugDriversWorkedTime1;
                    if (Config.DebugCheckAndLogOperations) {
                        Console.WriteLine("\n* Before *");
                        (debugCost1, debugCostWithoutPenalty1, debugPenaltyBase1, debugDriversWorkedTime1) = CostHelper.AssignmentCostWithPenalties(assignment, instance, penaltyFactor);
                        Console.WriteLine("Stored worked time: {0}", ParseHelper.ToString(driversWorkedTime));
                        Console.WriteLine("Checked worked time: {0}", ParseHelper.ToString(debugDriversWorkedTime1));
                    } else if (Config.DebugCheckOperations) {
                        (debugCost1, debugCostWithoutPenalty1, debugPenaltyBase1, debugDriversWorkedTime1) = CostHelper.AssignmentCostWithPenalties(assignment, instance, penaltyFactor);
                    }

                    operation.Execute();
                    cost += costDiff;
                    costWithoutPenalty += costWithoutPenaltyDiff;
                    penaltyBase += penaltyBaseDiff;

                    // Debug
                    if (Config.DebugCheckAndLogOperations) {
                        Console.WriteLine("\n* After *");
                        (double debugCost2, double debugCostWithoutPenalty2, double debugPenaltyBase2, double[] debugDriversWorkedTime2) = CostHelper.AssignmentCostWithPenalties(assignment, instance, penaltyFactor);
                        Console.WriteLine("Stored worked time: {0}", ParseHelper.ToString(driversWorkedTime));
                        Console.WriteLine("Checked worked time: {0}", ParseHelper.ToString(debugDriversWorkedTime2));
                        double debugCostDiff = debugCost2 - debugCost1;
                        double debugCostWithoutPenaltyDiff = debugCostWithoutPenalty2 - debugCostWithoutPenalty1;
                        double debugPenaltyBaseDiff = debugPenaltyBase2 - debugPenaltyBase1;
                        if (Math.Abs(debugPenaltyBaseDiff - penaltyBaseDiff) > 0.01) throw new Exception();
                        if (Math.Abs(debugCostWithoutPenaltyDiff - costWithoutPenaltyDiff) > 0.01) throw new Exception();
                        if (Math.Abs(debugCostDiff - costDiff) > 0.01) throw new Exception();
                    } else if (Config.DebugCheckOperations) {
                        (double debugCost2, double debugCostWithoutPenalty2, double debugPenaltyBase2, double[] debugDriversWorkedTime2) = CostHelper.AssignmentCostWithPenalties(assignment, instance, penaltyFactor);
                    }

                    if (cost < bestCost && penaltyBase < 0.01) {
                        // Check cost to remove floating point imprecisions
                        (cost, costWithoutPenalty, penaltyBase, driversWorkedTime) = CostHelper.AssignmentCostWithPenalties(assignment, instance, penaltyFactor);

                        if (cost < bestCost) {
                            bestCost = cost;
                            bestAssignment = (Driver[])assignment.Clone();
                        }
                    }
                }

                // Update iteration number
                iterationNum++;

                // Check cost to remove floating point imprecisions
                if (iterationNum % Config.SaCheckCostFrequency == 0) {
                    (cost, costWithoutPenalty, penaltyBase, driversWorkedTime) = CostHelper.AssignmentCostWithPenalties(assignment, instance, penaltyFactor);
                }

                // Log
                if (iterationNum % Config.SaLogFrequency == 0) {
                    string bestCostString = bestAssignment == null ? "" : ParseHelper.ToString(bestCost);
                    string penaltyString = penaltyBase > 0 ? ParseHelper.ToString(penaltyBase) : "-";
                    string assignmentStr = bestAssignment == null ? "" : string.Join(' ', bestAssignment.Select(driver => driver.Index));
                    Console.WriteLine("# {0,4}    Best cost: {1,10}    Cost: {2,10}    Penalty: {3,11}    Temp: {4,9}    P.factor: {5,5}    Best assignment: {6}", ParseHelper.LargeNumToString(iterationNum), bestCostString, ParseHelper.ToString(costWithoutPenalty), penaltyString, ParseHelper.ToString(temperature), ParseHelper.ToString(penaltyFactor, "0.00"), assignmentStr);
                }

                // Update temperature and penalty factor
                if (iterationNum % Config.SaParameterUpdateFrequency == 0) {
                    temperature *= Config.SaTemperatureReductionFactor;
                    penaltyFactor = Math.Min(1, penaltyFactor + Config.SaPenaltyIncrement);
                    (cost, costWithoutPenalty, penaltyBase, driversWorkedTime) = CostHelper.AssignmentCostWithPenalties(assignment, instance, penaltyFactor);
                }
            }

            // Check cost to remove floating point imprecisions
            (bestCost, _, _, _) = CostHelper.AssignmentCostWithPenalties(bestAssignment, instance, 1f);

            stopwatch.Stop();
            float saDuration = stopwatch.ElapsedMilliseconds / 1000f;
            float saSpeed = Config.SaIterationCount / saDuration;
            Console.WriteLine("SA finished {0} iterations in {1} s  |  Speed: {2} iterations/s", ParseHelper.LargeNumToString(iterationNum), ParseHelper.ToString(saDuration), ParseHelper.LargeNumToString(saSpeed));

            return (bestCost, bestAssignment);
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
        protected readonly double[] driversWorkedTime;
        protected readonly Instance instance;
        protected readonly float penaltyFactor;

        public Operation(Driver[] assignment, double[] driversWorkedTime, Instance instance, float penaltyFactor) {
            this.assignment = assignment;
            this.driversWorkedTime = driversWorkedTime;
            this.instance = instance;
            this.penaltyFactor = penaltyFactor;
        }

        public abstract (double, double, double) GetCostDiff(float penaltyFactor);
        public abstract void Execute();
    }

    class AssignTripOperation : Operation {
        readonly int tripIndex;
        readonly Trip trip;
        readonly Driver oldDriver, newDriver;
        float oldDriverWorkedTimeDiff, newDriverWorkedTimeDiff;

        public AssignTripOperation(int tripIndex, Driver newDriver, Driver[] assignment, double[] driversWorkedTime, Instance instance, float penaltyFactor) : base(assignment, driversWorkedTime, instance, penaltyFactor) {
            this.tripIndex = tripIndex;
            this.newDriver = newDriver;
            trip = instance.Trips[tripIndex];
            oldDriver = assignment[tripIndex];
        }

        public override (double, double, double) GetCostDiff(float penaltyFactor) {
            if (Config.DebugCheckAndLogOperations) Console.WriteLine("\n* Unassign diff *");
            float oldDriverWorkedTime = (float)driversWorkedTime[oldDriver.Index];
            (double oldDriverCostDiff, double oldDriverCostWithoutPenaltyDiff, double oldDriverPenaltyBaseDiff, float oldDriverWorkDayLengthDiff) = CostHelper.UnassignTripCostDiff(trip, oldDriver, null, assignment, oldDriverWorkedTime, instance, penaltyFactor);

            if (Config.DebugCheckAndLogOperations) Console.WriteLine("\n* Assign diff *");
            float newDriverWorkedTime = (float)driversWorkedTime[newDriver.Index];
            (double newDriverCostDiff, double newDriverCostWithoutPenaltyDiff, double newDriverPenaltyBaseDiff, float newDriverWorkDayLengthDiff) = CostHelper.AssignTripCostDiff(trip, newDriver, null, assignment, newDriverWorkedTime, instance, penaltyFactor);

            oldDriverWorkedTimeDiff = oldDriverWorkDayLengthDiff;
            newDriverWorkedTimeDiff = newDriverWorkDayLengthDiff;

            return (oldDriverCostDiff + newDriverCostDiff, oldDriverCostWithoutPenaltyDiff + newDriverCostWithoutPenaltyDiff, oldDriverPenaltyBaseDiff + newDriverPenaltyBaseDiff);
        }

        public override void Execute() {
            assignment[tripIndex] = newDriver;
            driversWorkedTime[oldDriver.Index] += oldDriverWorkedTimeDiff;
            driversWorkedTime[newDriver.Index] += newDriverWorkedTimeDiff;
        }

        public static AssignTripOperation CreateRandom(Driver[] assignment, double[] driversWorkedTime, Instance instance, float penaltyFactor, XorShiftRandom fastRand, double tripCountFactor, double driverCountMinusOneFactor) {
            int tripIndex = fastRand.NextIntWithFactor(tripCountFactor);
            Driver oldDriver = assignment[tripIndex];

            // Select random driver that is not the current driver
            int newDriverIndex = fastRand.NextIntWithFactor(driverCountMinusOneFactor);
            if (newDriverIndex >= oldDriver.Index) newDriverIndex++;
            Driver newDriver = instance.Drivers[newDriverIndex];

            return new AssignTripOperation(tripIndex, newDriver, assignment, driversWorkedTime, instance, penaltyFactor);
        }
    }

    class SwapTripOperation : Operation {
        readonly int tripIndex1, tripIndex2;
        readonly Trip trip1, trip2;
        readonly Driver driver1, driver2;
        float driver1WorkedTimeDiff, driver2WorkedTimeDiff;

        public SwapTripOperation(int tripIndex1, int tripIndex2, Driver[] assignment, double[] driversWorkedTime, Instance instance, float penaltyFactor) : base(assignment, driversWorkedTime, instance, penaltyFactor) {
            this.tripIndex1 = tripIndex1;
            this.tripIndex2 = tripIndex2;
            trip1 = instance.Trips[tripIndex1];
            trip2 = instance.Trips[tripIndex2];
            driver1 = assignment[tripIndex1];
            driver2 = assignment[tripIndex2];
        }

        public override (double, double, double) GetCostDiff(float penaltyFactor) {
            if (Config.DebugCheckAndLogOperations) Console.WriteLine("\n* Unassign first driver diff *");
            float driver1WorkedTime = (float)driversWorkedTime[driver1.Index];
            (double driver1UnassignCostDiff, double driver1UnassignCostWithoutPenaltyDiff, double driver1UnassignPenaltyBaseDiff, float driver1UnassignWorkDayLengthDiff) = CostHelper.UnassignTripCostDiff(trip1, driver1, null, assignment, driver1WorkedTime, instance, penaltyFactor);

            if (Config.DebugCheckAndLogOperations) Console.WriteLine("\n* Unassign second driver diff *");
            float driver2WorkedTime = (float)driversWorkedTime[driver2.Index];
            (double driver2UnassignCostDiff, double driver2UnassignCostWithoutPenaltyDiff, double driver2UnassignPenaltyBaseDiff, float driver2UnassignWorkDayLengthDiff) = CostHelper.UnassignTripCostDiff(trip2, driver2, null, assignment, driver2WorkedTime, instance, penaltyFactor);

            if (Config.DebugCheckAndLogOperations) Console.WriteLine("\n* Assign first driver diff *");
            float driver1WorkedTimeAfterUnassign = driver1WorkedTime + driver1UnassignWorkDayLengthDiff;
            (double driver1AssignCostDiff, double driver1AssignCostWithoutPenaltyDiff, double driver1AssignPenaltyBaseDiff, float driver1AssignWorkDayLengthDiff) = CostHelper.AssignTripCostDiff(trip2, driver1, trip1, assignment, driver1WorkedTimeAfterUnassign, instance, penaltyFactor);

            if (Config.DebugCheckAndLogOperations) Console.WriteLine("\n* Assign second driver diff *");
            float driver2WorkedTimeAfterUnassign = driver2WorkedTime + driver2UnassignWorkDayLengthDiff;
            (double driver2AssignCostDiff, double driver2AssignCostWithoutPenaltyDiff, double driver2AssignPenaltyBaseDiff, float driver2AssignWorkDayLengthDiff) = CostHelper.AssignTripCostDiff(trip1, driver2, trip2, assignment, driver2WorkedTimeAfterUnassign, instance, penaltyFactor);

            double costDiff = driver1UnassignCostDiff + driver2UnassignCostDiff + driver1AssignCostDiff + driver2AssignCostDiff;
            double costWithoutPenalty = driver1UnassignCostWithoutPenaltyDiff + driver2UnassignCostWithoutPenaltyDiff + driver1AssignCostWithoutPenaltyDiff + driver2AssignCostWithoutPenaltyDiff;
            double penaltyBaseDiff = driver1UnassignPenaltyBaseDiff + driver2UnassignPenaltyBaseDiff + driver1AssignPenaltyBaseDiff + driver2AssignPenaltyBaseDiff;

            driver1WorkedTimeDiff = driver1UnassignWorkDayLengthDiff + driver1AssignWorkDayLengthDiff;
            driver2WorkedTimeDiff = driver2UnassignWorkDayLengthDiff + driver2AssignWorkDayLengthDiff;

            return (costDiff, costWithoutPenalty, penaltyBaseDiff);
        }

        public override void Execute() {
            assignment[tripIndex2] = driver1;
            assignment[tripIndex1] = driver2;
            driversWorkedTime[driver1.Index] += driver1WorkedTimeDiff;
            driversWorkedTime[driver2.Index] += driver2WorkedTimeDiff;
        }

        public static SwapTripOperation CreateRandom(Driver[] assignment, double[] driversWorkedTime, Instance instance, float penaltyFactor, XorShiftRandom fastRand, double tripCountFactor, double tripCountMinusOneFactor) {
            int tripIndex1 = fastRand.NextIntWithFactor(tripCountFactor);

            // Select random second trip that is not the first trip
            int tripIndex2 = fastRand.NextIntWithFactor(tripCountMinusOneFactor);
            if (tripIndex2 >= tripIndex1) tripIndex2++;

            // Ensure the selected trips aren't assigned to the same driver
            if (assignment[tripIndex1] == assignment[tripIndex2]) return CreateRandom(assignment, driversWorkedTime, instance, penaltyFactor, fastRand, tripCountFactor, tripCountMinusOneFactor);

            return new SwapTripOperation(tripIndex1, tripIndex2, assignment, driversWorkedTime, instance, penaltyFactor);
        }
    }
}

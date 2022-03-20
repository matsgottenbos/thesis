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
            Trip[] driverLastTrips = new Trip[instance.AllDrivers.Length];
            for (int tripIndex = 0; tripIndex < assignmentIndices.Length; tripIndex++) {
                Trip trip = instance.Trips[tripIndex];
                int driverIndex = assignmentIndices[tripIndex];
                Driver driver = instance.AllDrivers[driverIndex];
                assignment[tripIndex] = driver;
            }

            #if DEBUG
            // Initialise debugger
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.ResetIteration(instance);
            }
            #endif

            // Get cost of initial assignment
            (double cost, double costWithoutPenalty, double basePenalty, int[] driversWorkedTime) = TotalCostCalculator.GetAssignmentCost(assignment, instance, penaltyFactor);

            // Initialise external driver counts
            int[] externalDriverCountsByType = new int[instance.ExternalDriversByType.Length];

            #if DEBUG
            // Reset iteration in debugger after initial assignment cost
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.ResetIteration(instance);
            }
            #endif

            // Initialise best solution variables
            double bestCost = double.MaxValue;
            Driver[] bestAssignment = null;

            // Start stopwatch
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (iterationNum < Config.SaIterationCount) {
                int operationIndex = rand.Next(4);
                Operation operation = operationIndex switch {
                    // Assign internal
                    0 => AssignTripToInternalOperation.CreateRandom(assignment, driversWorkedTime, externalDriverCountsByType, instance, penaltyFactor, fastRand),
                    1 => AssignTripToInternalOperation.CreateRandom(assignment, driversWorkedTime, externalDriverCountsByType, instance, penaltyFactor, fastRand),

                    // Assign existing external
                    2 => AssignTripToExternalOperation.CreateRandom(assignment, driversWorkedTime, externalDriverCountsByType, instance, penaltyFactor, fastRand),

                    // Swap
                    3 => SwapTripOperation.CreateRandom(assignment, driversWorkedTime, externalDriverCountsByType, instance, penaltyFactor, fastRand),
                    _ => throw new Exception(),
                };

                (double costDiff, double costWithoutPenaltyDiff, double basePenaltyDiff) = operation.GetCostDiff(penaltyFactor, iterationNum);

                bool isAccepted = costDiff < 0 || fastRand.NextDouble() < Math.Exp(-costDiff / temperature);
                if (isAccepted) {
                    operation.Execute();
                    cost += costDiff;
                    costWithoutPenalty += costWithoutPenaltyDiff;
                    basePenalty += basePenaltyDiff;

                    if (cost < bestCost && basePenalty < 0.01) {
                        // Check cost to remove floating point imprecisions
                        (cost, costWithoutPenalty, basePenalty, driversWorkedTime) = TotalCostCalculator.GetAssignmentCost(assignment, instance, penaltyFactor);

                        if (cost < bestCost) {
                            bestCost = cost;
                            bestAssignment = (Driver[])assignment.Clone();
                        }
                    }
                }

                // Update iteration number
                iterationNum++;

                #if DEBUG
                // Set debugger to next iteration
                if (Config.DebugCheckAndLogOperations) {
                    SaDebugger.NextIteration(instance);
                }
                #endif

                // Check cost to remove floating point imprecisions
                if (iterationNum % Config.SaCheckCostFrequency == 0) {
                    (cost, costWithoutPenalty, basePenalty, driversWorkedTime) = TotalCostCalculator.GetAssignmentCost(assignment, instance, penaltyFactor);
                }

                // Log
                if (iterationNum % Config.SaLogFrequency == 0) {
                    string bestCostString = bestAssignment == null ? "" : ParseHelper.ToString(bestCost);
                    string penaltyString = basePenalty > 0 ? ParseHelper.ToString(basePenalty, "0") : "-";
                    string assignmentStr = bestAssignment == null ? "" : string.Join(' ', bestAssignment.Select(driver => driver.GetId()));
                    Console.WriteLine("# {0,4}    Best cost: {1,10}    Cost: {2,10}    Penalty: {3,6}    Temp: {4,5}    P.factor: {5,5}    Best sol.: {6}", ParseHelper.LargeNumToString(iterationNum), bestCostString, ParseHelper.ToString(costWithoutPenalty), penaltyString, ParseHelper.ToString(temperature, "0"), ParseHelper.ToString(penaltyFactor, "0.00"), assignmentStr);
                }

                // Update temperature and penalty factor
                if (iterationNum % Config.SaParameterUpdateFrequency == 0) {
                    temperature *= Config.SaTemperatureReductionFactor;
                    penaltyFactor = Math.Min(1, penaltyFactor + Config.SaPenaltyIncrement);
                    (cost, costWithoutPenalty, basePenalty, driversWorkedTime) = TotalCostCalculator.GetAssignmentCost(assignment, instance, penaltyFactor);
                }

                #if DEBUG
                // Reset iteration in debugger after additional checks
                if (Config.DebugCheckAndLogOperations) {
                    SaDebugger.ResetIteration(instance);
                }
                #endif
            }

            // Check cost to remove floating point imprecisions
            (bestCost, _, _, _) = TotalCostCalculator.GetAssignmentCost(bestAssignment, instance, 1f);

            stopwatch.Stop();
            float saDuration = stopwatch.ElapsedMilliseconds / 1000f;
            float saSpeed = Config.SaIterationCount / saDuration;
            Console.WriteLine("SA finished {0} iterations in {1} s  |  Speed: {2} iterations/s", ParseHelper.LargeNumToString(iterationNum), ParseHelper.ToString(saDuration), ParseHelper.LargeNumToString(saSpeed));

            return (bestCost, bestAssignment);
        }

        int[] GetInitialAssignmentIndices(Random rand) {
            // Create an initial assignment with only internal drivers
            int[] assignmentIndices = new int[instance.Trips.Length];
            for (int tripIndex = 0; tripIndex < instance.Trips.Length; tripIndex++) {
                assignmentIndices[tripIndex] = rand.Next(instance.InternalDrivers.Length);
            }
            return assignmentIndices;
        }
    }

    abstract class Operation {
        protected readonly Driver[] assignment;
        protected readonly int[] driversWorkedTime, externalDriverCountsByType;
        protected readonly Instance instance;
        protected readonly float penaltyFactor;

        public Operation(Driver[] assignment, int[] driversWorkedTime, int[] externalDriverCountsByType, Instance instance, float penaltyFactor) {
            this.assignment = assignment;
            this.driversWorkedTime = driversWorkedTime;
            this.externalDriverCountsByType = externalDriverCountsByType;
            this.instance = instance;
            this.penaltyFactor = penaltyFactor;
        }

        public abstract (double, double, double) GetCostDiff(float penaltyFactor, int debugIterationNum);
        public abstract void Execute();
    }

    abstract class AssignTripOperation : Operation {
        readonly Trip trip;
        readonly Driver oldDriver, newDriver;
        int oldDriverWorkedTimeDiff, newDriverWorkedTimeDiff;

        public AssignTripOperation(int tripIndex, Driver newDriver, Driver[] assignment, int[] driversWorkedTime, int[] externalDriverCountsByType, Instance instance, float penaltyFactor) : base(assignment, driversWorkedTime, externalDriverCountsByType, instance, penaltyFactor) {
            this.newDriver = newDriver;
            trip = instance.Trips[tripIndex];
            oldDriver = assignment[tripIndex];
        }

        public override (double, double, double) GetCostDiff(float penaltyFactor, int debugIterationNum) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().Description = string.Format("Re-assign trip {0} from driver {1} to driver {2}", trip.Index, oldDriver.GetId(), newDriver.GetId());
            }
            #endif

            int oldDriverWorkedTime = driversWorkedTime[oldDriver.AllDriversIndex];
            (double oldDriverCostDiff, double oldDriverCostWithoutPenaltyDiff, double oldDriverBasePenaltyDiff, int oldDriverShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(false, trip, null, oldDriver, oldDriverWorkedTime, assignment, instance, penaltyFactor, debugIterationNum);

            int newDriverWorkedTime = driversWorkedTime[newDriver.AllDriversIndex];
            (double newDriverCostDiff, double newDriverCostWithoutPenaltyDiff, double newDriverBasePenaltyDiff, int newDriverShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(true, trip, null, newDriver, newDriverWorkedTime, assignment, instance, penaltyFactor, debugIterationNum);

            oldDriverWorkedTimeDiff = oldDriverShiftLengthDiff;
            newDriverWorkedTimeDiff = newDriverShiftLengthDiff;

            return (oldDriverCostDiff + newDriverCostDiff, oldDriverCostWithoutPenaltyDiff + newDriverCostWithoutPenaltyDiff, oldDriverBasePenaltyDiff + newDriverBasePenaltyDiff);
        }

        public override void Execute() {
            assignment[trip.Index] = newDriver;
            driversWorkedTime[oldDriver.AllDriversIndex] += oldDriverWorkedTimeDiff;
            driversWorkedTime[newDriver.AllDriversIndex] += newDriverWorkedTimeDiff;
        }
    }

    class AssignTripToInternalOperation : AssignTripOperation {
        public AssignTripToInternalOperation(int tripIndex, InternalDriver newInternalDriver, Driver[] assignment, int[] driversWorkedTime, int[] externalDriverCountsByType, Instance instance, float penaltyFactor) : base(tripIndex, newInternalDriver, assignment, driversWorkedTime, externalDriverCountsByType, instance, penaltyFactor) {

        }

        public static AssignTripOperation CreateRandom(Driver[] assignment, int[] driversWorkedTime, int[] externalDriverCountsByType, Instance instance, float penaltyFactor, XorShiftRandom fastRand) {
            int tripIndex = fastRand.NextInt(instance.Trips.Length);
            Driver oldDriver = assignment[tripIndex];

            // Select random internal driver that is not the current driver
            InternalDriver newInternalDriver;
            do {
                int newInternalDriverIndex = fastRand.NextInt(instance.InternalDrivers.Length);
                newInternalDriver = instance.InternalDrivers[newInternalDriverIndex];
            } while (newInternalDriver == oldDriver);

            return new AssignTripToInternalOperation(tripIndex, newInternalDriver, assignment, driversWorkedTime, externalDriverCountsByType, instance, penaltyFactor);
        }
    }

    class AssignTripToExternalOperation : AssignTripOperation {
        ExternalDriver newExternalDriver;

        public AssignTripToExternalOperation(int tripIndex, ExternalDriver newExternalDriver, Driver[] assignment, int[] driversWorkedTime, int[] externalDriverCountsByType, Instance instance, float penaltyFactor) : base(tripIndex, newExternalDriver, assignment, driversWorkedTime, externalDriverCountsByType, instance, penaltyFactor) {
            this.newExternalDriver = newExternalDriver;
        }

        public static AssignTripOperation CreateRandom(Driver[] assignment, int[] driversWorkedTime, int[] externalDriverCountsByType, Instance instance, float penaltyFactor, XorShiftRandom fastRand) {
            int tripIndex = fastRand.NextInt(instance.Trips.Length);
            Driver oldDriver = assignment[tripIndex];

            // Select random existing driver that is not the same as the current driver
            ExternalDriver newExternalDriver;
            do {
                // Select random external driver type
                int newExternalDriverTypeIndex = fastRand.NextInt(instance.ExternalDriversByType.Length);
                ExternalDriver[] externalDriversOfCurrentType = instance.ExternalDriversByType[newExternalDriverTypeIndex];

                // Select random external driver of this type; equal chance to select each existing or a new driver
                int currentCountOfType = externalDriverCountsByType[newExternalDriverTypeIndex];
                int maxNewIndexInTypeExclusive = Math.Min(currentCountOfType + 1, externalDriversOfCurrentType.Length);
                int newExternalDriverIndexInType = fastRand.NextInt(maxNewIndexInTypeExclusive);
                newExternalDriver = externalDriversOfCurrentType[newExternalDriverIndexInType];
            } while (newExternalDriver == oldDriver);

            return new AssignTripToExternalOperation(tripIndex, newExternalDriver, assignment, driversWorkedTime, externalDriverCountsByType, instance, penaltyFactor);
        }

        public override void Execute() {
            base.Execute();

            // If this is a new driver of this type, update the corresponding count
            externalDriverCountsByType[newExternalDriver.ExternalDriverTypeIndex] = Math.Max(externalDriverCountsByType[newExternalDriver.ExternalDriverTypeIndex], newExternalDriver.IndexInType + 1);
        }
    }

    class SwapTripOperation : Operation {
        readonly Trip trip1, trip2;
        readonly Driver driver1, driver2;
        int driver1WorkedTimeDiff, driver2WorkedTimeDiff;

        public SwapTripOperation(int tripIndex1, int tripIndex2, Driver[] assignment, int[] driversWorkedTime, int[] externalDriverCountsByType, Instance instance, float penaltyFactor) : base(assignment, driversWorkedTime, externalDriverCountsByType, instance, penaltyFactor) {
            trip1 = instance.Trips[tripIndex1];
            trip2 = instance.Trips[tripIndex2];
            driver1 = assignment[tripIndex1];
            driver2 = assignment[tripIndex2];
        }

        public override (double, double, double) GetCostDiff(float penaltyFactor, int debugIterationNum) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().Description = string.Format("Swap trip {0} from driver {1} with trip {2} from driver {3}", trip1.Index, driver1.AllDriversIndex, trip2.Index, driver2.AllDriversIndex);
            }
            #endif

            int driver1WorkedTime = driversWorkedTime[driver1.AllDriversIndex];
            (double driver1UnassignCostDiff, double driver1UnassignCostWithoutPenaltyDiff, double driver1UnassignBasePenaltyDiff, int driver1UnassignShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(false, trip1, null, driver1, driver1WorkedTime, assignment, instance, penaltyFactor, debugIterationNum);

            int driver2WorkedTime = driversWorkedTime[driver2.AllDriversIndex];
            (double driver2UnassignCostDiff, double driver2UnassignCostWithoutPenaltyDiff, double driver2UnassignBasePenaltyDiff, int driver2UnassignShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(false, trip2, null, driver2, driver2WorkedTime, assignment, instance, penaltyFactor, debugIterationNum);

            int driver1WorkedTimeAfterUnassign = driver1WorkedTime + driver1UnassignShiftLengthDiff;
            (double driver1AssignCostDiff, double driver1AssignCostWithoutPenaltyDiff, double driver1AssignBasePenaltyDiff, int driver1AssignShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(true, trip2, trip1, driver1, driver1WorkedTimeAfterUnassign, assignment, instance, penaltyFactor, debugIterationNum);

            int driver2WorkedTimeAfterUnassign = driver2WorkedTime + driver2UnassignShiftLengthDiff;
            (double driver2AssignCostDiff, double driver2AssignCostWithoutPenaltyDiff, double driver2AssignBasePenaltyDiff, int driver2AssignShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(true, trip1, trip2, driver2, driver2WorkedTimeAfterUnassign, assignment, instance, penaltyFactor, debugIterationNum);

            double costDiff = driver1UnassignCostDiff + driver2UnassignCostDiff + driver1AssignCostDiff + driver2AssignCostDiff;
            double costWithoutPenalty = driver1UnassignCostWithoutPenaltyDiff + driver2UnassignCostWithoutPenaltyDiff + driver1AssignCostWithoutPenaltyDiff + driver2AssignCostWithoutPenaltyDiff;
            double basePenaltyDiff = driver1UnassignBasePenaltyDiff + driver2UnassignBasePenaltyDiff + driver1AssignBasePenaltyDiff + driver2AssignBasePenaltyDiff;

            driver1WorkedTimeDiff = driver1UnassignShiftLengthDiff + driver1AssignShiftLengthDiff;
            driver2WorkedTimeDiff = driver2UnassignShiftLengthDiff + driver2AssignShiftLengthDiff;

            return (costDiff, costWithoutPenalty, basePenaltyDiff);
        }

        public override void Execute() {
            assignment[trip2.Index] = driver1;
            assignment[trip1.Index] = driver2;
            driversWorkedTime[driver1.AllDriversIndex] += driver1WorkedTimeDiff;
            driversWorkedTime[driver2.AllDriversIndex] += driver2WorkedTimeDiff;
        }

        public static SwapTripOperation CreateRandom(Driver[] assignment, int[] driversWorkedTime, int[] externalDriverCountsByType, Instance instance, float penaltyFactor, XorShiftRandom fastRand) {
            int tripIndex1 = fastRand.NextInt(instance.Trips.Length);

            // Select random second trip that is not the first trip, and that isn't assigned to the same driver as the first trip
            int tripIndex2;
            do {
                tripIndex2 = fastRand.NextInt(instance.Trips.Length);
            } while (tripIndex1 == tripIndex2 || assignment[tripIndex1] == assignment[tripIndex2]);

            return new SwapTripOperation(tripIndex1, tripIndex2, assignment, driversWorkedTime, externalDriverCountsByType, instance, penaltyFactor);
        }
    }
}

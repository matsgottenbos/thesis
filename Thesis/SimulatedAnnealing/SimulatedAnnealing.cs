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

            #if DEBUG
            // Initialise debugger
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.ResetIteration(instance);
            }
            #endif

            // Get cost of initial assignment
            (double cost, double costWithoutPenalty, double basePenalty, int[] driversWorkedTime) = TotalCostCalculator.GetAssignmentCost(assignment, instance, penaltyFactor);

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

            // Initialise two factors for fast random int generation
            double tripCountFactor = fastRand.GetIntFactor(assignment.Length);
            double tripCountMinusOneFactor = fastRand.GetIntFactor(assignment.Length - 1);
            double driverCountMinusOneFactor = fastRand.GetIntFactor(instance.Drivers.Length - 1);

            while (iterationNum < Config.SaIterationCount) {
                int operationIndex = rand.Next(2);
                Operation operation = operationIndex switch {
                    0 => AssignTripOperation.CreateRandom(assignment, driversWorkedTime, instance, penaltyFactor, fastRand, tripCountFactor, driverCountMinusOneFactor),
                    1 => SwapTripOperation.CreateRandom(assignment, driversWorkedTime, instance, penaltyFactor, fastRand, tripCountFactor, tripCountMinusOneFactor),
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
                    string assignmentStr = bestAssignment == null ? "" : string.Join(' ', bestAssignment.Select(driver => driver.Index));
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
            int[] assignmentIndices = new int[instance.Trips.Length];
            for (int tripIndex = 0; tripIndex < instance.Trips.Length; tripIndex++) {
                assignmentIndices[tripIndex] = rand.Next(instance.Drivers.Length);
            }
            return assignmentIndices;
        }
    }

    abstract class Operation {
        protected readonly Driver[] assignment;
        protected readonly int[] driversWorkedTime;
        protected readonly Instance instance;
        protected readonly float penaltyFactor;

        public Operation(Driver[] assignment, int[] driversWorkedTime, Instance instance, float penaltyFactor) {
            this.assignment = assignment;
            this.driversWorkedTime = driversWorkedTime;
            this.instance = instance;
            this.penaltyFactor = penaltyFactor;
        }

        public abstract (double, double, double) GetCostDiff(float penaltyFactor, int debugIterationNum);
        public abstract void Execute();
    }

    class AssignTripOperation : Operation {
        readonly Trip trip;
        readonly Driver oldDriver, newDriver;
        int oldDriverWorkedTimeDiff, newDriverWorkedTimeDiff;

        public AssignTripOperation(int tripIndex, Driver newDriver, Driver[] assignment, int[] driversWorkedTime, Instance instance, float penaltyFactor) : base(assignment, driversWorkedTime, instance, penaltyFactor) {
            this.newDriver = newDriver;
            trip = instance.Trips[tripIndex];
            oldDriver = assignment[tripIndex];
        }

        public override (double, double, double) GetCostDiff(float penaltyFactor, int debugIterationNum) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().Description = string.Format("Re-assign trip {0} from driver {1} to driver {2}", trip.Index, oldDriver.Index, newDriver.Index);
            }
            #endif

            int oldDriverWorkedTime = driversWorkedTime[oldDriver.Index];
            (double oldDriverCostDiff, double oldDriverCostWithoutPenaltyDiff, double oldDriverBasePenaltyDiff, int oldDriverShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(false, trip, null, oldDriver, oldDriverWorkedTime, assignment, instance, penaltyFactor, debugIterationNum);

            int newDriverWorkedTime = driversWorkedTime[newDriver.Index];
            (double newDriverCostDiff, double newDriverCostWithoutPenaltyDiff, double newDriverBasePenaltyDiff, int newDriverShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(true, trip, null, newDriver, newDriverWorkedTime, assignment, instance, penaltyFactor, debugIterationNum);

            oldDriverWorkedTimeDiff = oldDriverShiftLengthDiff;
            newDriverWorkedTimeDiff = newDriverShiftLengthDiff;

            return (oldDriverCostDiff + newDriverCostDiff, oldDriverCostWithoutPenaltyDiff + newDriverCostWithoutPenaltyDiff, oldDriverBasePenaltyDiff + newDriverBasePenaltyDiff);
        }

        public override void Execute() {
            assignment[trip.Index] = newDriver;
            driversWorkedTime[oldDriver.Index] += oldDriverWorkedTimeDiff;
            driversWorkedTime[newDriver.Index] += newDriverWorkedTimeDiff;
        }

        public static AssignTripOperation CreateRandom(Driver[] assignment, int[] driversWorkedTime, Instance instance, float penaltyFactor, XorShiftRandom fastRand, double tripCountFactor, double driverCountMinusOneFactor) {
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
        readonly Trip trip1, trip2;
        readonly Driver driver1, driver2;
        int driver1WorkedTimeDiff, driver2WorkedTimeDiff;

        public SwapTripOperation(int tripIndex1, int tripIndex2, Driver[] assignment, int[] driversWorkedTime, Instance instance, float penaltyFactor) : base(assignment, driversWorkedTime, instance, penaltyFactor) {
            trip1 = instance.Trips[tripIndex1];
            trip2 = instance.Trips[tripIndex2];
            driver1 = assignment[tripIndex1];
            driver2 = assignment[tripIndex2];
        }

        public override (double, double, double) GetCostDiff(float penaltyFactor, int debugIterationNum) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().Description = string.Format("Swap trip {0} from driver {1} with trip {2} from driver {3}", trip1.Index, driver1.Index, trip2.Index, driver2.Index);
            }
            #endif

            int driver1WorkedTime = driversWorkedTime[driver1.Index];
            (double driver1UnassignCostDiff, double driver1UnassignCostWithoutPenaltyDiff, double driver1UnassignBasePenaltyDiff, int driver1UnassignShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(false, trip1, null, driver1, driver1WorkedTime, assignment, instance, penaltyFactor, debugIterationNum);

            int driver2WorkedTime = driversWorkedTime[driver2.Index];
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
            driversWorkedTime[driver1.Index] += driver1WorkedTimeDiff;
            driversWorkedTime[driver2.Index] += driver2WorkedTimeDiff;
        }

        public static SwapTripOperation CreateRandom(Driver[] assignment, int[] driversWorkedTime, Instance instance, float penaltyFactor, XorShiftRandom fastRand, double tripCountFactor, double tripCountMinusOneFactor) {
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

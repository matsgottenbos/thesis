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

            // Create arrays with assignment info
            Driver[] assignment = new Driver[instance.Trips.Length];
            Trip[,] sameDayTripsBefore = new Trip[instance.Trips.Length, instance.Drivers.Length]; // For each trip, for each driver, the trip before on the same day by that driver
            Trip[,] sameDayTripsAfter = new Trip[instance.Trips.Length, instance.Drivers.Length]; // For each trip, for each driver, the trip after on the same day by that driver


            // Create a random assignment
            int[] assignmentIndices = GetInitialAssignmentIndices(rand);
            Trip[] driverLastTrips = new Trip[instance.Drivers.Length];
            for (int tripIndex = 0; tripIndex < assignmentIndices.Length; tripIndex++) {
                Trip trip = instance.Trips[tripIndex];
                int driverIndex = assignmentIndices[tripIndex];
                Driver driver = instance.Drivers[driverIndex];

                assignment[tripIndex] = driver;
                CostHelper.AssignSameDayTripsBeforeAfter(trip, driverIndex, sameDayTripsBefore, sameDayTripsAfter, instance);
            }

            // Get cost of initial assignment
            (double cost, double costWithoutPenalty, double penaltyBase) = CostHelper.AssignmentCostWithPenalties(assignmentIndices, instance, penaltyFactor);

            // Initialise best solution variables
            double bestCost = double.MaxValue;
            Driver[] bestAssignment = null;

            // Start stopwatch
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Initialise two factors for fast random int generation
            double tripCountFactor = fastRand.GetIntFactor(assignment.Length);
            double driverCountMinusOneFactor = fastRand.GetIntFactor(instance.Drivers.Length - 1);

            while (iterationNum < Config.SaIterationCount) {
                //int operationIndex = rand.Next(3);
                //Operation operation = operationIndex switch {
                //    0 => AssignTripOperation.CreateRandom(assignment, instance, rand),
                //    1 => UnassignTripOperation.CreateRandom(assignment, instance, rand),
                //    2 => ReassignTripOperation.CreateRandom(assignment, instance, rand),
                //    _ => throw new Exception(),
                //};

                AssignTripOperation operation = AssignTripOperation.CreateRandom(assignment, sameDayTripsBefore, sameDayTripsAfter, instance, penaltyFactor, fastRand, tripCountFactor, driverCountMinusOneFactor);

                //if (operation == null) continue;

                (double costDiff, double costWithoutPenaltyDiff, double penaltyBaseDiff) = operation.GetCostDiff(penaltyFactor);

                if (costDiff < 0 || fastRand.NextDouble() < Math.Exp(-costDiff / temperature)) {
                    operation.Execute();
                    cost += costDiff;
                    costWithoutPenalty += costWithoutPenaltyDiff;
                    penaltyBase += penaltyBaseDiff;

                    if (cost < -10) throw new Exception(string.Format("Negative cost: {0}", cost));
                    if (costWithoutPenalty < -10) throw new Exception(string.Format("Negative cost without penalty: {0}", costWithoutPenalty));
                    if (penaltyBase < -10) throw new Exception(string.Format("Negative penalty: {0}", penaltyBase));

                    if (cost < bestCost && penaltyBase < 0.01) {
                        // Check cost to remove floating point imprecisions
                        (cost, costWithoutPenalty, penaltyBase) = CostHelper.AssignmentCostWithPenalties(assignment, instance, penaltyFactor);

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
                    (cost, costWithoutPenalty, penaltyBase) = CostHelper.AssignmentCostWithPenalties(assignment, instance, penaltyFactor);
                }

                // Log
                if (iterationNum % Config.SaLogFrequency == 0) {
                    string assignmentStr = string.Join(' ', bestAssignment.Select(driver => driver.Index));
                    Console.WriteLine("#: {0}; Best cost: {1}; Cost: {2}; Penalty: {3}; Temp: {4}; P.factor: {5}; Best assignment: {6}", LargeNumToString(iterationNum, 2), ToString(bestCost), ToString(costWithoutPenalty), ToString(penaltyBase), ToString(temperature), ToString(penaltyFactor), assignmentStr);
                }

                // Update temperature and penalty factor
                if (iterationNum % Config.SaParameterUpdateFrequency == 0) {
                    temperature *= Config.SaTemperatureReductionFactor;
                    penaltyFactor = Math.Min(1, penaltyFactor + Config.SaPenaltyIncrement);
                    (cost, costWithoutPenalty, penaltyBase) = CostHelper.AssignmentCostWithPenalties(assignment, instance, penaltyFactor);
                }
            }

            // Check cost to remove floating point imprecisions
            (bestCost, _, _) = CostHelper.AssignmentCostWithPenalties(bestAssignment, instance, 1f);

            stopwatch.Stop();
            float saDuration = stopwatch.ElapsedMilliseconds / 1000f;
            float saSpeed = Config.SaIterationCount / saDuration;
            Console.WriteLine("SA finished {0} iterations in {1} s  |  Speed: {2} iterations/s", LargeNumToString(iterationNum, 2), ToString(saDuration), LargeNumToString(saSpeed));

            return (bestCost, bestAssignment);
        }

        static string ToString(double num, int precision = 4) {
            return Math.Round(num, precision).ToString(CultureInfo.InvariantCulture);
        }

        static string LargeNumToString(double num, int precision = 4) {
            if (num < 1000) {
                return ToString(num, precision);
            } else if (num < 1000000) {
                double numThousands = num / 1000;
                return ToString(numThousands, precision) + "k";
            } else if (num < 1000000000) {
                double numMillions = num / 1000000;
                return ToString(numMillions, precision) + "M";
            } else {
                double numMBllions = num / 1000000000;
                return ToString(numMBllions, precision) + "B";
            }
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
        protected readonly Trip[,] sameDayTripsBefore, sameDayTripsAfter;
        protected readonly Instance instance;
        protected readonly float penaltyFactor;

        public Operation(Driver[] assignment, Trip[,] sameDayTripsBefore, Trip[,] sameDayTripsAfter, Instance instance, float penaltyFactor) {
            this.assignment = assignment;
            this.sameDayTripsBefore = sameDayTripsBefore;
            this.sameDayTripsAfter = sameDayTripsAfter;
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

        public AssignTripOperation(int tripIndex, Driver newDriver, Driver[] assignment, Trip[,] sameDayTripsBefore, Trip[,] sameDayTripsAfter, Instance instance, float penaltyFactor) : base(assignment, sameDayTripsBefore, sameDayTripsAfter, instance, penaltyFactor) {
            this.tripIndex = tripIndex;
            this.newDriver = newDriver;
            trip = instance.Trips[tripIndex];
            oldDriver = assignment[tripIndex];
        }

        public override (double, double, double) GetCostDiff(float penaltyFactor) {
            (double oldDriverCostDiff, double oldDriverCostWithoutPenaltyDiff, double oldDriverPenaltyBaseDiff) = CostHelper.UnassignTripCostDiff(trip, oldDriver, assignment, sameDayTripsBefore, sameDayTripsAfter, instance, penaltyFactor);
            (double newDriverCostDiff, double newDriverCostWithoutPenaltyDiff, double newDriverPenaltyBaseDiff) = CostHelper.AssignTripCostDiff(trip, newDriver, assignment, sameDayTripsBefore, sameDayTripsAfter, instance, penaltyFactor);

            return (oldDriverCostDiff + newDriverCostDiff, oldDriverCostWithoutPenaltyDiff + newDriverCostWithoutPenaltyDiff, oldDriverPenaltyBaseDiff + newDriverPenaltyBaseDiff);
        }

        public override void Execute() {
            assignment[tripIndex] = newDriver;
            CostHelper.UnassignSameDayTripsBeforeAfter(trip, oldDriver.Index, sameDayTripsBefore, sameDayTripsAfter, instance);
            CostHelper.AssignSameDayTripsBeforeAfter(trip, newDriver.Index, sameDayTripsBefore, sameDayTripsAfter, instance);
        }

        public static AssignTripOperation CreateRandom(Driver[] assignment, Trip[,] sameDayTripsBefore, Trip[,] sameDayTripsAfter, Instance instance, float penaltyFactor, XorShiftRandom fastRand, double tripCountFactor, double driverCountMinusOneFactor) {
            int tripIndex = fastRand.NextIntWithFactor(tripCountFactor);
            Driver oldDriver = assignment[tripIndex];

            // Select random driver that is not the current driver
            int newDriverIndex = fastRand.NextIntWithFactor(driverCountMinusOneFactor);
            if (newDriverIndex >= oldDriver.Index) newDriverIndex++;
            Driver newDriver = instance.Drivers[newDriverIndex];

            return new AssignTripOperation(tripIndex, newDriver, assignment, sameDayTripsBefore, sameDayTripsAfter, instance, penaltyFactor);
        }
    }
}

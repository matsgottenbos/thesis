using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class SimulatedAnnealing {
        readonly SaInfo info;

        public SimulatedAnnealing(Instance instance, Random rand, XorShiftRandom fastRand) {
            info = new SaInfo(instance, rand, fastRand);

            // Initialise variables
            info.IterationNum = 0;
            info.Temperature = Config.SaInitialTemperature;
            info.PenaltyFactor = Config.SaInitialPenaltyFactor;
            info.BestCost = double.MaxValue;

            // Create a random assignment
            info.Assignment = new Driver[instance.Trips.Length];
            int[] assignmentIndices = GetInitialAssignmentIndices();
            Trip[] driverLastTrips = new Trip[instance.AllDrivers.Length];
            for (int tripIndex = 0; tripIndex < assignmentIndices.Length; tripIndex++) {
                Trip trip = instance.Trips[tripIndex];
                int driverIndex = assignmentIndices[tripIndex];
                Driver driver = instance.AllDrivers[driverIndex];
                info.Assignment[tripIndex] = driver;
            }

            #if DEBUG
            // Initialise debugger
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.ResetIteration(instance);
            }
            #endif

            // Get cost of initial assignment
            (info.Cost, info.CostWithoutPenalty,  info.BasePenalty, info.DriversWorkedTime) = TotalCostCalculator.GetAssignmentCost(info);

            // Initialise external driver counts
            info.ExternalDriverCountsByType = new int[instance.ExternalDriversByType.Length];

            #if DEBUG
            // Reset iteration in debugger after initial assignment cost
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.ResetIteration(instance);
            }
            #endif
        }

        public (double, Driver[]) Run() {
            // Start stopwatch
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (info.IterationNum < Config.SaIterationCount) {
                int operationIndex = info.Rand.Next(4);
                Operation operation = operationIndex switch {
                    // Assign internal
                    0 => AssignTripToInternalOperation.CreateRandom(info),
                    1 => AssignTripToInternalOperation.CreateRandom(info),

                    // Assign existing external
                    2 => AssignTripToExternalOperation.CreateRandom(info),

                    // Swap
                    3 => SwapTripOperation.CreateRandom(info),

                    _ => throw new Exception(),
                };

                (double costDiff, double costWithoutPenaltyDiff, double basePenaltyDiff) = operation.GetCostDiff();

                bool isAccepted = costDiff < 0 || info.FastRand.NextDouble() < Math.Exp(-costDiff / info.Temperature);
                if (isAccepted) {
                    operation.Execute();
                    info.Cost += costDiff;
                    info.CostWithoutPenalty += costWithoutPenaltyDiff;
                    info.BasePenalty += basePenaltyDiff;

                    if (info.Cost < info.BestCost && info.BasePenalty < 0.01) {
                        // Check cost to remove floating point imprecisions
                        (info.Cost, info.CostWithoutPenalty, info.BasePenalty, info.DriversWorkedTime) = TotalCostCalculator.GetAssignmentCost(info);

                        if (info.Cost < info.BestCost) {
                            info.BestCost = info.Cost;
                            info.BestAssignment = (Driver[])info.Assignment.Clone();
                        }
                    }
                }

                // Update iteration number
                info.IterationNum++;

                #if DEBUG
                // Set debugger to next iteration
                if (Config.DebugCheckAndLogOperations) {
                    SaDebugger.NextIteration(info.Instance);
                }
                #endif

                // Check cost to remove floating point imprecisions
                if (info.IterationNum % Config.SaCheckCostFrequency == 0) {
                    (info.Cost, info.CostWithoutPenalty, info.BasePenalty, info.DriversWorkedTime) = TotalCostCalculator.GetAssignmentCost(info);
                }

                // Log
                if (info.IterationNum % Config.SaLogFrequency == 0) {
                    string bestCostString = info.BestAssignment == null ? "" : ParseHelper.ToString(info.BestCost);
                    string penaltyString = info.BasePenalty > 0 ? ParseHelper.ToString(info.BasePenalty, "0") : "-";
                    string assignmentStr = info.BestAssignment == null ? "" : string.Join(' ', info.BestAssignment.Select(driver => driver.GetId()));
                    Console.WriteLine("# {0,4}    Best cost: {1,10}    Cost: {2,10}    Penalty: {3,6}    Temp: {4,5}    P.factor: {5,5}    Best sol.: {6}", ParseHelper.LargeNumToString(info.IterationNum), bestCostString, ParseHelper.ToString(info.CostWithoutPenalty), penaltyString, ParseHelper.ToString(info.Temperature, "0"), ParseHelper.ToString(info.PenaltyFactor, "0.00"), assignmentStr);
                }

                // Update temperature and penalty factor
                if (info.IterationNum % Config.SaParameterUpdateFrequency == 0) {
                    info.Temperature *= Config.SaTemperatureReductionFactor;
                    info.PenaltyFactor = Math.Min(1, info.PenaltyFactor + Config.SaPenaltyIncrement);
                    (info.Cost, info.CostWithoutPenalty, info.BasePenalty, info.DriversWorkedTime) = TotalCostCalculator.GetAssignmentCost(info);
                }

                #if DEBUG
                // Reset iteration in debugger after additional checks
                if (Config.DebugCheckAndLogOperations) {
                    SaDebugger.ResetIteration(info.Instance);
                }
                #endif
            }

            // Check cost to remove floating point imprecisions
            info.PenaltyFactor = 1;
            (info.BestCost, _, _, _) = TotalCostCalculator.GetAssignmentCost(info);

            stopwatch.Stop();
            float saDuration = stopwatch.ElapsedMilliseconds / 1000f;
            float saSpeed = Config.SaIterationCount / saDuration;
            Console.WriteLine("SA finished {0} iterations in {1} s  |  Speed: {2} iterations/s", ParseHelper.LargeNumToString(info.IterationNum), ParseHelper.ToString(saDuration), ParseHelper.LargeNumToString(saSpeed));

            return (info.BestCost, info.BestAssignment);
        }

        int[] GetInitialAssignmentIndices() {
            // Create an initial assignment with only internal drivers
            int[] assignmentIndices = new int[info.Instance.Trips.Length];
            for (int tripIndex = 0; tripIndex < info.Instance.Trips.Length; tripIndex++) {
                assignmentIndices[tripIndex] = info.Rand.Next(info.Instance.InternalDrivers.Length);
            }
            return assignmentIndices;
        }
    }

    abstract class Operation {
        protected readonly SaInfo info;

        public Operation(SaInfo info) {
            this.info = info;
        }

        public abstract (double, double, double) GetCostDiff();
        public abstract void Execute();
    }

    abstract class AssignTripOperation : Operation {
        readonly Trip trip;
        readonly Driver oldDriver, newDriver;
        int oldDriverWorkedTimeDiff, newDriverWorkedTimeDiff;

        public AssignTripOperation(int tripIndex, Driver newDriver, SaInfo info) : base(info) {
            this.newDriver = newDriver;
            trip = info.Instance.Trips[tripIndex];
            oldDriver = info.Assignment[tripIndex];
        }

        public override (double, double, double) GetCostDiff() {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().Description = string.Format("Re-assign trip {0} from driver {1} to driver {2}", trip.Index, oldDriver.GetId(), newDriver.GetId());
            }
            #endif

            int oldDriverWorkedTime = info.DriversWorkedTime[oldDriver.AllDriversIndex];
            (double oldDriverCostDiff, double oldDriverCostWithoutPenaltyDiff, double oldDriverBasePenaltyDiff, int oldDriverShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(false, trip, null, oldDriver, oldDriverWorkedTime, info);

            int newDriverWorkedTime = info.DriversWorkedTime[newDriver.AllDriversIndex];
            (double newDriverCostDiff, double newDriverCostWithoutPenaltyDiff, double newDriverBasePenaltyDiff, int newDriverShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(true, trip, null, newDriver, newDriverWorkedTime, info);

            oldDriverWorkedTimeDiff = oldDriverShiftLengthDiff;
            newDriverWorkedTimeDiff = newDriverShiftLengthDiff;

            return (oldDriverCostDiff + newDriverCostDiff, oldDriverCostWithoutPenaltyDiff + newDriverCostWithoutPenaltyDiff, oldDriverBasePenaltyDiff + newDriverBasePenaltyDiff);
        }

        public override void Execute() {
            info.Assignment[trip.Index] = newDriver;
            info.DriversWorkedTime[oldDriver.AllDriversIndex] += oldDriverWorkedTimeDiff;
            info.DriversWorkedTime[newDriver.AllDriversIndex] += newDriverWorkedTimeDiff;
        }
    }

    class AssignTripToInternalOperation : AssignTripOperation {
        public AssignTripToInternalOperation(int tripIndex, InternalDriver newInternalDriver, SaInfo info) : base(tripIndex, newInternalDriver, info) {

        }

        public static AssignTripOperation CreateRandom(SaInfo info) {
            int tripIndex = info.FastRand.NextInt(info.Instance.Trips.Length);
            Driver oldDriver = info.Assignment[tripIndex];

            // Select random internal driver that is not the current driver
            InternalDriver newInternalDriver;
            do {
                int newInternalDriverIndex = info.FastRand.NextInt(info.Instance.InternalDrivers.Length);
                newInternalDriver = info.Instance.InternalDrivers[newInternalDriverIndex];
            } while (newInternalDriver == oldDriver);

            return new AssignTripToInternalOperation(tripIndex, newInternalDriver, info);
        }
    }

    class AssignTripToExternalOperation : AssignTripOperation {
        ExternalDriver newExternalDriver;

        public AssignTripToExternalOperation(int tripIndex, ExternalDriver newExternalDriver, SaInfo info) : base(tripIndex, newExternalDriver, info) {
            this.newExternalDriver = newExternalDriver;
        }

        public static AssignTripOperation CreateRandom(SaInfo info) {
            int tripIndex = info.FastRand.NextInt(info.Instance.Trips.Length);
            Driver oldDriver = info.Assignment[tripIndex];

            // Select random existing driver that is not the same as the current driver
            ExternalDriver newExternalDriver;
            do {
                // Select random external driver type
                int newExternalDriverTypeIndex = info.FastRand.NextInt(info.Instance.ExternalDriversByType.Length);
                ExternalDriver[] externalDriversOfCurrentType = info.Instance.ExternalDriversByType[newExternalDriverTypeIndex];

                // Select random external driver of this type; equal chance to select each existing or a new driver
                int currentCountOfType = info.ExternalDriverCountsByType[newExternalDriverTypeIndex];
                int maxNewIndexInTypeExclusive = Math.Min(currentCountOfType + 1, externalDriversOfCurrentType.Length);
                int newExternalDriverIndexInType = info.FastRand.NextInt(maxNewIndexInTypeExclusive);
                newExternalDriver = externalDriversOfCurrentType[newExternalDriverIndexInType];
            } while (newExternalDriver == oldDriver);

            return new AssignTripToExternalOperation(tripIndex, newExternalDriver, info);
        }

        public override void Execute() {
            base.Execute();

            // If this is a new driver of this type, update the corresponding count
            info.ExternalDriverCountsByType[newExternalDriver.ExternalDriverTypeIndex] = Math.Max(info.ExternalDriverCountsByType[newExternalDriver.ExternalDriverTypeIndex], newExternalDriver.IndexInType + 1);
        }
    }

    class SwapTripOperation : Operation {
        readonly Trip trip1, trip2;
        readonly Driver driver1, driver2;
        int driver1WorkedTimeDiff, driver2WorkedTimeDiff;

        public SwapTripOperation(int tripIndex1, int tripIndex2, SaInfo info) : base(info) {
            trip1 = info.Instance.Trips[tripIndex1];
            trip2 = info.Instance.Trips[tripIndex2];
            driver1 = info.Assignment[tripIndex1];
            driver2 = info.Assignment[tripIndex2];
        }

        public override (double, double, double) GetCostDiff() {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentOperation().Description = string.Format("Swap trip {0} from driver {1} with trip {2} from driver {3}", trip1.Index, driver1.AllDriversIndex, trip2.Index, driver2.AllDriversIndex);
            }
            #endif

            int driver1WorkedTime = info.DriversWorkedTime[driver1.AllDriversIndex];
            (double driver1UnassignCostDiff, double driver1UnassignCostWithoutPenaltyDiff, double driver1UnassignBasePenaltyDiff, int driver1UnassignShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(false, trip1, null, driver1, driver1WorkedTime, info);

            int driver2WorkedTime = info.DriversWorkedTime[driver2.AllDriversIndex];
            (double driver2UnassignCostDiff, double driver2UnassignCostWithoutPenaltyDiff, double driver2UnassignBasePenaltyDiff, int driver2UnassignShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(false, trip2, null, driver2, driver2WorkedTime, info);

            int driver1WorkedTimeAfterUnassign = driver1WorkedTime + driver1UnassignShiftLengthDiff;
            (double driver1AssignCostDiff, double driver1AssignCostWithoutPenaltyDiff, double driver1AssignBasePenaltyDiff, int driver1AssignShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(true, trip2, trip1, driver1, driver1WorkedTimeAfterUnassign, info);

            int driver2WorkedTimeAfterUnassign = driver2WorkedTime + driver2UnassignShiftLengthDiff;
            (double driver2AssignCostDiff, double driver2AssignCostWithoutPenaltyDiff, double driver2AssignBasePenaltyDiff, int driver2AssignShiftLengthDiff) = CostDiffCalculator.AssignOrUnassignTrip(true, trip1, trip2, driver2, driver2WorkedTimeAfterUnassign, info);

            double costDiff = driver1UnassignCostDiff + driver2UnassignCostDiff + driver1AssignCostDiff + driver2AssignCostDiff;
            double costWithoutPenalty = driver1UnassignCostWithoutPenaltyDiff + driver2UnassignCostWithoutPenaltyDiff + driver1AssignCostWithoutPenaltyDiff + driver2AssignCostWithoutPenaltyDiff;
            double basePenaltyDiff = driver1UnassignBasePenaltyDiff + driver2UnassignBasePenaltyDiff + driver1AssignBasePenaltyDiff + driver2AssignBasePenaltyDiff;

            driver1WorkedTimeDiff = driver1UnassignShiftLengthDiff + driver1AssignShiftLengthDiff;
            driver2WorkedTimeDiff = driver2UnassignShiftLengthDiff + driver2AssignShiftLengthDiff;

            return (costDiff, costWithoutPenalty, basePenaltyDiff);
        }

        public override void Execute() {
            info.Assignment[trip2.Index] = driver1;
            info.Assignment[trip1.Index] = driver2;
            info.DriversWorkedTime[driver1.AllDriversIndex] += driver1WorkedTimeDiff;
            info.DriversWorkedTime[driver2.AllDriversIndex] += driver2WorkedTimeDiff;
        }

        public static SwapTripOperation CreateRandom(SaInfo info) {
            int tripIndex1 = info.FastRand.NextInt(info.Instance.Trips.Length);

            // Select random second trip that is not the first trip, and that isn't assigned to the same driver as the first trip
            int tripIndex2;
            do {
                tripIndex2 = info.FastRand.NextInt(info.Instance.Trips.Length);
            } while (tripIndex1 == tripIndex2 || info.Assignment[tripIndex1] == info.Assignment[tripIndex2]);

            return new SwapTripOperation(tripIndex1, tripIndex2, info);
        }
    }
}

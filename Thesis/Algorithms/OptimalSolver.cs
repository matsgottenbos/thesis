using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class OptimalSolver {
        readonly Instance instance;
        readonly SaInfo dummyInfo;

        public OptimalSolver(Instance instance) {
            this.instance = instance;

            dummyInfo = new SaInfo(instance, null, null);
        }

        public Solution Solve() {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Calculate minimum remaning driving costs after each assigned number of trips
            double[] minRemainingDrivingCosts = new double[instance.Trips.Length];
            float minSalaryRate = Config.InternalDriverDailySalaryRates.Select(salaryRate => salaryRate.SalaryRate).Min();
            for (int tripIndex = instance.Trips.Length - 1; tripIndex >= 1; tripIndex--) {
                float tripMinDrivingCost = instance.Trips[tripIndex].Duration * minSalaryRate;
                minRemainingDrivingCosts[tripIndex - 1] = minRemainingDrivingCosts[tripIndex] + tripMinDrivingCost;
            }

            (AssignmentNode bestNode, double _) = AssignmentDfs(minRemainingDrivingCosts);

            if (bestNode == null) {
                stopwatch.Stop();
                Console.WriteLine("No solution; determined in {0} s", stopwatch.ElapsedMilliseconds / 1000f);
                return null;
            }

            // Check cost
            (double bestNodeCost, _, double bestNodePenalty, _) = GetNodeCost(bestNode);
            if (bestNodePenalty > 0) {
                throw new Exception("Optimal algorithm returned an infeasible solution");
            }

            int tripCount = instance.Trips.Length;
            Driver[] bestAssignmentIndices = NodeToAssignment(bestNode);
            Driver[] bestAssignment = new Driver[tripCount];
            for (int tripIndex = 0; tripIndex < tripCount; tripIndex++) {
                Driver driver = bestAssignmentIndices[tripIndex];
                bestAssignment[tripIndex] = instance.AllDrivers[driver.AllDriversIndex];
            }

            stopwatch.Stop();
            Console.WriteLine("Optimal solution found in {0} s", stopwatch.ElapsedMilliseconds / 1000f);

            return new Solution(bestNodeCost, bestAssignment);
        }

        (AssignmentNode, double) AssignmentDfs(double[] minRemainingDrivingCosts, AssignmentNode node = null, double nodeCost = 0, int newTripIndex = 0, double costUpperBound = double.MaxValue, string bestAssignmentStr = "-") {
            if (newTripIndex == instance.Trips.Length) {
                return (node, nodeCost);
            }

            // Logging progress
            if (newTripIndex < 4) {
                string logStr = "";
                AssignmentNode searchNode = node;
                while (searchNode != null) {
                    int hotelIndicator = node.IsHotelStayAfter ? 1 : 0;
                    logStr = string.Format("{0} / {1}  |  ", 2 * searchNode.DriverIndex + hotelIndicator, 2 * instance.AllDrivers.Length) + logStr;
                    searchNode = searchNode.Prev;
                }

                string bestCostStr = costUpperBound < double.MaxValue ? ParseHelper.ToString(costUpperBound) : "-";
                Console.WriteLine("{0}  |  {1}  |  {2}", bestCostStr, bestAssignmentStr, logStr);
            }

            double bestNodeCost = costUpperBound;
            AssignmentNode bestNode = null;
            string newBestAssignmentStr = bestAssignmentStr;
            for (int hotelIndicator = 0; hotelIndicator < 2; hotelIndicator++) {
                for (int driverIndex = 0; driverIndex < instance.AllDrivers.Length; driverIndex++) {
                    bool isHotelStayAfter = hotelIndicator == 1;
                    AssignmentNode newNode = new AssignmentNode(newTripIndex, driverIndex, isHotelStayAfter, node);

                    // Check feasibility and cost
                    double? newNodeCostDiff = GetAdditionCostDiffIfFeasible(newNode);
                    if (!newNodeCostDiff.HasValue) continue;

                    double newNodeCost = nodeCost + newNodeCostDiff.Value;
                    double newNodeMinFinalCost = newNodeCost + minRemainingDrivingCosts[newTripIndex];
                    if (newNodeMinFinalCost > costUpperBound) continue;

                    (AssignmentNode dfsResultNode, double dfsResultCost) = AssignmentDfs(minRemainingDrivingCosts, newNode, newNodeCost, newTripIndex + 1, bestNodeCost, newBestAssignmentStr);
                    if (dfsResultCost < bestNodeCost) {
                        bestNodeCost = dfsResultCost;
                        bestNode = dfsResultNode;
                        Driver[] bestAssignment = NodeToAssignment(bestNode);
                        newBestAssignmentStr = string.Join(' ', bestAssignment.Select(driver => driver.GetId()));
                    }
                }
            }

            // Logging progress
            if (newTripIndex == 0) {
                int tripOptionCount = 2 * instance.AllDrivers.Length;
                Console.WriteLine("{0} / {1}", tripOptionCount, tripOptionCount);
            }

            return (bestNode, bestNodeCost);
        }

        double? GetAdditionCostDiffIfFeasible(AssignmentNode node) {
            throw new NotImplementedException("TODO: include hotel stays in cost diff");

            Driver driver = instance.AllDrivers[node.DriverIndex];
            Trip nodeTrip = instance.Trips[node.TripIndex];

            // Get driver's trip before this one, and driver's first trip of the shift
            Trip lastTripInternal = nodeTrip;
            Trip firstTripInternal = null;
            Trip prevTripInternal = null;
            Trip prevShiftLastTrip = null;
            Trip driverPrevSearchTrip = nodeTrip;
            AssignmentNode searchNode = node.Prev;
            int startTimeThreshold = 0;
            double? costDiff = null;
            while (searchNode != null) {
                Trip searchTrip = instance.Trips[searchNode.TripIndex];
                if (searchTrip.StartTime < startTimeThreshold) {
                    // No more trips in this shift
                    break;
                }

                if (searchNode.DriverIndex == node.DriverIndex) {
                    // Check precedence
                    if (!instance.IsValidPrecedence(searchTrip, nodeTrip)) return null;

                    if (instance.AreSameShift(searchTrip, driverPrevSearchTrip)) {
                        if (prevTripInternal == null) {
                            // This is the trip before, store it
                            prevTripInternal = searchTrip;
                        }
                    } else {
                        // This is a different shift
                        if (firstTripInternal == null) {
                            // This is the beginning of the current trip; determine and check shift length
                            firstTripInternal = driverPrevSearchTrip;
                            prevShiftLastTrip = searchTrip;
                            costDiff = GetShiftCostDiff(driverPrevSearchTrip, lastTripInternal, prevTripInternal, driver);
                        } else {
                            // This is the beginning of the previous trip; no need to continue
                            break;
                        }
                    }

                    driverPrevSearchTrip = searchTrip;
                    startTimeThreshold = searchTrip.StartTime - Config.BetweenShiftsMaxStartTimeDiff;
                }

                searchNode = searchNode.Prev;
            }

            // Get cost diff if not already determined
            if (firstTripInternal == null) {
                // We were at the beginning of the current shift; check shift length, and then the check is complete
                costDiff = GetShiftCostDiff(driverPrevSearchTrip, lastTripInternal, prevTripInternal, driver);
            } else {
                // We were at the beginning of the previous shift; check rest time, and then the check is complete
                if (driver.RestTimeWithPickup(driverPrevSearchTrip, prevShiftLastTrip, firstTripInternal) < Config.MinRestTime) return null;
            }

            if (!costDiff.HasValue) return null;

            // Check contract time
            if (!CheckDriverMaxContractTime(node, nodeTrip, driver)) return null;
            if (!CheckMinContractTime(node)) return null;

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                (_, _, double debugNodePenalty, _) = GetNodeCost(node);
                if (debugNodePenalty > 0) {
                    throw new Exception();
                }
            }
            #endif

            return costDiff;
        }

        double? GetShiftCostDiff(Trip firstTripInternal, Trip lastTripInternal, Trip prevTripInternal, Driver driver) {
            // Get new shift length
            int newShiftLength = driver.ShiftLengthWithPickup(firstTripInternal, lastTripInternal);

            // Check shift length
            if (newShiftLength > Config.MaxShiftLengthWithTravel) return null;

            // Get new shift cost
            float newShiftCost = driver.ShiftCostWithPickup(firstTripInternal, lastTripInternal);

            float shiftCostDiff;
            if (prevTripInternal == null) {
                // There is only one trip in this shift, so the shift cost is the diff
                shiftCostDiff = newShiftCost;
            } else {
                // Determine difference with previous shift cost
                shiftCostDiff = newShiftCost - driver.ShiftCostWithPickup(firstTripInternal, prevTripInternal);
            }

            return shiftCostDiff;
        }

        /** Check that this driver doesn't exceed his maximum contract time */
        bool CheckDriverMaxContractTime(AssignmentNode node, Trip nodeTrip, Driver driver) {
            int driverWorkedTime = 0;
            AssignmentNode searchNode = node.Prev;
            Trip driverPrevSearchTrip = nodeTrip;
            Trip lastTripInternal = nodeTrip;
            while (searchNode != null) {
                Trip searchTrip = instance.Trips[searchNode.TripIndex];

                if (searchNode.DriverIndex == node.DriverIndex) {
                    if (!instance.AreSameShift(searchTrip, driverPrevSearchTrip)) {
                        // End the shift
                        driverWorkedTime += driver.ShiftLengthWithPickup(driverPrevSearchTrip, lastTripInternal);
                        lastTripInternal = searchTrip;
                    }
                    driverPrevSearchTrip = searchTrip;
                }

                searchNode = searchNode.Prev;
            }

            // End first shift
            driverWorkedTime += driver.ShiftLengthWithPickup(driverPrevSearchTrip, lastTripInternal);
            if (driver.GetMaxContractTimeViolation(driverWorkedTime) > 0) return false;
            return true;
        }

        /** Check if minimum contract time can still be achieved for all drivers */
        bool CheckMinContractTime(AssignmentNode node) {
            // Only check for the last two trips; before that, hardly any branches can be cut
            int tripsLeftToAssign = instance.Trips.Length - node.TripIndex - 1;
            if (tripsLeftToAssign > 2) return true;

            int[] allDriversWorkedTime = new int[instance.AllDrivers.Length];
            Trip[] allDriversPrevSearchTrip = new Trip[instance.AllDrivers.Length];
            Trip[] allDriversLastTripInternal = new Trip[instance.AllDrivers.Length];
            AssignmentNode searchNode = node;
            while (searchNode != null) {
                Trip searchTrip = instance.Trips[searchNode.TripIndex];
                Trip driverPrevSearchTrip = allDriversPrevSearchTrip[searchNode.DriverIndex];

                if (driverPrevSearchTrip == null) {
                    allDriversLastTripInternal[searchNode.DriverIndex] = searchTrip;
                } else {
                    Trip lastTripInternal = allDriversLastTripInternal[searchNode.DriverIndex];
                    Driver driver = instance.AllDrivers[searchNode.DriverIndex];
                    if (!instance.AreSameShift(searchTrip, driverPrevSearchTrip)) {
                        // End shift for driver
                        allDriversWorkedTime[searchNode.DriverIndex] += driver.ShiftLengthWithPickup(driverPrevSearchTrip, lastTripInternal);
                        allDriversLastTripInternal[searchNode.DriverIndex] = searchTrip;
                    }
                }

                allDriversPrevSearchTrip[searchNode.DriverIndex] = searchTrip;
                searchNode = searchNode.Prev;
            }

            // End shift for all drivers
            int minTimeViolations = 0;
            for (int driverIndex = 0; driverIndex < instance.AllDrivers.Length; driverIndex++) {
                Driver driver = instance.AllDrivers[driverIndex];
                Trip driverPrevSearchTrip = allDriversPrevSearchTrip[driverIndex];
                if (driverPrevSearchTrip == null) {
                    // Driver has no assigned trips
                    if (driver.GetMinContractTimeViolation(0) > 0) {
                        minTimeViolations++;
                        if (minTimeViolations > tripsLeftToAssign) return false;
                    }
                    continue;
                }

                Trip lastTripInternal = allDriversLastTripInternal[driverIndex];
                int driverWorkedTime = allDriversWorkedTime[driverIndex] + driver.ShiftLengthWithPickup(driverPrevSearchTrip, lastTripInternal);

                // Check minimum contract time
                if (driver.GetMinContractTimeViolation(driverWorkedTime) > 0) {
                    minTimeViolations++;
                    if (minTimeViolations > tripsLeftToAssign) return false;
                }
            }

            return true;
        }

        (double, double, double, int[]) GetNodeCost(AssignmentNode node) {
            dummyInfo.Assignment = NodeToAssignment(node);
            (double cost, double costWithoutPenalty, double penalty, int[] driversWorkedTime, _, _, _, _, _) =  TotalCostCalculator.GetAssignmentCost(dummyInfo);
            return (cost, costWithoutPenalty, penalty, driversWorkedTime);
        }

        Driver[] NodeToAssignment(AssignmentNode node) {
            Driver[] assignment = new Driver[node.TripIndex + 1];
            for (int tripIndex = node.TripIndex; tripIndex >= 0; tripIndex--) {
                assignment[tripIndex] = instance.AllDrivers[node.DriverIndex];
                node = node.Prev;
            }
            return assignment;
        }
    }

    class Solution {
        public readonly double Cost;
        public readonly Driver[] Assignment;

        public Solution(double cost, Driver[] assignment) {
            Cost = cost;
            Assignment = assignment;
        }
    }

    class AssignmentNode {
        public readonly int TripIndex, DriverIndex;
        public readonly bool IsHotelStayAfter;
        public readonly AssignmentNode Prev;

        public AssignmentNode(int tripIndex, int assignedDriverIndex, bool isHotelStayAfter, AssignmentNode prev) {
            TripIndex = tripIndex;
            DriverIndex = assignedDriverIndex;
            IsHotelStayAfter = isHotelStayAfter;
            Prev = prev;
        }
    }
}

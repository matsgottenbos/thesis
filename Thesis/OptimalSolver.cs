using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class OptimalSolver {
        readonly Instance instance;

        public OptimalSolver(Instance instance) {
            this.instance = instance;
        }

        public Solution Solve() {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Calculate minimum remaning driving costs after each assigned number of trips
            double[] minRemainingDrivingCosts = new double[instance.Trips.Length];
            for (int tripIndex = instance.Trips.Length - 1; tripIndex >= 1; tripIndex--) {
                float tripMinDrivingCost = instance.Trips[tripIndex].DrivingCost;
                minRemainingDrivingCosts[tripIndex - 1] = minRemainingDrivingCosts[tripIndex] + tripMinDrivingCost;
            }

            (AssignmentNode bestNode, double _) = AssignmentDfs(minRemainingDrivingCosts);

            if (bestNode == null) {
                stopwatch.Stop();
                Console.WriteLine("No solution; determined in {0} s", stopwatch.ElapsedMilliseconds / 1000f);
                return null;
            }

            // Check cost
            double bestNodeCost = GetNodeCost(bestNode);

            int tripCount = instance.Trips.Length;
            int[] bestAssignmentIndices = NodeToAssignmentIndices(bestNode);
            Driver[] bestAssignment = new Driver[tripCount];
            for (int tripIndex = 0; tripIndex < tripCount; tripIndex++) {
                int driverIndex = bestAssignmentIndices[tripIndex];
                bestAssignment[tripIndex] = instance.Drivers[driverIndex];
            }

            stopwatch.Stop();
            Console.WriteLine("Optimal solution found in {0} s", stopwatch.ElapsedMilliseconds / 1000f);

            return new Solution(bestNodeCost, bestAssignment);
        }

        (AssignmentNode, double) AssignmentDfs(double[] minRemainingDrivingCosts, AssignmentNode node = null, double nodeCost = 0, int newTripIndex = 0, double costUpperBound = double.MaxValue) {
            if (newTripIndex == instance.Trips.Length) {
                return (node, nodeCost);
            }

            // Logging progress
            if (newTripIndex < 5) {
                string logStr = "";
                AssignmentNode searchNode = node;
                while (searchNode != null) {
                    logStr = string.Format("{0} / {1}  |  ", searchNode.DriverIndex, Config.GenDriverCount) + logStr;
                    searchNode = searchNode.Prev;
                }
                Console.WriteLine(Math.Round(costUpperBound, 4) + "  |  " + logStr);
            }

            double bestNodeCost = costUpperBound;
            AssignmentNode bestNode = null;
            for (int driverIndex = 0; driverIndex < Config.GenDriverCount; driverIndex++) {
                AssignmentNode newNode = new AssignmentNode(newTripIndex, driverIndex, node);

                // Check feasibility and cost
                double? newNodeCostDiff = GetAdditionCostDiffIfFeasible(newNode);
                if (!newNodeCostDiff.HasValue) continue;

                double newNodeCost = nodeCost + newNodeCostDiff.Value;
                double newNodeMinFinalCost = newNodeCost + minRemainingDrivingCosts[newTripIndex];
                if (newNodeMinFinalCost > costUpperBound) continue;

                (AssignmentNode dfsResultNode, double dfsResultCost) = AssignmentDfs(minRemainingDrivingCosts, newNode, newNodeCost, newTripIndex + 1, bestNodeCost);
                if (dfsResultCost < bestNodeCost) {
                    bestNodeCost = dfsResultCost;
                    bestNode = dfsResultNode;
                }
            }

            // Logging progress
            if (newTripIndex == 0) {
                Console.WriteLine("{0} / {1}", Config.GenDriverCount, Config.GenDriverCount);
            }

            return (bestNode, bestNodeCost);
        }

        double? GetAdditionCostDiffIfFeasible(AssignmentNode node) {
            int nodeDriverIndex = node.DriverIndex;
            Driver driver = instance.Drivers[nodeDriverIndex];
            Trip nodeTrip = instance.Trips[node.TripIndex];
            int nodeDayIndex = nodeTrip.DayIndex;

            // Get driver's trip before this one, and driver's first trip of the day
            Trip driverSameDayTripBefore = null;
            Trip driverFirstSameDayTrip = nodeTrip;
            AssignmentNode searchNode = node.Prev;
            while (searchNode != null) {
                Trip searchTrip = instance.Trips[searchNode.TripIndex];
                if (searchTrip.DayIndex != nodeDayIndex) break;

                if (searchNode.DriverIndex == nodeDriverIndex) {
                    if (driverSameDayTripBefore == null) {
                        if (!instance.TripSuccession[searchTrip.Index, nodeTrip.Index]) return null;
                        driverSameDayTripBefore = searchTrip;
                    }
                    driverFirstSameDayTrip = searchTrip;
                }
                searchNode = searchNode.Prev;
            }

            // Check working day length
            float workDayEndTime = CostHelper.WorkDayEndTimeWithoutTwoWayTravel(driverFirstSameDayTrip, nodeTrip, instance);
            float workingDayLength = workDayEndTime - CostHelper.WorkDayStartTimeWithTwoWayTravel(driverFirstSameDayTrip, driver);
            if (workingDayLength > Config.MaxWorkDayLength) return null;

            // Check contract hours
            if (!CheckDriverMaxContractHours(node, nodeTrip, driver)) return null;
            if (!CheckMinContractHours(node)) return null;

            // Get cost diff
            double workingTimeDiff;
            if (driverSameDayTripBefore == null) workingTimeDiff = workingDayLength;
            else workingTimeDiff = workDayEndTime - CostHelper.WorkDayEndTimeWithoutTwoWayTravel(driverFirstSameDayTrip, driverSameDayTripBefore, instance);

            double costDiff = workingTimeDiff * Config.HourlyRate;

            return costDiff;
        }

        /** Check that this driver doesn't exceed his max contract hours */
        bool CheckDriverMaxContractHours(AssignmentNode node, Trip nodeTrip, Driver driver) {
            float driverWorkedHours = 0;
            AssignmentNode searchNode = node.Prev;
            Trip driverFirstDayTrip = nodeTrip;
            Trip driverLastDayTrip = nodeTrip;
            while (searchNode != null) {
                Trip searchTrip = instance.Trips[searchNode.TripIndex];

                if (searchNode.DriverIndex == node.DriverIndex) {
                    if (searchTrip.DayIndex != driverFirstDayTrip.DayIndex) {
                        driverWorkedHours += CostHelper.WorkDayLength(driverFirstDayTrip, driverLastDayTrip, driver, instance);
                        driverLastDayTrip = searchTrip;
                    }
                    driverFirstDayTrip = searchTrip;
                }

                searchNode = searchNode.Prev;
            }

            driverWorkedHours += CostHelper.WorkDayLength(driverFirstDayTrip, driverLastDayTrip, driver, instance);
            if (driverWorkedHours > driver.MaxWorkedHours) return false;
            return true;
        }

        /** Check if minimum contract hours can still be achieved for all drivers */
        bool CheckMinContractHours(AssignmentNode node) {
            // Only check for the last two trips; before that, hardly any branches can be cut
            if (node.TripIndex < instance.Trips.Length - 2) return true;

            int tripsLeftToAssign = instance.Trips.Length - node.TripIndex + 1;
            float[] allDriversWorkedHours = new float[instance.Drivers.Length];
            Trip[] allDriversFirstDayTrip = new Trip[instance.Drivers.Length];
            Trip[] allDriversLastDayTrip = new Trip[instance.Drivers.Length];
            AssignmentNode searchNode = node;
            while (searchNode != null) {
                Trip searchTrip = instance.Trips[searchNode.TripIndex];

                Trip currentDriverFirstDayTrip = allDriversFirstDayTrip[searchNode.DriverIndex];

                if (currentDriverFirstDayTrip == null) {
                    allDriversLastDayTrip[searchNode.DriverIndex] = searchTrip;
                } else {
                    Trip currentDriverLastDayTrip = allDriversLastDayTrip[searchNode.DriverIndex];
                    Driver currentDriver = instance.Drivers[searchNode.DriverIndex];
                    if (searchTrip.DayIndex != currentDriverFirstDayTrip.DayIndex) {
                        allDriversWorkedHours[searchNode.DriverIndex] += CostHelper.WorkDayLength(currentDriverFirstDayTrip, currentDriverLastDayTrip, currentDriver, instance);
                        allDriversLastDayTrip[searchNode.DriverIndex] = searchTrip;
                    }
                }

                allDriversFirstDayTrip[searchNode.DriverIndex] = searchTrip;

                searchNode = searchNode.Prev;
            }

            // End day for all drivers
            int minHoursViolations = 0;
            for (int driverIndex = 0; driverIndex < instance.Drivers.Length; driverIndex++) {
                Driver currentDriver = instance.Drivers[driverIndex];
                Trip currentDriverFirstDayTrip = allDriversFirstDayTrip[driverIndex];
                if (currentDriverFirstDayTrip == null) {
                    // Driver has no assigned trips
                    if (currentDriver.MinWorkedHours > 0) {
                        minHoursViolations++;
                        if (minHoursViolations > tripsLeftToAssign) return false;
                    }
                    continue;
                }

                Trip currentDriverLastDayTrip = allDriversLastDayTrip[driverIndex];
                float currentDriverWorkedHours = allDriversWorkedHours[driverIndex] + CostHelper.WorkDayLength(currentDriverFirstDayTrip, currentDriverLastDayTrip, currentDriver, instance);

                // Check minimum contract hours
                if (currentDriverWorkedHours < currentDriver.MinWorkedHours) {
                    minHoursViolations++;
                    if (minHoursViolations > tripsLeftToAssign) return false;
                }
            }

            return true;
        }

        double GetNodeCost(AssignmentNode node) {
            int[] assignmentIndices = NodeToAssignmentIndices(node);
            return CostHelper.AssignmentCostWithoutPenalties(assignmentIndices, instance);
        }

        int[] NodeToAssignmentIndices(AssignmentNode node) {
            int[] assignment = new int[node.TripIndex + 1];
            for (int tripIndex = node.TripIndex; tripIndex >= 0; tripIndex--) {
                assignment[tripIndex] = node.DriverIndex;
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
        public readonly AssignmentNode Prev;

        public AssignmentNode(int tripIndex, int assignedDriverIndex, AssignmentNode prev) {
            TripIndex = tripIndex;
            DriverIndex = assignedDriverIndex;
            Prev = prev;
        }
    }
}

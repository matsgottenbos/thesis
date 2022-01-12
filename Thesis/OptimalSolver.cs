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

            // Check cost
            double bestNodeCost = GetNodeCost(bestNode);

            if (bestNode == null) {
                stopwatch.Stop();
                Console.WriteLine("No solution; determined in {0} s", stopwatch.ElapsedMilliseconds / 1000f);
                return null;
            }

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

            // Debug
            if (newTripIndex < 5) {
                string logStr = "";
                AssignmentNode searchNode = node;
                while (searchNode != null) {
                    logStr = string.Format("{0} / {1}  |  ", searchNode.DriverIndex, Config.DriverCount) + logStr;
                    searchNode = searchNode.Prev;
                }
                Console.WriteLine(Math.Round(costUpperBound, 4) + "  |  " + logStr);
            }

            double bestNodeCost = costUpperBound;
            AssignmentNode bestNode = null;
            for (int driverIndex = 0; driverIndex < Config.DriverCount; driverIndex++) {
                AssignmentNode newNode = new AssignmentNode(newTripIndex, driverIndex, node);

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

            // Debug
            if (newTripIndex == 0) {
                Console.WriteLine("{0} / {1}", Config.DriverCount, Config.DriverCount);
            }

            return (bestNode, bestNodeCost);
        }

        double? GetAdditionCostDiffIfFeasible(AssignmentNode node) {
            int nodeDriverIndex = node.DriverIndex;
            Driver driver = instance.Drivers[nodeDriverIndex];
            Trip nodeTrip = instance.Trips[node.TripIndex];
            int nodeDayIndex = nodeTrip.DayIndex;
            Trip driverSameDayTripBefore = null;
            Trip driverFirstDayTrip = nodeTrip;

            // Get driver's trip before this one, and driver's first trip of the day
            AssignmentNode searchNode = node.Prev;
            while (searchNode != null) {
                Trip searchNodeTrip = instance.Trips[searchNode.TripIndex];
                if (searchNodeTrip.DayIndex != nodeDayIndex) break;

                if (searchNode.DriverIndex == nodeDriverIndex) {
                    if (driverSameDayTripBefore == null) {
                        // Check precedence
                        if (!instance.TripSuccession[searchNodeTrip.Index, nodeTrip.Index]) return null;

                        driverSameDayTripBefore = searchNodeTrip;
                    }

                    driverFirstDayTrip = searchNodeTrip;
                }

                searchNode = searchNode.Prev;
            }

            // Check working day length
            float workDayEndTime = CostHelper.WorkDayEndTimeWithoutTwoWayTravel(driverFirstDayTrip, nodeTrip, instance);
            float workingDayLength = workDayEndTime - CostHelper.WorkDayStartTimeWithTwoWayTravel(driverFirstDayTrip, driver);
            if (workingDayLength > Config.MaxWorkDayLength) return null;

            // Get cost diff
            double workingTimeDiff;
            if (driverSameDayTripBefore == null) workingTimeDiff = workingDayLength;
            else workingTimeDiff = workDayEndTime - CostHelper.WorkDayEndTimeWithoutTwoWayTravel(driverFirstDayTrip, driverSameDayTripBefore, instance);

            double costDiff = workingTimeDiff * Config.HourlyRate;

            return costDiff;
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

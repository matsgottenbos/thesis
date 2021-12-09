using System;
using System.Collections.Generic;
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
            List<AssignmentNode> possibleNodes = EnumerateAssignments();

            int tripCount = instance.Trips.Length;
            double bestCost = int.MaxValue;
            AssignmentNode bestNode = null;
            for (int i = 0; i < possibleNodes.Count; i++) {
                AssignmentNode node = possibleNodes[i];
                double cost = GetNodeCost(node, tripCount);
                if (cost < bestCost) {
                    bestCost = cost;
                    bestNode = node;
                }
            }

            int[] bestAssignmentIndices = NodeToAssignmentIndices(bestNode, tripCount);
            Driver[] bestAssignment = new Driver[tripCount];
            for (int tripIndex = 0; tripIndex < tripCount; tripIndex++) {
                int driverIndex = bestAssignmentIndices[tripIndex];
                bestAssignment[tripIndex] = instance.Drivers[driverIndex];
            }

            return new Solution(bestCost, bestAssignment);
        }
        

        List<AssignmentNode> EnumerateAssignments() {
            List<AssignmentNode> previousTripNodes = new List<AssignmentNode>() { null };
            List<List<AssignmentNode>> nodesPerTrip = new List<List<AssignmentNode>>();
            for (int tripIndex = 0; tripIndex < instance.Trips.Length; tripIndex++) {
                List<AssignmentNode> currentTripNodes = new List<AssignmentNode>();
                for (int previousNodeIndex = 0; previousNodeIndex < previousTripNodes.Count; previousNodeIndex++) {
                    AssignmentNode previousTripNode = previousTripNodes[previousNodeIndex];
                    for (int driverIndex = 0; driverIndex < Config.DriverCount; driverIndex++) {
                        AssignmentNode node = new AssignmentNode(driverIndex, previousTripNode);
                        if (IsNodeFeasible(node, tripIndex)) currentTripNodes.Add(node);
                    }
                }

                previousTripNodes = currentTripNodes;
                nodesPerTrip.Add(currentTripNodes);
            }

            return previousTripNodes;
        }

        bool IsNodeFeasible(AssignmentNode node, int assignedTripCount) {
            int[] assignment = NodeToAssignmentIndices(node, assignedTripCount);
            Trip[] driversPreviousTrip = new Trip[Config.DriverCount];

            for (int tripIndex = 0; tripIndex < assignment.Length; tripIndex++) {
                int driverIndex = assignment[tripIndex];
                Trip trip = instance.Trips[tripIndex];
                Trip driverIndexPreviousTrip = driversPreviousTrip[driverIndex];

                if (driverIndexPreviousTrip == null || driverIndexPreviousTrip.Successors.Contains(trip)) {
                    driversPreviousTrip[driverIndex] = trip;
                } else {
                    // These trips cannot be scheduled consecutively
                    return false;
                }
            }

            return true;
        }

        double GetNodeCost(AssignmentNode node, int assignedTripCount) {
            int[] assignmentIndices = NodeToAssignmentIndices(node, assignedTripCount);
            return CostHelper.AssignmentCostWithoutPenalties(assignmentIndices, instance);
        }

        int[] NodeToAssignmentIndices(AssignmentNode node, int assignedTripCount) {
            int[] assignment = new int[assignedTripCount];
            for (int tripIndex = assignedTripCount - 1; tripIndex >= 0; tripIndex--) {
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
        public readonly int DriverIndex;
        public readonly AssignmentNode Prev;

        public AssignmentNode(int driverIndex, AssignmentNode prev) {
            DriverIndex = driverIndex;
            Prev = prev;
        }
    }
}

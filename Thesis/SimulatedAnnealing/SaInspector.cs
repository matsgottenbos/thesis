using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Thesis {
    class SaInspector {
        readonly Instance instance;
        readonly SaInfo dummyInfo;

        public SaInspector(Instance instance) {
            this.instance = instance;

            dummyInfo = new SaInfo(instance, null, null);
            dummyInfo.PenaltyFactor = 1f;

            Stopwatch stopwatch = new Stopwatch();
            Random rand = new Random(1);

            int tripCount = 500;
            int driverCount = 30;

            // Test array
            int[] arr = new int[tripCount];
            stopwatch.Start();
            for (int i = 0; i < 10000000; i++) {
                int tripIndex = rand.Next(tripCount);
                int driverIndex = rand.Next(driverCount);

                int? prevTripIndex = null;
                for (int searchTripIndex = driverIndex; searchTripIndex >= 0; searchTripIndex--) {
                    prevTripIndex = searchTripIndex;
                }

                int? nextTripIndex = null;
                for (int searchTripIndex = driverIndex; searchTripIndex < arr.Length; searchTripIndex++) {
                    nextTripIndex = searchTripIndex;
                }

                arr[tripIndex] = driverIndex;
            }
            stopwatch.Stop();
            Console.WriteLine("Array: {0}", stopwatch.ElapsedMilliseconds);

            // Test lists
            int[] arr2 = new int[tripCount];
            List<int>[] lists = new List<int>[driverCount];
            for (int i = 0; i < lists.Length; i++) lists[i] = new List<int>();
            stopwatch.Start();
            for (int i = 0; i < 10000000; i++) {
                int tripIndex = rand.Next(tripCount);
                int driverIndex = rand.Next(driverCount);

                int oldDriverIndex = arr2[tripIndex];

                lists[oldDriverIndex].Remove(tripIndex);

                List<int> list = lists[driverIndex];
                for (int j = 0; j < list.Count; j++) {
                    if (list[j] < driverIndex) {
                        list.Insert(j, driverIndex);
                        break;
                    }
                }
                arr2[tripIndex] = driverIndex;
            }
            stopwatch.Stop();
            Console.WriteLine("Lists: {0}", stopwatch.ElapsedMilliseconds);

            // Test objs
            int[] arr3 = new int[tripCount];
            DebugObj[] objs = new DebugObj[driverCount];
            for (int i = 0; i < arr3.Length; i++) objs[0] = new DebugObj(i, objs[0]);
            stopwatch.Start();
            for (int i = 0; i < 10000000; i++) {
                int tripIndex = rand.Next(tripCount);
                int driverIndex = rand.Next(driverCount);

                int oldDriverIndex = arr3[tripIndex];
                DebugObj oldObj = objs[oldDriverIndex];
                while (true) {
                    if (oldObj.Prev == null) break;
                    if (oldObj.Prev.Value == tripIndex) {
                        oldObj.Prev = oldObj.Prev.Prev;
                        break;
                    }
                    oldObj = oldObj.Prev;
                }

                DebugObj obj = objs[driverIndex];
                if (obj == null) {
                    objs[driverIndex] = new DebugObj(tripIndex, null);
                } else {
                    while (true) {
                        if (obj.Prev == null || obj.Prev.Value < tripIndex) {
                            obj.Prev = new DebugObj(tripIndex, obj.Prev);
                            break;
                        }
                        obj = obj.Prev;
                    }
                }
                arr3[tripIndex] = driverIndex;
            }
            stopwatch.Stop();
            Console.WriteLine("Objs: {0}", stopwatch.ElapsedMilliseconds);


            Console.ReadLine();

            InspectAssignment("e0.0 0 1 3 4 e0.1 e1.0 0 3 0 2 1 e0.0 e0.1 4"); // Opt
            InspectAssignment("e0.0 1 2 3 4 0 0 e1.0 3 1 2 4 e0.0 1 0"); // SA

            Console.ReadLine();
        }

        void InspectAssignment(string assignmentStr) {
            dummyInfo.Assignment = assignmentStr.Split().Select(driverIndexStr => ParseDriver(driverIndexStr)).ToArray();

            (double cost, double costWithoutPenalty, double basePenalty, int[] driversWorkedTime) = TotalCostCalculator.GetAssignmentCost(dummyInfo);

            Console.WriteLine("Assignment: {0}\nCost: {1}\nCost without penalty: {2}\nBase penalty: {3}\n", assignmentStr, ParseHelper.ToString(cost), ParseHelper.ToString(costWithoutPenalty), ParseHelper.ToString(basePenalty));
        }

        Driver ParseDriver(string driverStr) {
            if (driverStr[0] == 'e') {
                // External driver
                int typeIndex = int.Parse(Regex.Replace(driverStr, @"e(\d+)\.(\d+)", "$1"));
                int indexInType = int.Parse(Regex.Replace(driverStr, @"e(\d+)\.(\d+)", "$2"));
                return instance.ExternalDriversByType[typeIndex][indexInType];
            } else {
                // Internal driver
                int internalDriverIndex = int.Parse(driverStr);
                return instance.InternalDrivers[internalDriverIndex];
            }
        }
    }

    class DebugObj {
        public readonly int Value;
        public DebugObj Prev;

        public DebugObj(int value, DebugObj prev) {
            Value = value;
            Prev = prev;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Thesis {
    class DebugClass {
        public int a, b, c, d, e, f, g, h, i, j;
    }

    struct DebugStruct {
        public int a, b, c, d, e, f, g, h, i, j;
    }

    class DebugInspector {
        readonly Instance instance;
        readonly SaInfo info;

        public DebugInspector(Instance instance) {
            this.instance = instance;

            info = new SaInfo(instance, null, null);

            Stopwatch stopwatch = new Stopwatch();

            // Method 1: 16721 ms
            // Method 2: 8644 ms
            // Method 3: 10207 ms
            // Method 2: 8557 ms
            // Method 3: 10131 ms
            // Method 1: 15524 ms

            stopwatch.Restart();
            for (int i = 0; i < 1000000000; i++) {
                (int a, int b, int c, int d, int e, int f, int g, int h, int i2, int j) = DebugMethod1();
            }
            stopwatch.Stop();
            Console.WriteLine("Method 1: {0} ms", stopwatch.ElapsedMilliseconds);

            stopwatch.Restart();
            for (int i = 0; i < 1000000000; i++) {
                DebugClass debugClass = DebugMethod2();
            }
            stopwatch.Stop();
            Console.WriteLine("Method 2: {0} ms", stopwatch.ElapsedMilliseconds);

            stopwatch.Restart();
            for (int i = 0; i < 1000000000; i++) {
                DebugStruct debugStruct = DebugMethod3();
            }
            stopwatch.Stop();
            Console.WriteLine("Method 3: {0} ms", stopwatch.ElapsedMilliseconds);

            stopwatch.Restart();
            for (int i = 0; i < 1000000000; i++) {
                DebugClass debugClass = DebugMethod2();
            }
            stopwatch.Stop();
            Console.WriteLine("Method 2: {0} ms", stopwatch.ElapsedMilliseconds);

            stopwatch.Restart();
            for (int i = 0; i < 1000000000; i++) {
                DebugStruct debugStruct = DebugMethod3();
            }
            stopwatch.Stop();
            Console.WriteLine("Method 3: {0} ms", stopwatch.ElapsedMilliseconds);

            stopwatch.Restart();
            for (int i = 0; i < 1000000000; i++) {
                (int a, int b, int c, int d, int e, int f, int g, int h, int i2, int j) = DebugMethod1();
            }
            stopwatch.Stop();
            Console.WriteLine("Method 1: {0} ms", stopwatch.ElapsedMilliseconds);

            Console.ReadLine();
        }

        static (int, int, int, int, int, int, int, int, int, int) DebugMethod1() {
            int a = 5;
            int b = 10;
            int c = 15;
            int d = 20;
            int e = 25;
            int f = 30;
            int g = 35;
            int h = 40;
            int i = 45;
            int j = 50;
            return (a, b, c, d, e, f, g, h, i, j);
        }

        static DebugClass DebugMethod2() {
            DebugClass debugClass = new DebugClass();
            debugClass.a = 5;
            debugClass.b = 10;
            debugClass.c = 15;
            debugClass.d = 20;
            debugClass.e = 25;
            debugClass.f = 30;
            debugClass.g = 35;
            debugClass.h = 40;
            debugClass.i = 45;
            debugClass.j = 50;
            return debugClass;
        }

        static DebugStruct DebugMethod3() {
            DebugStruct debugStruct = new DebugStruct();
            debugStruct.a = 5;
            debugStruct.b = 10;
            debugStruct.c = 15;
            debugStruct.d = 20;
            debugStruct.e = 25;
            debugStruct.f = 30;
            debugStruct.g = 35;
            debugStruct.h = 40;
            debugStruct.i = 45;
            debugStruct.j = 50;
            return debugStruct;
        }

        void InspectAssignment(string assignmentStr) {
            (info.Assignment, info.IsHotelStayAfterTrip) = ParseHelper.ParseAssignmentString(assignmentStr, instance);
            List<Trip>[] driverPaths = TotalCostCalculator.GetPathPerDriver(info);

            (info.Cost, info.CostWithoutPenalty, info.Penalty, info.DriversWorkedTime, info.DriversShiftCounts, info.PrecedenceViolationCount, info.ShiftLengthViolationCount, info.RestTimeViolationCount, info.ContractTimeViolationCount, info.ShiftCountViolationAmount, info.InvalidHotelCount) = TotalCostCalculator.GetAssignmentCost(info);

            // Log assignment info
            Console.WriteLine("Assignment: {0}", assignmentStr);
            Console.WriteLine("Cost: {0}", ParseHelper.ToString(info.Cost));
            Console.WriteLine("Cost without penalty: {0}", ParseHelper.ToString(info.CostWithoutPenalty));
            Console.WriteLine("Penalty: {0}", ParseHelper.GetPenaltyString(info));
            Console.WriteLine("Worked times: {0}", ParseHelper.ToString(info.DriversWorkedTime));
            Console.WriteLine("Shift counts: {0}", ParseHelper.ToString(info.DriversShiftCounts));
            Console.WriteLine("Sum of worked times: {0}", info.DriversWorkedTime.Sum());
            Console.WriteLine();

            // Log driver penalties
            for (int driverIndex = 0; driverIndex < instance.AllDrivers.Length; driverIndex++) {
                Driver driver = instance.AllDrivers[driverIndex];
                List<Trip> driverPath = driverPaths[driverIndex];
                (_, _, double driverPenalty, int driverWorkedTime, int driverShiftCount, int precedenceViolationCount, int shiftLengthViolationCount, int restTimeViolationCount, int contractTimeViolationCount, int shiftCountViolationAmount, int invalidHotelCount) = TotalCostCalculator.GetDriverPathCost(driverPath, info.IsHotelStayAfterTrip, driver, info, false);

                if (driverPenalty > 0) {
                    Console.WriteLine("Driver {0} penalty: {1}", driver.GetId(), ParseHelper.GetPenaltyString(driverPenalty, precedenceViolationCount, shiftLengthViolationCount, restTimeViolationCount, contractTimeViolationCount, shiftCountViolationAmount, invalidHotelCount));
                }
            }
            Console.WriteLine();

            // Log driver paths
            //for (int driverIndex = 0; driverIndex < driverPaths.Length; driverIndex++) {
            //    Driver driver = info.Instance.AllDrivers[driverIndex];
            //    Console.WriteLine("Driver {0}: {1}", driver.GetId(), ParseHelper.DriverPathToString(driverPaths[driverIndex], info));
            //}
            //Console.WriteLine();
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
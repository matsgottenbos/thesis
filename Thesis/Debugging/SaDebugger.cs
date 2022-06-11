using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class SaDebugger {
        static OperationInfo CurrentOperation = null;

        public static OperationInfo GetCurrentOperation() {
            return CurrentOperation;
        }

        public static OperationPart GetCurrentOperationPart() {
            return CurrentOperation.CurrentPart;
        }

        public static TotalInfo GetCurrentStageInfo() {
            return CurrentOperation.CurrentPart.GetCurrentStageInfo();
        }

        public static void NextIteration(SaInfo info) {
            CurrentOperation = new OperationInfo(info);
        }

        public static void ResetIteration(SaInfo info) {
            CurrentOperation = new OperationInfo(info);
        }
    }

    class OperationInfo {
        public string Description;
        public readonly List<OperationPart> Parts = new List<OperationPart>();
        public OperationPart CurrentPart = null;
        readonly SaInfo info;

        public OperationInfo(SaInfo info) {
            this.info = info;
        }

        public void StartPart(string partDescription, Driver driver) {
            if (CurrentPart != null) Parts.Add(CurrentPart);
            CurrentPart = new OperationPart(partDescription, this, driver, info);
        }
    }

    enum OperationPartStage {
        None,
        OldNormal,
        NewNormal,
        OldChecked,
        NewChecked,
    }

    class OperationPart {
        OperationPartStage stage;
        readonly TotalInfo oldNormalInfo, newNormalInfo, oldCheckedInfo, newCheckedInfo;
        readonly string description;
        readonly OperationInfo operation;
        readonly Driver driver;
        readonly SaInfo info;

        public OperationPart(string description, OperationInfo operation, Driver driver, SaInfo info) {
            this.description = description;
            this.operation = operation;
            this.driver = driver;
            this.info = info;
            oldNormalInfo = new TotalInfo();
            newNormalInfo = new TotalInfo();
            oldCheckedInfo = new TotalInfo();
            newCheckedInfo = new TotalInfo();
        }

        public void SetStage(OperationPartStage stage) {
            this.stage = stage;
        }

        public TotalInfo GetCurrentStageInfo() {
            return stage switch {
                OperationPartStage.OldNormal => oldNormalInfo,
                OperationPartStage.NewNormal => newNormalInfo,
                OperationPartStage.OldChecked => oldCheckedInfo,
                OperationPartStage.NewChecked => newCheckedInfo,
                _ => throw new Exception("Incorrect stage"),
            };
        }

        public void CheckErrors() {
            TotalInfoBasic normalDiff = newNormalInfo - oldNormalInfo;
            TotalInfoBasic checkedDiff = newCheckedInfo - oldCheckedInfo;
            if (!TotalInfoBasic.AreEqual(normalDiff, checkedDiff)) {
                TotalInfoBasic errorAmounts = normalDiff - checkedDiff;

                LogErrorHeader("Operation error");
                LogTotalInfo("Error amounts", errorAmounts, false, false);
                LogTotalInfo("Normal diff", normalDiff, true);
                LogTotalInfo("Checked diff", checkedDiff, true);
                LogTotalInfo("Old normal info", oldNormalInfo, false);
                LogTotalInfo("New normal info", newNormalInfo, false);
                LogTotalInfo("Old checked info", oldCheckedInfo, false);
                LogTotalInfo("New checked info", newCheckedInfo, false);

                Console.ReadLine();
                throw new Exception("Operation part calculations incorrect, see console");
            }
        }

        public void LogErrorHeader(string title) {
            Console.WriteLine("*** {0} in iteration {1} ***", title, info.IterationNum);
            Console.WriteLine("Current operation: {0}", operation.Description);
            for (int i = 0; i < operation.Parts.Count; i++) Console.WriteLine("Previous part: {0}", operation.Parts[i].description);
            Console.WriteLine("Current part: {0}", description);
        }

        public static void LogTotalInfo(string title, TotalInfoBasic totalInfo, bool isDiff, bool shouldLogZeros = true) {
            Console.WriteLine("\n* {0} *", title);
            totalInfo.Log(isDiff, shouldLogZeros);
        }

        public static string ParseValuePairs(List<(int, int)> valuePairs) {
            return string.Join(" ", valuePairs.Select(valuePair => string.Format("({0}|{1})", valuePair.Item1, valuePair.Item2)));
        }
    }

    class TotalInfo : TotalInfoBasic {
        readonly List<DebugShiftInfo> shifts;
        DebugShiftInfo currentShift;

        public TotalInfo() {
            shifts = new List<DebugShiftInfo>();
            currentShift = new DebugShiftInfo();
        }

        public void AddTrip(Trip trip, bool isPrecedenceViolationAfter, bool isInvalidHotelAfter) {
            currentShift.Trips.Add(new DebugTripInfo(trip, isPrecedenceViolationAfter, isInvalidHotelAfter));
        }

        public void EndShiftPart1(int? restTimeAfter, bool isHotelAfter, bool isInvalidHotelAfter) {
            currentShift.RestTimeAfter = restTimeAfter;
            currentShift.IsHotelAfter = isHotelAfter;
            currentShift.IsInvalidHotelAfter = isInvalidHotelAfter;
        }

        public void EndShiftPart2(ShiftInfo shiftInfo, int shiftLengthWithTravel, int travelTimeBefore, int travelTimeAfter) {
            currentShift.ShiftInfo = shiftInfo;
            currentShift.ShiftLengthWithTravel = shiftLengthWithTravel;
            currentShift.TravelTimeBefore = travelTimeBefore;
            currentShift.TravelTimeAfter = travelTimeAfter;
            shifts.Add(currentShift);
            currentShift = new DebugShiftInfo();
        }

        public override void Log(bool isDiff, bool shouldLogZeros = true) {
            base.Log(isDiff, shouldLogZeros);

            // Log path string
            for (int shiftIndex = 0; shiftIndex < shifts.Count; shiftIndex++) {
                DebugShiftInfo shiftInfo = shifts[shiftIndex];
                for (int shiftTripIndex = 0; shiftTripIndex < shiftInfo.Trips.Count; shiftTripIndex++) {
                    DebugTripInfo tripInfo = shiftInfo.Trips[shiftTripIndex];
                    Console.Write(tripInfo.Trip.Index);
                    if (shiftTripIndex + 1 < shiftInfo.Trips.Count) Console.Write("-");
                }
                Console.Write("|");
                if (shiftInfo.IsHotelAfter) Console.WriteLine("H|");
            }
            Console.WriteLine();

            // Log path info
            for (int shiftIndex = 0; shiftIndex < shifts.Count; shiftIndex++) {
                DebugShiftInfo shiftInfo = shifts[shiftIndex];
                Console.WriteLine("Shift {0}--{1} length with travel: {2}", shiftInfo.Trips[0].Trip.Index, shiftInfo.Trips[^1].Trip.Index, shiftInfo.ShiftLengthWithTravel);
                Console.WriteLine("Shift {0}--{1} length without travel: {2}", shiftInfo.Trips[0].Trip.Index, shiftInfo.Trips[^1].Trip.Index, shiftInfo.ShiftInfo.DrivingTime);
                if (shiftInfo.RestTimeAfter.HasValue) Console.WriteLine("Rest after shift {0}--{1}: {2}", shiftInfo.Trips[0].Trip.Index, shiftInfo.Trips[^1].Trip.Index, shiftInfo.RestTimeAfter);
                // Todo: add more shift info

                for (int shiftTripIndex = 0; shiftTripIndex < shiftInfo.Trips.Count; shiftTripIndex++) {
                    DebugTripInfo tripInfo = shiftInfo.Trips[shiftTripIndex];
                    // Todo: add trip info
                }
            }
        }
    }

    class TotalInfoBasic {
        public DriverInfo DriverInfo;

        public void SetDriverInfo(DriverInfo driverInfo) {
            DriverInfo = driverInfo;
        }

        public virtual void Log(bool isDiff, bool shouldLogZeros = true) {
            DriverInfo.DebugLog(isDiff, shouldLogZeros);
        }

        public static bool AreEqual(TotalInfoBasic a, TotalInfoBasic b) {
            return DriverInfo.AreEqual(a.DriverInfo, b.DriverInfo);
        }


        public static TotalInfoBasic operator -(TotalInfoBasic a) {
            return new TotalInfoBasic() {
                DriverInfo = -a.DriverInfo,
            };
        }
        public static TotalInfoBasic operator +(TotalInfoBasic a, TotalInfoBasic b) {
            return new TotalInfoBasic() {
                DriverInfo = a.DriverInfo + b.DriverInfo,
            };
        }
        public static TotalInfoBasic operator -(TotalInfoBasic a, TotalInfoBasic b) => a + -b;
    }

    class DebugShiftInfo {
        public List<DebugTripInfo> Trips;
        public ShiftInfo ShiftInfo;
        public int ShiftLengthWithTravel, TravelTimeBefore, TravelTimeAfter;
        public int? RestTimeAfter;
        public bool IsHotelAfter, IsInvalidHotelAfter;

        public DebugShiftInfo() {
            Trips = new List<DebugTripInfo>();
        }
    }

    class DebugTripInfo {
        public Trip Trip;
        public bool IsPrecedenceViolationAfter, IsInvalidHotelAfter;

        public DebugTripInfo(Trip trip, bool isPrecedenceViolation, bool isInvalidHotel) {
            Trip = trip;
            IsPrecedenceViolationAfter = isPrecedenceViolation;
            IsInvalidHotelAfter = isInvalidHotel;
        }
    }




    class CheckedTotal {
        public readonly TotalInfoBasic Total = new TotalInfoBasic();
        public string DriverPathString = "";
        public List<(int, int)> ShiftLengths = new List<(int, int)>();
        public List<int> RestTimes = new List<int>();

        public void Log() {
            Console.WriteLine("Driver path: {0}", DriverPathString);
            Console.WriteLine("Shift lengths: {0}", OperationPart.ParseValuePairs(ShiftLengths));
            Console.WriteLine("Rest times: {0}", ParseHelper.ToString(RestTimes));
            Console.WriteLine("Worked time: {0}", ShiftLengths.Select(shiftLengthPair => shiftLengthPair.Item1).Sum());
            Total.Log(false);
        }
    }
}

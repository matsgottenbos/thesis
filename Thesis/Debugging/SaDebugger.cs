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

        public static DebugTotalInfo GetCurrentStageInfo() {
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
        readonly DebugTotalInfo oldNormalInfo, newNormalInfo, oldCheckedInfo, newCheckedInfo;
        readonly string description;
        readonly OperationInfo operation;
        readonly Driver driver;
        readonly SaInfo info;

        public OperationPart(string description, OperationInfo operation, Driver driver, SaInfo info) {
            this.description = description;
            this.operation = operation;
            this.driver = driver;
            this.info = info;
            oldNormalInfo = new DebugTotalInfo();
            newNormalInfo = new DebugTotalInfo();
            oldCheckedInfo = new DebugTotalInfo();
            newCheckedInfo = new DebugTotalInfo();
        }

        public void SetStage(OperationPartStage stage) {
            this.stage = stage;
        }

        public DebugTotalInfo GetCurrentStageInfo() {
            return stage switch {
                OperationPartStage.OldNormal => oldNormalInfo,
                OperationPartStage.NewNormal => newNormalInfo,
                OperationPartStage.OldChecked => oldCheckedInfo,
                OperationPartStage.NewChecked => newCheckedInfo,
                _ => throw new Exception("Incorrect stage"),
            };
        }

        public void CheckDriverErrors() {
            // Check diffs
            SaDriverInfo normalDiff = newNormalInfo.DriverInfo - oldNormalInfo.DriverInfo;
            SaDriverInfo checkedDiff = newCheckedInfo.DriverInfo - oldCheckedInfo.DriverInfo;
            if (!SaDriverInfo.AreEqual(normalDiff, checkedDiff)) {
                SaDriverInfo errorAmounts = normalDiff - checkedDiff;

                LogErrorHeader("Operation error: incorrect operation diff");
                LogInfo("Error amounts", errorAmounts, false, false);
                LogInfo("Normal diff", normalDiff, true);
                LogInfo("Checked diff", checkedDiff, true);
                LogInfo("Old normal info", oldNormalInfo, false);
                LogInfo("New normal info", newNormalInfo, false);
                LogInfo("Old checked info", oldCheckedInfo, false);
                LogInfo("New checked info", newCheckedInfo, false);

                Console.ReadLine();
                throw new Exception("Operation part calculations incorrect, see console");
            }
        }

        public void CheckExternalDriverTypeErrors() {
            // Check old value
            if (!SaExternalDriverTypeInfo.AreEqual(oldNormalInfo.ExternalDriverTypeInfo, oldCheckedInfo.ExternalDriverTypeInfo)) {
                SaExternalDriverTypeInfo errorAmounts = oldNormalInfo.ExternalDriverTypeInfo - oldCheckedInfo.ExternalDriverTypeInfo;

                LogErrorHeader("Operation error: incorrect old values");
                LogInfo("Error amounts", errorAmounts, false, false);
                LogInfo("Old normal info", oldNormalInfo, false);
                LogInfo("Old checked info", oldCheckedInfo, false);

                if (driver is ExternalDriver externalDriver) {
                    LogExternalDriverInfo(externalDriver);
                }

                Console.ReadLine();
                throw new Exception("Operation part calculations incorrect, see console");
            }

            // Check diffs
            SaExternalDriverTypeInfo normalDiff = newNormalInfo.ExternalDriverTypeInfo - oldNormalInfo.ExternalDriverTypeInfo;
            SaExternalDriverTypeInfo checkedDiff = newCheckedInfo.ExternalDriverTypeInfo - oldCheckedInfo.ExternalDriverTypeInfo;
            if (!SaExternalDriverTypeInfo.AreEqual(normalDiff, checkedDiff)) {
                SaExternalDriverTypeInfo errorAmounts = normalDiff - checkedDiff;

                LogErrorHeader("Operation error: incorrect operation diff");
                LogInfo("Error amounts", errorAmounts, false, false);
                LogInfo("Normal diff", normalDiff, true);
                LogInfo("Checked diff", checkedDiff, true);
                LogInfo("Old normal info", oldNormalInfo, false);
                LogInfo("New normal info", newNormalInfo, false);
                LogInfo("Old checked info", oldCheckedInfo, false);
                LogInfo("New checked info", newCheckedInfo, false);

                if (driver is ExternalDriver externalDriver) {
                    LogExternalDriverInfo(externalDriver);
                }

                Console.ReadLine();
                throw new Exception("Operation part calculations incorrect, see console");
            }
        }

        void LogErrorHeader(string title) {
            Console.WriteLine("*** {0} in iteration {1} ***", title, info.IterationNum);
            Console.WriteLine("Current operation: {0}", operation.Description);
            for (int i = 0; i < operation.Parts.Count; i++) Console.WriteLine("Previous part: {0}", operation.Parts[i].description);
            Console.WriteLine("Current part: {0}", description);
        }

        static void LogInfo(string title, DebugTotalInfoBasic totalInfo, bool isDiff, bool shouldLogZeros = true) {
            Console.WriteLine("\n* {0} *", title);
            totalInfo.Log(isDiff, shouldLogZeros);
        }
        static void LogInfo(string title, SaDriverInfo driverInfo, bool isDiff, bool shouldLogZeros = true) {
            Console.WriteLine("\n* {0} *", title);
            driverInfo.DebugLog(isDiff, shouldLogZeros);
        }
        static void LogInfo(string title, SaExternalDriverTypeInfo externalDriverTypeInfo, bool isDiff, bool shouldLogZeros = true) {
            Console.WriteLine("\n* {0} *", title);
            externalDriverTypeInfo.DebugLog(isDiff, shouldLogZeros);
        }

        static void LogExternalDriverInfo(ExternalDriver externalDriver) {
            ExternalDriverTypeSettings externalDriverTypeSettings = Config.ExternalDriverTypes[externalDriver.ExternalDriverTypeIndex];
            Console.WriteLine("\n* External driver type *");
            Console.WriteLine("Company name: {0}", externalDriverTypeSettings.CompanyName);
            Console.WriteLine("Is international: {0}", externalDriverTypeSettings.IsInternational);
            Console.WriteLine("Min shift count: {0}", externalDriverTypeSettings.MinShiftCount);
            Console.WriteLine("Max shift count: {0}", externalDriverTypeSettings.MaxShiftCount);
        }

        public static string ParseValuePairs(List<(int, int)> valuePairs) {
            return string.Join(" ", valuePairs.Select(valuePair => string.Format("({0}|{1})", valuePair.Item1, valuePair.Item2)));
        }
    }

    class DebugTotalInfo : DebugTotalInfoBasic {
        readonly List<DebugShiftInfo> shifts;
        DebugShiftInfo currentShift;

        public DebugTotalInfo() {
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

    class DebugTotalInfoBasic {
        public SaDriverInfo DriverInfo;
        public SaExternalDriverTypeInfo ExternalDriverTypeInfo;

        public void SetDriverInfo(SaDriverInfo driverInfo) {
            DriverInfo = driverInfo;
        }

        public void SetExternalDriverTypeInfo(SaExternalDriverTypeInfo externalDriverTypeInfo) {
            ExternalDriverTypeInfo = externalDriverTypeInfo;
        }

        public virtual void Log(bool isDiff, bool shouldLogZeros = true) {
            if (DriverInfo != null) DriverInfo.DebugLog(isDiff, shouldLogZeros);
            if (ExternalDriverTypeInfo != null) ExternalDriverTypeInfo.DebugLog(isDiff, shouldLogZeros);
        }
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
        public readonly DebugTotalInfoBasic Total = new DebugTotalInfoBasic();
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

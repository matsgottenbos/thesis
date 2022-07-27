namespace DriverPlannerShared {
    public static class SaDebugger {
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

    public class OperationInfo {
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

    public enum OperationPartStage {
        None,
        OldNormal,
        NewNormal,
        OldChecked,
        NewChecked,
    }

    public class OperationPart {
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
                LogInfo("Old checked info", oldCheckedInfo, false);
                LogInfo("New normal info", newNormalInfo, false);
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
                LogInfo("Old checked info", oldCheckedInfo, false);
                LogInfo("New normal info", newNormalInfo, false);
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

        void LogExternalDriverInfo(ExternalDriver externalDriver) {
            ExternalDriverType externalDriverType = info.Instance.ExternalDriverTypes[externalDriver.ExternalDriverTypeIndex];
            Console.WriteLine("\n* External driver type *");
            Console.WriteLine("Company name: {0}", externalDriverType.CompanyName);
            Console.WriteLine("Is international: {0}", externalDriverType.IsInternational);
            Console.WriteLine("Min shift count: {0}", externalDriverType.MinShiftCount);
            Console.WriteLine("Max shift count: {0}", externalDriverType.MaxShiftCount);
        }

        public static string ParseValuePairs(List<(int, int)> valuePairs) {
            return string.Join(" ", valuePairs.Select(valuePair => string.Format("({0}|{1})", valuePair.Item1, valuePair.Item2)));
        }
    }

    public class DebugTotalInfo : DebugTotalInfoBasic {
        readonly List<DebugShiftInfo> shifts;
        DebugShiftInfo currentShift;

        public DebugTotalInfo() {
            shifts = new List<DebugShiftInfo>();
            currentShift = new DebugShiftInfo();
        }

        public void AddActivity(Activity activity, bool isOverlapViolationAfter, bool isInvalidHotelAfter) {
            currentShift.Activities.Add(new DebugActivityInfo(activity, isOverlapViolationAfter, isInvalidHotelAfter));
        }

        public void SetRestInfo(int? restTimeAfter, bool isHotelAfter, bool isInvalidHotelAfter) {
            currentShift.RestTimeAfter = restTimeAfter;
            currentShift.IsHotelAfter = isHotelAfter;
            currentShift.IsInvalidHotelAfter = isInvalidHotelAfter;
        }

        public void SetShiftDetails(Activity shiftFirstActivity, Activity shiftLastActivity, Activity afterHotelActivity, MainShiftInfo mainShiftInfo, DriverTypeMainShiftInfo driverTypeMainShiftInfo, int fullShiftLength, int sharedCarTravelTimeBefore, int ownCarTravelTimeBefore, int sharedCarTravelTimeAfter, int ownCarTravelTimeAfter) {
            currentShift.ShiftFirstActivity = shiftFirstActivity;
            currentShift.ShiftLastActivity = shiftLastActivity;
            currentShift.AfterHotelActivity = afterHotelActivity;
            currentShift.MainShiftInfo = mainShiftInfo;
            currentShift.DriverTypeMainShiftInfo = driverTypeMainShiftInfo;
            currentShift.FullShiftLength = fullShiftLength;
            currentShift.SharedCarTravelTimeBefore = sharedCarTravelTimeBefore;
            currentShift.OwnCarTravelTimeBefore = ownCarTravelTimeBefore;
            currentShift.SharedCarTravelTimeAfter = sharedCarTravelTimeAfter;
            currentShift.OwnCarTravelTimeAfter = ownCarTravelTimeAfter;

            shifts.Add(currentShift);
            currentShift = new DebugShiftInfo();
        }

        public override void Log(bool isDiff, bool shouldLogZeros = true) {
            base.Log(isDiff, shouldLogZeros);

            // Log path string
            for (int shiftIndex = 0; shiftIndex < shifts.Count; shiftIndex++) {
                DebugShiftInfo shiftInfo = shifts[shiftIndex];
                for (int shiftActivityIndex = 0; shiftActivityIndex < shiftInfo.Activities.Count; shiftActivityIndex++) {
                    DebugActivityInfo activityInfo = shiftInfo.Activities[shiftActivityIndex];
                    Console.Write(activityInfo.Activity.Index);
                    if (shiftActivityIndex + 1 < shiftInfo.Activities.Count) Console.Write("-");
                }
                Console.Write("|");
                if (shiftInfo.IsHotelAfter) Console.WriteLine("H|");
            }
            Console.WriteLine();

            // Log path info
            for (int shiftIndex = 0; shiftIndex < shifts.Count; shiftIndex++) {
                shifts[shiftIndex].Log();
            }
        }
    }

    public class DebugTotalInfoBasic {
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

    public class DebugShiftInfo {
        public List<DebugActivityInfo> Activities;
        public Activity ShiftFirstActivity, ShiftLastActivity, AfterHotelActivity;
        public MainShiftInfo MainShiftInfo;
        public DriverTypeMainShiftInfo DriverTypeMainShiftInfo;
        public int FullShiftLength, SharedCarTravelTimeBefore, OwnCarTravelTimeBefore, SharedCarTravelTimeAfter, OwnCarTravelTimeAfter;
        public int? RestTimeAfter;
        public bool IsHotelAfter, IsInvalidHotelAfter;

        public DebugShiftInfo() {
            Activities = new List<DebugActivityInfo>();
        }

        public void Log() {
            Console.WriteLine("> Shift {0}--{1}", Activities[0].Activity.Index, Activities[^1].Activity.Index);
            Console.WriteLine("Shift first activity: {0}", ShiftFirstActivity?.Index.ToString() ?? "-");
            Console.WriteLine("Shift last activity: {0}", ShiftLastActivity?.Index.ToString() ?? "-");
            Console.WriteLine("After hotel activity: {0}", AfterHotelActivity?.Index.ToString() ?? "-");
            Console.WriteLine("Activity part length: {0}", Activities[^1].Activity.EndTime - Activities[0].Activity.StartTime);
            Console.WriteLine("Real main length: {0}", MainShiftInfo.RealMainShiftLength);
            Console.WriteLine("Paid main length: {0}", DriverTypeMainShiftInfo.PaidMainShiftLength);
            Console.WriteLine("Shared car travel time before: {0}", SharedCarTravelTimeBefore);
            Console.WriteLine("Own car travel time before: {0}", OwnCarTravelTimeBefore);
            Console.WriteLine("Shared car travel time after: {0}", SharedCarTravelTimeAfter);
            Console.WriteLine("Own car travel time after: {0}", OwnCarTravelTimeAfter);
            Console.WriteLine("Full length: {0}", FullShiftLength);
            Console.WriteLine("Rest time after: {0}", RestTimeAfter?.ToString() ?? "-");
        }
    }

    public class DebugActivityInfo {
        public Activity Activity;
        public bool IsOverlapViolationAfter, IsInvalidHotelAfter;

        public DebugActivityInfo(Activity activity, bool isOverlapViolationAfter, bool isInvalidHotel) {
            Activity = activity;
            IsOverlapViolationAfter = isOverlapViolationAfter;
            IsInvalidHotelAfter = isInvalidHotel;
        }
    }
}

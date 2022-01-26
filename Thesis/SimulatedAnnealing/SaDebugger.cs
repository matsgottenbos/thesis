using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class SaDebugger {
        static CheckedInfo prevChecked = new CheckedInfo();
        public static CheckedInfo CurrentChecked = new CheckedInfo();
        public static OperationInfo CurrentOperation = null;
        static int iterationNum = 0;

        public static void NextIteration(bool isAccepted, Instance instance) {
            if (isAccepted) {
                prevChecked = CurrentChecked;
                CurrentChecked = new CheckedInfo();
            }

            CurrentOperation = new OperationInfo(instance);
            iterationNum++;
        }

        public static void FinishInitialCheck(Instance instance) {
            prevChecked = CurrentChecked;
            CurrentChecked = new CheckedInfo();
            CurrentOperation = new OperationInfo(instance);
        }

        public static void CheckErrors() {
            FullInfo checkedDiff = CurrentChecked.Info - prevChecked.Info;
            FullInfo operationDiff = CurrentOperation.Combine();
            FullInfo errorAmounts = operationDiff - checkedDiff;

            if (!FullInfo.AreEqual(operationDiff, checkedDiff)) {
                LogErrors(checkedDiff, operationDiff, errorAmounts);
                throw new Exception("Operation calculations were incorrect, see console");
            }
        }
        static void LogErrors(FullInfo checkedDiff, FullInfo operationDiff, FullInfo errorAmounts) {
            Console.WriteLine("*** Error in iteration {0} ***", iterationNum);

            Console.WriteLine("\n* Checked diff *");
            checkedDiff.Log();

            Console.WriteLine("\n* Operation diff *");
            operationDiff.Log();

            Console.WriteLine("\n\n* Error amounts *");
            errorAmounts.Log();

            Console.WriteLine("\n\n* Checked additional info *");
            Console.WriteLine("\nBefore driver paths");
            prevChecked.LogAdditionalInfo();
            Console.WriteLine("\nAfter driver paths");
            CurrentChecked.LogAdditionalInfo();

            Console.WriteLine("\n\n* Operation additional info *");
            CurrentOperation.LogAdditionalInfo();
        }
    }

    class FullInfo {
        public double? Cost, CostWithoutPenalty, PenaltyBase;
        public int? PrecedenceViolationCount, WdlViolationCount, WdlViolationAmount, RtViolationCount, RtViolationAmount, CtViolationCount, CtViolationAmount;
        public int[] DriversWorkedTime;
        readonly bool isDiff;

        public FullInfo(bool isDiff = false) {
            this.isDiff = isDiff;
        }

        public void Log() {
            string diffStr = isDiff ? " diff" : "";

            Console.WriteLine("Cost{0}: {1}", diffStr, ParseHelper.ToString(Cost.Value));
            Console.WriteLine("Cost without penalty{0}: {1}", diffStr, ParseHelper.ToString(CostWithoutPenalty.Value));
            Console.WriteLine("Penalty base{0}: {1}", diffStr, ParseHelper.ToString(PenaltyBase.Value));
            Console.WriteLine("Precedence violation count{0}: {1}", diffStr, PrecedenceViolationCount);
            Console.WriteLine("WDL violation count{0}: {1}", diffStr, WdlViolationCount);
            Console.WriteLine("WDL violation amount{0}: {1}", diffStr, WdlViolationAmount);
            Console.WriteLine("RT violation count{0}: {1}", diffStr, RtViolationCount);
            Console.WriteLine("RT violation amount{0}: {1}", diffStr, RtViolationAmount);
            Console.WriteLine("CT violation count{0}: {1}", diffStr, CtViolationCount);
            Console.WriteLine("CT violation amount{0}: {1}", diffStr, CtViolationAmount);
            Console.WriteLine("Worked hours{0}: {1}", diffStr, ParseHelper.ToString(DriversWorkedTime));
        }

        public static bool AreEqual(FullInfo a, FullInfo b) {
            return (
                IsFloatEqual(a.Cost, b.Cost) &&
                IsFloatEqual(a.CostWithoutPenalty, b.CostWithoutPenalty) &&
                IsFloatEqual(a.PenaltyBase, b.PenaltyBase) &&
                a.PrecedenceViolationCount == b.PrecedenceViolationCount &&
                a.WdlViolationCount == b.WdlViolationCount &&
                a.WdlViolationAmount == b.WdlViolationAmount &&
                a.RtViolationCount == b.RtViolationCount &&
                a.RtViolationAmount == b.RtViolationAmount &&
                a.CtViolationCount == b.CtViolationCount &&
                a.CtViolationAmount == b.CtViolationAmount
            );
        }
        static bool IsFloatEqual(double? a, double? b) {
            return Math.Abs(a.Value - b.Value) < 0.01;
        }

        public static FullInfo operator -(FullInfo a) {
            return new FullInfo(true) {
                Cost = -a.Cost,
                CostWithoutPenalty = -a.CostWithoutPenalty,
                PenaltyBase = -a.PenaltyBase,
                PrecedenceViolationCount = -a.PrecedenceViolationCount,
                WdlViolationCount = -a.WdlViolationCount,
                WdlViolationAmount = -a.WdlViolationAmount,
                RtViolationCount = -a.RtViolationCount,
                RtViolationAmount = -a.RtViolationAmount,
                CtViolationCount = -a.CtViolationCount,
                CtViolationAmount = -a.CtViolationAmount,
                DriversWorkedTime = a.DriversWorkedTime.Select(x => -x).ToArray(),
            };
        }
        public static FullInfo operator +(FullInfo a, FullInfo b) {
            return new FullInfo(true) {
                Cost = a.Cost + b.Cost,
                CostWithoutPenalty = a.CostWithoutPenalty + b.CostWithoutPenalty,
                PenaltyBase = a.PenaltyBase + b.PenaltyBase,
                PrecedenceViolationCount = a.PrecedenceViolationCount + b.PrecedenceViolationCount,
                WdlViolationCount = a.WdlViolationCount + b.WdlViolationCount,
                WdlViolationAmount = a.WdlViolationAmount + b.WdlViolationAmount,
                RtViolationCount = a.RtViolationCount + b.RtViolationCount,
                RtViolationAmount = a.RtViolationAmount + b.RtViolationAmount,
                CtViolationCount = a.CtViolationCount + b.CtViolationCount,
                CtViolationAmount = a.CtViolationAmount + b.CtViolationAmount,
                DriversWorkedTime = a.DriversWorkedTime.Zip(b.DriversWorkedTime, (x, y) => x + y).ToArray(),
            };
        }
        public static FullInfo operator -(FullInfo a, FullInfo b) => a + -b;
    }

    class CheckedInfo {
        public readonly FullInfo Info = new FullInfo();
        public string[] DriverPathStrings = new string[Config.GenDriverCount];

        public void LogAdditionalInfo() {
            for (int driverIndex = 0; driverIndex < DriverPathStrings.Length; driverIndex++) {
                Console.WriteLine("Driver {0}: {1}", driverIndex, DriverPathStrings[driverIndex]);
            }
        }
    }

    class OperationInfo {
        public readonly List<OperationPartInfo> Parts = new List<OperationPartInfo>();
        public OperationPartInfo CurrentPart = null;
        readonly Instance instance;

        public OperationInfo(Instance instance) {
            this.instance = instance;
        }

        public void StartPart(string description) {
            if (CurrentPart != null) Parts.Add(CurrentPart);
            CurrentPart = new OperationPartInfo(description, instance);
        }

        public FullInfo Combine() {
            FullInfo combined = CurrentPart.ToFullInfo();
            for (int i = 0; i < Parts.Count; i++) {
                combined += Parts[i].ToFullInfo();
            }
            return combined;
        }

        public void LogAdditionalInfo() {
            for (int i = 0; i < Parts.Count; i++) {
                Parts[i].Log();
            }
            CurrentPart.Log();
        }
    }

    class OperationPartInfo {
        public Trip TripBeforeSameShift, TripAfterSameShift, TripBeforePrevShift, TripAfterNextShift;
        public string ShiftInfoStr;
        public double CostDiff, CostWithoutPenaltyDiff, PenaltyBaseDiff;
        public PrecedenceValueChange Precedence;
        public ViolationValueChange WorkDayLength, RestTime, ContractTime;
        public int[] DriversWorkedTimeDiff;
        readonly string description;
        readonly Instance instance;

        public OperationPartInfo(string description, Instance instance) {
            this.description = description;
            this.instance = instance;

            Precedence = new PrecedenceValueChange("Precedence", instance);
            WorkDayLength = new ViolationValueChange("WDL", instance, (workDayLength, _) => Math.Max(0, workDayLength - Config.MaxWorkDayLength));
            RestTime = new ViolationValueChange("RT", instance, (restTime, _) => Math.Max(0, Config.MinRestTime - restTime));
            ContractTime = new ViolationValueChange("CT", instance, (workedHours, driver) => Math.Max(0, driver.MinContractTime - workedHours) + Math.Max(0, workedHours - driver.MaxContractTime));
            DriversWorkedTimeDiff = new int[Config.GenDriverCount];
    }

        public FullInfo ToFullInfo() {
            return new FullInfo(true) {
                Cost = CostDiff,
                CostWithoutPenalty = CostWithoutPenaltyDiff,
                PenaltyBase = PenaltyBaseDiff,
                PrecedenceViolationCount = Precedence.NewViolationCount - Precedence.OldViolationCount,
                WdlViolationCount = WorkDayLength.NewViolationCount - WorkDayLength.OldViolationCount,
                WdlViolationAmount = WorkDayLength.NewViolationAmount - WorkDayLength.OldViolationAmount,
                RtViolationCount = RestTime.NewViolationCount - RestTime.OldViolationCount,
                RtViolationAmount = RestTime.NewViolationAmount - RestTime.OldViolationAmount,
                CtViolationCount = ContractTime.NewViolationCount - ContractTime.OldViolationCount,
                CtViolationAmount = ContractTime.NewViolationAmount - ContractTime.OldViolationAmount,
                DriversWorkedTime = DriversWorkedTimeDiff,
            };
        }

        public void Log() {
            Console.WriteLine("\n" + description);
            Console.WriteLine(ShiftInfoStr);
            Console.WriteLine("Before same shift: {0}; After same shift: {1}; Before prev shift: {2}; After next shift: {3}", GetTripString(TripBeforeSameShift), GetTripString(TripAfterSameShift), GetTripString(TripBeforePrevShift), GetTripString(TripAfterNextShift));
            LogValue("Cost diff", CostDiff);
            LogValue("Cost without penalty diff", CostWithoutPenaltyDiff);
            LogValue("Penalty base diff", PenaltyBaseDiff);

            Precedence.Log();
            WorkDayLength.Log();
            RestTime.Log();
        }

        string GetTripString(Trip trip) {
            return trip == null ? "-" : trip.Index.ToString();
        }

        void LogValue<T>(string name, T value) {
            if (value is double valueDouble) Console.WriteLine("{0}: {1}", name, ParseHelper.ToString(valueDouble));
            else Console.WriteLine("{0}: {1}", name, value);
        }
    }



    abstract class ValueChange<T> {
        protected readonly string Name;
        protected readonly Instance instance;

        public ValueChange(string name, Instance instance) {
            Name = name;
            this.instance = instance;
        }

        public void Add(T oldValue, T newValue, Driver driver, bool shouldReverse) {
            AddOld(oldValue, driver, shouldReverse);
            AddNew(newValue, driver, shouldReverse);
        }

        public void AddOld(T oldValue, Driver driver, bool shouldReverse) {
            if (shouldReverse) AddNewInternal(oldValue, driver);
            else AddOldInternal(oldValue, driver);
        }
        public void AddNew(T newValue, Driver driver, bool shouldReverse) {
            if (shouldReverse) AddOldInternal(newValue, driver);
            else AddNewInternal(newValue, driver);
        }

        protected abstract void AddOldInternal(T oldValue, Driver driver);
        protected abstract void AddNewInternal(T oldValue, Driver driver);

        public abstract void Log();

        protected void LogVar(string varName, int? oldVar, int? newVar) {
            Console.WriteLine("{0} {1}: {2} ({3} -> {4})", Name, varName, newVar - oldVar, oldVar, newVar);
        }
    }

    class PrecedenceValueChange: ValueChange<(Trip, Trip)> {
        public int OldViolationCount, NewViolationCount;

        public PrecedenceValueChange(string name, Instance instance) : base(name, instance) { }

        protected override void AddOldInternal((Trip, Trip) oldConsecutiveTrips, Driver driver) => AddSpecific(oldConsecutiveTrips, driver, instance, ref OldViolationCount);
        protected override void AddNewInternal((Trip, Trip) newConsecutiveTrips, Driver driver) => AddSpecific(newConsecutiveTrips, driver, instance, ref NewViolationCount);

        protected void AddSpecific((Trip, Trip) consecutiveTrips, Driver driver, Instance instance, ref int violationCountVar) {
            int violationCount = instance.TripSuccession[consecutiveTrips.Item1.Index, consecutiveTrips.Item2.Index] ? 0 : 1;
            violationCountVar += violationCount;
        }

        public override void Log() {
            LogVar("violation count", OldViolationCount, NewViolationCount);
        }
    }

    class ViolationValueChange : ValueChange<int> {
        public int OldValue, NewValue, OldViolationCount, NewViolationCount, OldViolationAmount, NewViolationAmount;
        Func<int, Driver, int> getViolationAmount;

        public ViolationValueChange(string name, Instance instance, Func<int, Driver, int> getViolationAmount) : base(name, instance) {
            this.getViolationAmount = getViolationAmount;
        }

        protected override void AddOldInternal(int oldValue, Driver driver) => AddSpecific(oldValue, driver, ref OldValue, ref OldViolationCount, ref OldViolationAmount);
        protected override void AddNewInternal(int newValue, Driver driver) => AddSpecific(newValue, driver, ref NewValue, ref NewViolationCount, ref NewViolationAmount);

        protected void AddSpecific(int value, Driver driver, ref int valueVar, ref int violationCountVar, ref int violationAmountVar) {
            int violationAmount = getViolationAmount(value, driver);
            int violationCount = violationAmount > 0 ? 1 : 0;

            valueVar += value;
            violationAmountVar += violationAmount;
            violationCountVar += violationCount;
        }

        public override void Log() {
            LogVar("value", OldValue, NewValue);
            LogVar("violation count", OldViolationCount, NewViolationCount);
            LogVar("violation amount", OldViolationAmount, NewViolationAmount);
        }
    }
}

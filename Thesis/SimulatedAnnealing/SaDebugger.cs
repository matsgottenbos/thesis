using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class SaDebugger {
        public static int IterationNum = 0;
        static OperationInfo CurrentOperation = null;

        public static OperationInfo GetCurrentOperation() {
            return CurrentOperation;
        }

        public static OperationPart GetCurrentOperationPart() {
            return CurrentOperation.CurrentPart;
        }

        public static NormalDiff GetCurrentNormalDiff() {
            return CurrentOperation.CurrentPart.Normal;
        }

        public static CheckedTotal GetCurrentCheckedTotal() {
            return CurrentOperation.CurrentPart.CheckedCurrent;
        }

        public static void NextIteration(Instance instance) {
            IterationNum++;
            CurrentOperation = new OperationInfo(IterationNum, instance);
        }

        public static void ResetIteration(Instance instance) {
            CurrentOperation = new OperationInfo(IterationNum, instance);
        }
    }

    class OperationInfo {
        public string Description;
        public readonly List<OperationPart> Parts = new List<OperationPart>();
        public OperationPart CurrentPart = null;
        readonly int iterationNum;
        readonly Instance instance;

        public OperationInfo(int iterationNum, Instance instance) {
            this.iterationNum = iterationNum;
            this.instance = instance;
        }

        public void StartPart(string partDescription, bool isAssign, Driver driver) {
            if (CurrentPart != null) Parts.Add(CurrentPart);
            CurrentPart = new OperationPart(iterationNum, partDescription, isAssign, this, driver, instance);
        }
    }

    class OperationPart {
        public CheckedTotal CheckedCurrent;
        public readonly NormalDiff Normal;
        CheckedTotal checkedBefore, checkedAfter;
        TotalInfo checkedDiff;
        readonly int iterationNum;
        public readonly string Description;
        readonly OperationInfo operation;
        readonly Driver driver;
        readonly Instance instance;

        public OperationPart(int iterationNum, string description, bool isAssign, OperationInfo operation, Driver driver, Instance instance) {
            this.iterationNum = iterationNum;
            Description = description;
            this.operation = operation;
            this.driver = driver;
            this.instance = instance;
            CheckedCurrent = new CheckedTotal();
            Normal = new NormalDiff(isAssign, driver, instance);
        }

        public void FinishCheckBefore() {
            checkedBefore = CheckedCurrent;
            CheckedCurrent = new CheckedTotal();
        }

        public void FinishCheckAfter() {
            checkedAfter = CheckedCurrent;
            checkedDiff = checkedAfter.Total - checkedBefore.Total;
        }

        public void CheckErrors() {
            TotalInfo operationDiff = Normal.ToTotal();
            TotalInfo errorAmounts = operationDiff - checkedDiff;

            if (!TotalInfo.AreEqual(operationDiff, checkedDiff)) {
                LogErrors(errorAmounts);
                Console.ReadLine();
                throw new Exception("Operation part calculations incorrect, see console");
            }
        }

        void LogErrors(TotalInfo errorAmounts) {
            Console.WriteLine("*** Error in iteration {0} ***", iterationNum);
            Console.WriteLine("Current operation: {0}", operation.Description);
            for (int i = 0; i < operation.Parts.Count; i++) Console.WriteLine("Previous part: {0}", operation.Parts[i].Description);
            Console.WriteLine("Current part:  {0}", Description);

            Console.WriteLine("\n* Error amounts *");
            errorAmounts.Log(false);

            Console.WriteLine("\n* Normal diff *");
            Normal.ToTotal().Log();

            Console.WriteLine("\n* Checked diff *");
            checkedDiff.Log();

            Console.WriteLine("\n* Normal info *");
            Normal.Log();

            Console.WriteLine("\n* Checked total before *");
            checkedBefore.Log();

            Console.WriteLine("\n* Checked total after *");
            checkedAfter.Log();

            Console.WriteLine("\n* Driver info *");
            Console.WriteLine("Min contract time: {0}", driver.MinContractTime);
            Console.WriteLine("Max contract time: {0}", driver.MaxContractTime);
        }
    }

    class CheckedTotal {
        public readonly TotalInfo Total = new TotalInfo();
        public string DriverPathString = "";
        public List<int> ShiftLengths = new List<int>();
        public List<int> RestTimes = new List<int>();

        public void Log() {
            Console.WriteLine("Driver path: {0}", DriverPathString);
            Console.WriteLine("Shift lengths: {0}", ParseHelper.ToString(ShiftLengths));
            Console.WriteLine("Rest times: {0}", ParseHelper.ToString(RestTimes));
            Console.WriteLine("Worked time: {0}", ShiftLengths.Sum());
            Total.Log();
        }
    }

    class TotalInfo {
        public double? Cost, CostWithoutPenalty, PenaltyBase;
        public int? PrecedenceViolationCount, SlViolationCount, SlViolationAmount, RtViolationCount, RtViolationAmount, CtValue, CtViolationCount, CtViolationAmount;
        readonly bool isDiff;

        public TotalInfo(bool isDiff = false) {
            this.isDiff = isDiff;
        }

        public void Log(bool shouldLogZeros = true) {
            string diffStr = isDiff ? " diff" : "";

            if (shouldLogZeros || PrecedenceViolationCount != 0) Console.WriteLine("Precedence violation count{0}: {1}", diffStr, PrecedenceViolationCount);
            if (shouldLogZeros || SlViolationCount != 0) Console.WriteLine("SL violation count{0}: {1}", diffStr, SlViolationCount);
            if (shouldLogZeros || SlViolationAmount != 0) Console.WriteLine("SL violation amount{0}: {1}", diffStr, SlViolationAmount);
            if (shouldLogZeros || RtViolationCount != 0) Console.WriteLine("RT violation count{0}: {1}", diffStr, RtViolationCount);
            if (shouldLogZeros || RtViolationAmount != 0) Console.WriteLine("RT violation amount{0}: {1}", diffStr, RtViolationAmount);
            if (shouldLogZeros || CtViolationCount != 0) Console.WriteLine("CT violation count{0}: {1}", diffStr, CtViolationCount);
            if (shouldLogZeros || CtViolationAmount != 0) Console.WriteLine("CT violation amount{0}: {1}", diffStr, CtViolationAmount);
            if (shouldLogZeros || Math.Abs(Cost.Value) > Config.FloatingPointMargin) Console.WriteLine("Cost{0}: {1}", diffStr, ParseHelper.ToString(Cost.Value));
            if (shouldLogZeros || Math.Abs(CostWithoutPenalty.Value) > Config.FloatingPointMargin) Console.WriteLine("Cost without penalty{0}: {1}", diffStr, ParseHelper.ToString(CostWithoutPenalty.Value));
            if (shouldLogZeros || Math.Abs(PenaltyBase.Value) > Config.FloatingPointMargin) Console.WriteLine("Penalty base{0}: {1}", diffStr, ParseHelper.ToString(PenaltyBase.Value));
        }

        public static bool AreEqual(TotalInfo a, TotalInfo b) {
            return (
                IsFloatEqual(a.Cost, b.Cost) &&
                IsFloatEqual(a.CostWithoutPenalty, b.CostWithoutPenalty) &&
                IsFloatEqual(a.PenaltyBase, b.PenaltyBase) &&
                a.PrecedenceViolationCount == b.PrecedenceViolationCount &&
                a.SlViolationCount == b.SlViolationCount &&
                a.SlViolationAmount == b.SlViolationAmount &&
                a.RtViolationCount == b.RtViolationCount &&
                a.RtViolationAmount == b.RtViolationAmount &&
                a.CtValue == b.CtValue &&
                a.CtViolationCount == b.CtViolationCount &&
                a.CtViolationAmount == b.CtViolationAmount
            );
        }
        static bool IsFloatEqual(double? a, double? b) {
            return Math.Abs(a.Value - b.Value) < 0.01;
        }

        public static TotalInfo operator -(TotalInfo a) {
            return new TotalInfo(true) {
                Cost = -a.Cost,
                CostWithoutPenalty = -a.CostWithoutPenalty,
                PenaltyBase = -a.PenaltyBase,
                PrecedenceViolationCount = -a.PrecedenceViolationCount,
                SlViolationCount = -a.SlViolationCount,
                SlViolationAmount = -a.SlViolationAmount,
                RtViolationCount = -a.RtViolationCount,
                RtViolationAmount = -a.RtViolationAmount,
                CtValue = -a.CtValue,
                CtViolationCount = -a.CtViolationCount,
                CtViolationAmount = -a.CtViolationAmount,
            };
        }
        public static TotalInfo operator +(TotalInfo a, TotalInfo b) {
            return new TotalInfo(true) {
                Cost = a.Cost + b.Cost,
                CostWithoutPenalty = a.CostWithoutPenalty + b.CostWithoutPenalty,
                PenaltyBase = a.PenaltyBase + b.PenaltyBase,
                PrecedenceViolationCount = a.PrecedenceViolationCount + b.PrecedenceViolationCount,
                SlViolationCount = a.SlViolationCount + b.SlViolationCount,
                SlViolationAmount = a.SlViolationAmount + b.SlViolationAmount,
                RtViolationCount = a.RtViolationCount + b.RtViolationCount,
                RtViolationAmount = a.RtViolationAmount + b.RtViolationAmount,
                CtValue = a.CtValue + b.CtValue,
                CtViolationCount = a.CtViolationCount + b.CtViolationCount,
                CtViolationAmount = a.CtViolationAmount + b.CtViolationAmount,
            };
        }
        public static TotalInfo operator -(TotalInfo a, TotalInfo b) => a + -b;
    }

    class NormalDiff {
        public Trip PrevTripInternal, NextTripInternal, FirstTripInternal, LastTripInternal, PrevShiftFirstTrip, PrevShiftLastTrip, NextShiftFirstTrip;
        public string TripPosition, ShiftPosition, MergeSplitInfo;
        public double CostDiff, CostWithoutPenaltyDiff, BasePenaltyDiff;
        public PrecedenceValueChange Precedence;
        public ViolationValueChange ShiftLength, RestTime, ContractTime;

        public NormalDiff(bool isAssign, Driver driver, Instance instance) {
            Precedence = new PrecedenceValueChange("Precedence", isAssign, driver, instance);
            ShiftLength = new ViolationValueChange("SL", isAssign, driver, instance, (workDayLength, _) => Math.Max(0, workDayLength - Config.MaxWorkDayLength));
            RestTime = new ViolationValueChange("RT", isAssign, driver, instance, (restTime, _) => Math.Max(0, Config.MinRestTime - restTime));
            ContractTime = new ViolationValueChange("CT", false, driver, instance, (workedHours, driver) => Math.Max(0, driver.MinContractTime - workedHours) + Math.Max(0, workedHours - driver.MaxContractTime));
    }

        public TotalInfo ToTotal() {
            return new TotalInfo(true) {
                Cost = CostDiff,
                CostWithoutPenalty = CostWithoutPenaltyDiff,
                PenaltyBase = BasePenaltyDiff,
                PrecedenceViolationCount = Precedence.newViolations.Count - Precedence.oldViolations.Count,
                SlViolationCount = ShiftLength.NewViolationCount - ShiftLength.OldViolationCount,
                SlViolationAmount = ShiftLength.NewViolationAmount - ShiftLength.OldViolationAmount,
                RtViolationCount = RestTime.NewViolationCount - RestTime.OldViolationCount,
                RtViolationAmount = RestTime.NewViolationAmount - RestTime.OldViolationAmount,
                CtViolationCount = ContractTime.NewViolationCount - ContractTime.OldViolationCount,
                CtViolationAmount = ContractTime.NewViolationAmount - ContractTime.OldViolationAmount,
            };
        }

        public void Log() {
            Console.WriteLine("Trip position: {0}", TripPosition);
            Console.WriteLine("Shift position: {0}", ShiftPosition);
            Console.WriteLine("Split/merge info: {0}", MergeSplitInfo);
            Console.WriteLine("Prev trip internal: {0}; Next trip internal: {1}; First trip internal: {2}; Last trip internal: {3}; Prev shift first trip: {4}; Prev shift last trip: {5}; Next shift first trip: {6}", GetTripString(PrevTripInternal), GetTripString(NextTripInternal), GetTripString(FirstTripInternal), GetTripString(LastTripInternal), GetTripString(PrevShiftFirstTrip), GetTripString(PrevShiftLastTrip), GetTripString(NextShiftFirstTrip));
            LogValue("Cost diff", CostDiff);
            LogValue("Cost without penalty diff", CostWithoutPenaltyDiff);
            LogValue("Base penalty diff", BasePenaltyDiff);

            Precedence.Log();
            ShiftLength.Log();
            RestTime.Log();
            ContractTime.Log();
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
        protected readonly string name;
        protected readonly bool shouldReverse;
        protected readonly Driver driver;
        protected readonly Instance instance;

        public ValueChange(string name, bool shouldReverse, Driver driver, Instance instance) {
            this.name = name;
            this.shouldReverse = shouldReverse;
            this.driver = driver;
            this.instance = instance;
        }

        public void Add(T oldValue, T newValue) {
            AddOld(oldValue);
            AddNew(newValue);
        }

        public void AddOld(T oldValue) {
            if (shouldReverse) AddNewInternal(oldValue);
            else AddOldInternal(oldValue);
        }
        public void AddNew(T newValue) {
            if (shouldReverse) AddOldInternal(newValue);
            else AddNewInternal(newValue);
        }

        protected abstract void AddOldInternal(T oldValue);
        protected abstract void AddNewInternal(T oldValue);

        public abstract void Log();

        protected void LogIntDiff(string varName, int? oldValue, int? newValue) {
            Console.WriteLine("{0} {1}: {2} ({3} -> {4})", name, varName, newValue - oldValue, oldValue, newValue);
        }

        protected void LogStringDiff(string varName, string oldValue, string newValue) {
            if (oldValue == "") oldValue = "-";
            if (newValue == "") newValue = "-";
            Console.WriteLine("{0} {1}: {2} -> {3}", name, varName, oldValue, newValue);
        }
    }

    class PrecedenceValueChange: ValueChange<(Trip, Trip)> {
        public List<(Trip, Trip)> oldViolations = new List<(Trip, Trip)>();
        public List<(Trip, Trip)> newViolations = new List<(Trip, Trip)>();

        public PrecedenceValueChange(string name, bool shouldReverse, Driver driver, Instance instance) : base(name, shouldReverse, driver, instance) { }

        protected override void AddOldInternal((Trip, Trip) trips) => AddSpecific(trips, instance, oldViolations);
        protected override void AddNewInternal((Trip, Trip) trips) => AddSpecific(trips, instance, newViolations);

        protected void AddSpecific((Trip, Trip) trips, Instance instance, List<(Trip, Trip)> violationsList) {
            if (!instance.TripSuccession[trips.Item1.Index, trips.Item2.Index]) {
                violationsList.Add(trips);
            }
        }

        public override void Log() {
            LogStringDiff("violations", ParseViolationsList(oldViolations), ParseViolationsList(newViolations));
            LogIntDiff("violation count", oldViolations.Count, newViolations.Count);
        }

        string ParseViolationsList(List<(Trip, Trip)> violationsList) {
            return string.Join(' ', violationsList.Select(violation => violation.Item1.Index + "-" + violation.Item2.Index));
        }
    }

    class ViolationValueChange : ValueChange<int> {
        public int OldViolationCount, NewViolationCount, OldViolationAmount, NewViolationAmount;
        List<int> oldValues = new List<int>();
        List<int> newValues = new List<int>();
        Func<int, Driver, int> getViolationAmount;

        public ViolationValueChange(string name, bool shouldReverse, Driver driver, Instance instance, Func<int, Driver, int> getViolationAmount) : base(name, shouldReverse, driver, instance) {
            this.getViolationAmount = getViolationAmount;
        }

        protected override void AddOldInternal(int oldValue) => AddSpecific(oldValue, oldValues, ref OldViolationCount, ref OldViolationAmount);
        protected override void AddNewInternal(int newValue) => AddSpecific(newValue, newValues, ref NewViolationCount, ref NewViolationAmount);

        protected void AddSpecific(int value, List<int> valueList, ref int violationCountVar, ref int violationAmountVar) {
            int violationAmount = getViolationAmount(value, driver);
            int violationCount = violationAmount > 0 ? 1 : 0;

            valueList.Add(value);
            violationAmountVar += violationAmount;
            violationCountVar += violationCount;
        }

        public override void Log() {
            LogStringDiff("value", ParseHelper.ToString(oldValues), ParseHelper.ToString(newValues));
            LogIntDiff("violation count", OldViolationCount, NewViolationCount);
            LogIntDiff("violation amount", OldViolationAmount, NewViolationAmount);
        }
    }
}

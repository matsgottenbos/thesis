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

        public static void NextIteration(SaInfo info) {
            IterationNum++;
            CurrentOperation = new OperationInfo(IterationNum, info);
        }

        public static void ResetIteration(SaInfo info) {
            CurrentOperation = new OperationInfo(IterationNum, info);
        }
    }

    class OperationInfo {
        public string Description;
        public readonly List<OperationPart> Parts = new List<OperationPart>();
        public OperationPart CurrentPart = null;
        readonly int iterationNum;
        readonly SaInfo info;

        public OperationInfo(int iterationNum, SaInfo info) {
            this.iterationNum = iterationNum;
            this.info = info;
        }

        public void StartPart(string partDescription, bool shouldReverse, Driver driver) {
            if (CurrentPart != null) Parts.Add(CurrentPart);
            CurrentPart = new OperationPart(iterationNum, partDescription, shouldReverse, this, driver, info);
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
        readonly SaInfo info;

        public OperationPart(int iterationNum, string description, bool shouldReverse, OperationInfo operation, Driver driver, SaInfo info) {
            this.iterationNum = iterationNum;
            Description = description;
            this.operation = operation;
            this.driver = driver;
            this.info = info;
            CheckedCurrent = new CheckedTotal();
            Normal = new NormalDiff(shouldReverse, driver, info);
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

        public void LogErrors(TotalInfo errorAmounts) {
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
            if (driver is InternalDriver internalDriver) {
                Console.WriteLine("Min contract time: {0}", internalDriver.MinContractTime);
                Console.WriteLine("Max contract time: {0}", internalDriver.MaxContractTime);
            } else {
                Console.WriteLine("Min contract time: -");
                Console.WriteLine("Max contract time: -");
            }
        }

        public static string ParseValuePairs(List<(int, int)> valuePairs) {
            return string.Join(" ", valuePairs.Select(valuePair => string.Format("({0}|{1})", valuePair.Item1, valuePair.Item2)));
        }
    }

    class CheckedTotal {
        public readonly TotalInfo Total = new TotalInfo();
        public string DriverPathString = "";
        public List<(int, int)> ShiftLengths = new List<(int, int)>();
        public List<int> RestTimes = new List<int>();

        public void Log() {
            Console.WriteLine("Driver path: {0}", DriverPathString);
            Console.WriteLine("Shift lengths: {0}", OperationPart.ParseValuePairs(ShiftLengths));
            Console.WriteLine("Rest times: {0}", ParseHelper.ToString(RestTimes));
            Console.WriteLine("Worked time: {0}", ShiftLengths.Select(shiftLengthPair => shiftLengthPair.Item1).Sum());
            Total.Log();
        }
    }

    class TotalInfo {
        public double? Cost, CostWithoutPenalty, Penalty;
        public DriverInfo DriverInfo;
        public PenaltyInfo PenaltyInfo;
        readonly bool isDiff;

        public TotalInfo(bool isDiff = false) {
            this.isDiff = isDiff;
        }

        public void Log(bool shouldLogZeros = true) {
            string diffStr = isDiff ? " diff" : "";

            if (shouldLogZeros || PenaltyInfo.PrecedenceViolationCount != 0) Console.WriteLine("Precedence violation count{0}: {1}", diffStr, PenaltyInfo.PrecedenceViolationCount);
            if (shouldLogZeros || PenaltyInfo.ShiftLengthViolationCount != 0) Console.WriteLine("SL violation count{0}: {1}", diffStr, PenaltyInfo.ShiftLengthViolationCount);
            if (shouldLogZeros || PenaltyInfo.ShiftLengthViolation != 0) Console.WriteLine("SL violation amount{0}: {1}", diffStr, PenaltyInfo.RestTimeViolationCount);
            if (shouldLogZeros || PenaltyInfo.RestTimeViolationCount != 0) Console.WriteLine("RT violation count{0}: {1}", diffStr, PenaltyInfo.RestTimeViolationCount);
            if (shouldLogZeros || PenaltyInfo.RestTimeViolation != 0) Console.WriteLine("RT violation amount{0}: {1}", diffStr, PenaltyInfo.RestTimeViolation);
            if (shouldLogZeros || PenaltyInfo.ContractTimeViolationCount != 0) Console.WriteLine("CT violation count{0}: {1}", diffStr, PenaltyInfo.ContractTimeViolationCount);
            if (shouldLogZeros || PenaltyInfo.ContractTimeViolation != 0) Console.WriteLine("CT violation amount{0}: {1}", diffStr, PenaltyInfo.ContractTimeViolation);
            if (shouldLogZeros || DriverInfo.ShiftCount != 0) Console.WriteLine("SC value{0}: {1}", diffStr, DriverInfo.ShiftCount);
            if (shouldLogZeros || PenaltyInfo.ShiftCountViolationAmount != 0) Console.WriteLine("SC violation amount{0}: {1}", diffStr, PenaltyInfo.ShiftCountViolationAmount);
            if (shouldLogZeros || PenaltyInfo.InvalidHotelCount != 0) Console.WriteLine("Invalid hotel count{0}: {1}", diffStr, PenaltyInfo.InvalidHotelCount);
            if (shouldLogZeros || Math.Abs(Cost.Value) > Config.FloatingPointMargin) Console.WriteLine("Cost{0}: {1}", diffStr, ParseHelper.ToString(Cost.Value));
            if (shouldLogZeros || Math.Abs(CostWithoutPenalty.Value) > Config.FloatingPointMargin) Console.WriteLine("Cost without penalty{0}: {1}", diffStr, ParseHelper.ToString(CostWithoutPenalty.Value));
            if (shouldLogZeros || Math.Abs(Penalty.Value) > Config.FloatingPointMargin) Console.WriteLine("Penalty{0}: {1}", diffStr, ParseHelper.ToString(Penalty.Value));
            if (shouldLogZeros || DriverInfo.WorkedTime != 0) Console.WriteLine("Worked time{0}: {1}", diffStr, DriverInfo.WorkedTime);
        }

        public static bool AreEqual(TotalInfo a, TotalInfo b) {
            return (
                IsFloatEqual(a.Cost, b.Cost) &&
                IsFloatEqual(a.CostWithoutPenalty, b.CostWithoutPenalty) &&
                IsFloatEqual(a.Penalty, b.Penalty) &&
                a.DriverInfo.WorkedTime == b.DriverInfo.WorkedTime &&
                a.DriverInfo.ShiftCount == b.DriverInfo.ShiftCount &&
                a.PenaltyInfo.PrecedenceViolationCount == b.PenaltyInfo.PrecedenceViolationCount &&
                a.PenaltyInfo.ShiftLengthViolationCount == b.PenaltyInfo.ShiftLengthViolationCount &&
                a.PenaltyInfo.ShiftLengthViolation == b.PenaltyInfo.ShiftLengthViolation &&
                a.PenaltyInfo.RestTimeViolationCount == b.PenaltyInfo.RestTimeViolationCount &&
                a.PenaltyInfo.RestTimeViolation == b.PenaltyInfo.RestTimeViolation &&
                a.PenaltyInfo.ContractTimeViolationCount == b.PenaltyInfo.ContractTimeViolationCount &&
                a.PenaltyInfo.ContractTimeViolation == b.PenaltyInfo.ContractTimeViolation &&
                a.PenaltyInfo.ShiftCountViolationAmount == b.PenaltyInfo.ShiftCountViolationAmount &&
                a.PenaltyInfo.InvalidHotelCount == b.PenaltyInfo.InvalidHotelCount
            );
        }
        static bool IsFloatEqual(double? a, double? b) {
            return Math.Abs(a.Value - b.Value) < 0.01;
        }

        public static TotalInfo operator -(TotalInfo a) {
            return new TotalInfo(true) {
                Cost = -a.Cost,
                CostWithoutPenalty = -a.CostWithoutPenalty,
                Penalty = -a.Penalty,
                DriverInfo = new DriverInfo() {
                    WorkedTime = -a.DriverInfo.WorkedTime,
                    ShiftCount = -a.DriverInfo.ShiftCount,
                },
                PenaltyInfo = new PenaltyInfo() {
                    PrecedenceViolationCount = -a.PenaltyInfo.PrecedenceViolationCount,
                    ShiftLengthViolationCount = -a.PenaltyInfo.ShiftLengthViolationCount,
                    ShiftLengthViolation = -a.PenaltyInfo.ShiftLengthViolation,
                    RestTimeViolationCount = -a.PenaltyInfo.RestTimeViolationCount,
                    RestTimeViolation = -a.PenaltyInfo.RestTimeViolation,
                    ContractTimeViolationCount = -a.PenaltyInfo.ContractTimeViolationCount,
                    ContractTimeViolation = -a.PenaltyInfo.ContractTimeViolation,
                    ShiftCountViolationAmount = -a.PenaltyInfo.ShiftCountViolationAmount,
                    InvalidHotelCount = -a.PenaltyInfo.InvalidHotelCount,
                },
            };
        }
        public static TotalInfo operator +(TotalInfo a, TotalInfo b) {
            return new TotalInfo(true) {
                Cost = a.Cost + b.Cost,
                CostWithoutPenalty = a.CostWithoutPenalty + b.CostWithoutPenalty,
                Penalty = a.Penalty + b.Penalty,
                DriverInfo = new DriverInfo() {
                    WorkedTime = a.DriverInfo.WorkedTime + b.DriverInfo.WorkedTime,
                    ShiftCount = a.DriverInfo.ShiftCount + b.DriverInfo.ShiftCount,
                },
                PenaltyInfo = new PenaltyInfo() {
                    PrecedenceViolationCount = a.PenaltyInfo.PrecedenceViolationCount + b.PenaltyInfo.PrecedenceViolationCount,
                    ShiftLengthViolationCount = a.PenaltyInfo.ShiftLengthViolationCount + b.PenaltyInfo.ShiftLengthViolationCount,
                    ShiftLengthViolation = a.PenaltyInfo.ShiftLengthViolation + b.PenaltyInfo.ShiftLengthViolation,
                    RestTimeViolationCount = a.PenaltyInfo.RestTimeViolationCount + b.PenaltyInfo.RestTimeViolationCount,
                    RestTimeViolation = a.PenaltyInfo.RestTimeViolation + b.PenaltyInfo.RestTimeViolation,
                    ContractTimeViolationCount = a.PenaltyInfo.ContractTimeViolationCount + b.PenaltyInfo.ContractTimeViolationCount,
                    ContractTimeViolation = a.PenaltyInfo.ContractTimeViolation + b.PenaltyInfo.ContractTimeViolation,
                    ShiftCountViolationAmount = a.PenaltyInfo.ShiftCountViolationAmount + b.PenaltyInfo.ShiftCountViolationAmount,
                    InvalidHotelCount = a.PenaltyInfo.InvalidHotelCount + b.PenaltyInfo.InvalidHotelCount,
                },
            };
        }
        public static TotalInfo operator -(TotalInfo a, TotalInfo b) => a + -b;
    }

    class NormalDiff {
        public double CostDiff, CostWithoutPenaltyDiff, PenaltyDiff;
        public PrecedenceValueChange Precedence;
        public ViolationPairValueChange ShiftLength;
        public ViolationValueChange RestTime, ContractTime, ShiftCount;
        public HotelValueChange Hotels;
        public string DriverPathString, RelevantRangeInfo;

        public NormalDiff(bool shouldReverse, Driver driver, SaInfo info) {
            Precedence = new PrecedenceValueChange("Precedence", shouldReverse, driver, info);
            ShiftLength = new ViolationPairValueChange("SL", shouldReverse, driver, info, (shiftLengthPair, _) => Math.Max(0, shiftLengthPair.Item1 - Config.MaxShiftLengthWithoutTravel) + Math.Max(0, shiftLengthPair.Item2 - Config.MaxShiftLengthWithTravel));
            RestTime = new ViolationValueChange("RT", shouldReverse, driver, info, (restTime, _) => Math.Max(0, Config.MinRestTime - restTime));
            ContractTime = new ViolationValueChange("CT", false, driver, info, (workedHours, driver) => driver.GetTotalContractTimeViolation(workedHours));
            ShiftCount = new ViolationValueChange("SC", false, driver, info, (shiftCount, _) => Math.Max(0, shiftCount - Config.DriverMaxShiftCount));
            Hotels = new HotelValueChange("Hotel", shouldReverse, driver, info);
        }

        public TotalInfo ToTotal() {
            return new TotalInfo(true) {
                Cost = CostDiff,
                CostWithoutPenalty = CostWithoutPenaltyDiff,
                Penalty = PenaltyDiff,
                DriverInfo = new DriverInfo() {
                    WorkedTime = ContractTime.GetValueSumDiff(),
                    ShiftCount = ShiftCount.GetValueSumDiff(),
                },
                PenaltyInfo = new PenaltyInfo() {
                    PrecedenceViolationCount = Precedence.NewViolations.Count - Precedence.OldViolations.Count,
                    ShiftLengthViolationCount = ShiftLength.NewViolationCount - ShiftLength.OldViolationCount,
                    ShiftLengthViolation = ShiftLength.NewViolationAmount - ShiftLength.OldViolationAmount,
                    RestTimeViolationCount = RestTime.NewViolationCount - RestTime.OldViolationCount,
                    RestTimeViolation = RestTime.NewViolationAmount - RestTime.OldViolationAmount,
                    ContractTimeViolationCount = ContractTime.NewViolationCount - ContractTime.OldViolationCount,
                    ContractTimeViolation = ContractTime.NewViolationAmount - ContractTime.OldViolationAmount,
                    ShiftCountViolationAmount = ShiftCount.NewViolationAmount - ShiftCount.OldViolationAmount,
                    InvalidHotelCount = Hotels.NewViolations.Count - Hotels.OldViolations.Count,
                },
            };
        }

        public void Log() {
            Console.WriteLine("Driver path: {0}", DriverPathString);
            Console.WriteLine(RelevantRangeInfo);
            LogValue("Cost diff", CostDiff);
            LogValue("Cost without penalty diff", CostWithoutPenaltyDiff);
            LogValue("Penalty diff", PenaltyDiff);

            Precedence.Log();
            ShiftLength.Log();
            RestTime.Log();
            ContractTime.Log();
            ShiftCount.Log();
            Hotels.Log();
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
        protected readonly SaInfo info;

        public ValueChange(string name, bool shouldReverse, Driver driver, SaInfo info) {
            this.name = name;
            this.shouldReverse = shouldReverse;
            this.driver = driver;
            this.info = info;
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

    class IntValueChange : ValueChange<int> {
        List<int> oldValues = new List<int>();
        List<int> newValues = new List<int>();

        public IntValueChange(string name, bool shouldReverse, Driver driver, SaInfo info) : base(name, shouldReverse, driver, info) { }

        protected override void AddOldInternal(int value) => AddSpecific(value, oldValues);
        protected override void AddNewInternal(int value) => AddSpecific(value, newValues);

        protected void AddSpecific(int value, List<int> valueList) {
            valueList.Add(value);
        }

        public override void Log() {
            LogStringDiff("value", ParseHelper.ToString(oldValues), ParseHelper.ToString(newValues));
        }
    }

    class PrecedenceValueChange: ValueChange<(Trip, Trip)> {
        public List<(Trip, Trip)> OldViolations = new List<(Trip, Trip)>();
        public List<(Trip, Trip)> NewViolations = new List<(Trip, Trip)>();

        public PrecedenceValueChange(string name, bool shouldReverse, Driver driver, SaInfo info) : base(name, shouldReverse, driver, info) { }

        protected override void AddOldInternal((Trip, Trip) trips) => AddSpecific(trips, OldViolations);
        protected override void AddNewInternal((Trip, Trip) trips) => AddSpecific(trips, NewViolations);

        protected void AddSpecific((Trip, Trip) trips, List<(Trip, Trip)> violationsList) {
            if (!info.Instance.IsValidPrecedence(trips.Item1, trips.Item2)) {
                violationsList.Add(trips);
            }
        }

        public override void Log() {
            LogStringDiff("violations", ParseViolationsList(OldViolations), ParseViolationsList(NewViolations));
            LogIntDiff("violation count", OldViolations.Count, NewViolations.Count);
        }

        string ParseViolationsList(List<(Trip, Trip)> violationsList) {
            return string.Join(' ', violationsList.Select(violation => violation.Item1.Index + "-" + violation.Item2.Index));
        }
    }

    class HotelValueChange : ValueChange<Trip> {
        public List<Trip> OldViolations = new List<Trip>();
        public List<Trip> NewViolations = new List<Trip>();

        public HotelValueChange(string name, bool shouldReverse, Driver driver, SaInfo info) : base(name, shouldReverse, driver, info) { }

        protected override void AddOldInternal(Trip trip) => AddSpecific(trip, OldViolations);
        protected override void AddNewInternal(Trip trip) => AddSpecific(trip, NewViolations);

        protected void AddSpecific(Trip trip, List<Trip> violationsList) {
            violationsList.Add(trip);
        }

        public override void Log() {
            LogStringDiff("violations", ParseViolationsList(OldViolations), ParseViolationsList(NewViolations));
            LogIntDiff("violation count", OldViolations.Count, NewViolations.Count);
        }

        string ParseViolationsList(List<Trip> violationsList) {
            return string.Join(' ', violationsList.Select(violation => violation.Index));
        }
    }

    class ViolationValueChange : ValueChange<int> {
        public int OldViolationCount, NewViolationCount, OldViolationAmount, NewViolationAmount;
        List<int> oldValues = new List<int>();
        List<int> newValues = new List<int>();
        Func<int, Driver, int> getViolationAmount;

        public ViolationValueChange(string name, bool shouldReverse, Driver driver, SaInfo info, Func<int, Driver, int> getViolationAmount) : base(name, shouldReverse, driver, info) {
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

        public int GetValueSumDiff() {
            return newValues.Sum() - oldValues.Sum();
        }
    }

    class ViolationPairValueChange : ValueChange<(int, int)> {
        public int OldViolationCount, NewViolationCount, OldViolationAmount, NewViolationAmount;
        List<(int, int)> oldValuePairs = new List<(int, int)>();
        List<(int, int)> newValuePairs = new List<(int, int)>();
        Func<(int, int), Driver, int> getViolationAmount;

        public ViolationPairValueChange(string name, bool shouldReverse, Driver driver, SaInfo info, Func<(int, int), Driver, int> getViolationAmount) : base(name, shouldReverse, driver, info) {
            this.getViolationAmount = getViolationAmount;
        }

        protected override void AddOldInternal((int, int) oldValuePair) => AddSpecific(oldValuePair, oldValuePairs, ref OldViolationCount, ref OldViolationAmount);
        protected override void AddNewInternal((int, int) newValuePair) => AddSpecific(newValuePair, newValuePairs, ref NewViolationCount, ref NewViolationAmount);

        protected void AddSpecific((int, int) valuePair, List<(int, int)> valuePairList, ref int violationCountVar, ref int violationAmountVar) {
            int violationAmount = getViolationAmount(valuePair, driver);
            int violationCount = violationAmount > 0 ? 1 : 0;

            valuePairList.Add(valuePair);
            violationAmountVar += violationAmount;
            violationCountVar += violationCount;
        }

        public override void Log() {
            LogStringDiff("value", OperationPart.ParseValuePairs(oldValuePairs), OperationPart.ParseValuePairs(newValuePairs));
            LogIntDiff("violation count", OldViolationCount, NewViolationCount);
            LogIntDiff("violation amount", OldViolationAmount, NewViolationAmount);
        }
    }
}

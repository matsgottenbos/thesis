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
            errorAmounts.Log(false, false);

            Console.WriteLine("\n* Normal diff *");
            Normal.ToTotal().Log(true);

            Console.WriteLine("\n* Checked diff *");
            checkedDiff.Log(true);

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
            Total.Log(false);
        }
    }

    class TotalInfo {
        public double? Cost, CostWithoutPenalty, Penalty, DriverSatisfaction, Satisfaction;
        public DriverInfo DriverInfo;
        public PenaltyInfo PenaltyInfo;

        public void Log(bool isDiff, bool shouldLogZeros = true) {
            DriverInfo.DebugLog(isDiff, shouldLogZeros);
            PenaltyInfo.DebugLog(isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(Cost.Value, "Cost", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(CostWithoutPenalty.Value, "Cost without penalty", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(Penalty.Value, "Penalty", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(DriverSatisfaction.Value, "Driver satisfaction", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(Satisfaction.Value, "Satisfaction", isDiff, shouldLogZeros);
        }

        public static bool AreEqual(TotalInfo a, TotalInfo b) {
            return (
                IsDoubleEqual(a.Cost, b.Cost) &&
                IsDoubleEqual(a.CostWithoutPenalty, b.CostWithoutPenalty) &&
                IsDoubleEqual(a.Penalty, b.Penalty) &&
                IsDoubleEqual(a.DriverSatisfaction, b.DriverSatisfaction) &&
                IsDoubleEqual(a.Satisfaction, b.Satisfaction) &&
                DriverInfo.AreEqual(a.DriverInfo, b.DriverInfo) &&
                PenaltyInfo.AreEqual(a.PenaltyInfo, b.PenaltyInfo)
            );
        }
        static bool IsDoubleEqual(double? a, double? b) {
            return Math.Abs(a.Value - b.Value) < 0.01;
        }

        public static TotalInfo operator -(TotalInfo a) {
            return new TotalInfo() {
                Cost = -a.Cost,
                CostWithoutPenalty = -a.CostWithoutPenalty,
                Penalty = -a.Penalty,
                DriverSatisfaction = -a.DriverSatisfaction,
                Satisfaction = -a.Satisfaction,
                DriverInfo = -a.DriverInfo,
                PenaltyInfo = -a.PenaltyInfo,
            };
        }
        public static TotalInfo operator +(TotalInfo a, TotalInfo b) {
            return new TotalInfo() {
                Cost = a.Cost + b.Cost,
                CostWithoutPenalty = a.CostWithoutPenalty + b.CostWithoutPenalty,
                Penalty = a.Penalty + b.Penalty,
                Satisfaction = a.Satisfaction + b.Satisfaction,
                DriverSatisfaction = a.DriverSatisfaction + b.DriverSatisfaction,
                DriverInfo = a.DriverInfo + b.DriverInfo,
                PenaltyInfo = a.PenaltyInfo + b.PenaltyInfo,
            };
        }
        public static TotalInfo operator -(TotalInfo a, TotalInfo b) => a + -b;
    }

    class NormalDiff {
        public double CostDiff, CostWithoutPenaltyDiff, PenaltyDiff, DriverSatisfactionDiff, SatisfactionDiff;
        public ViolationCountValueChange<(Trip, Trip)> Precedence;
        public ViolationAmountValueChange<(int, int)> ShiftLength;
        public ViolationAmountValueChange<int> RestTime, ContractTime, ShiftCount;
        public ValueChange<Trip> Hotels, InvalidHotels;
        public ValueChange<(Trip, Trip)> NightShifts, WeekendShifts;
        public ValueChange<int> TravelTime;
        public string DriverPathString, RelevantRangeInfo;

        public NormalDiff(bool shouldReverse, Driver driver, SaInfo info) {
            Precedence = new ViolationCountValueChange<(Trip, Trip)>("Precedence", shouldReverse, driver, info, (tripPair, _) => !info.Instance.IsValidPrecedence(tripPair.Item1, tripPair.Item2));
            ShiftLength = new ViolationAmountValueChange<(int, int)>("SL", shouldReverse, driver, info, (shiftLengthPair, _) => Math.Max(0, shiftLengthPair.Item1 - Config.MaxShiftLengthWithoutTravel) + Math.Max(0, shiftLengthPair.Item2 - Config.MaxShiftLengthWithTravel));
            RestTime = new ViolationAmountValueChange<int>("RT", shouldReverse, driver, info, (restTime, _) => Math.Max(0, Config.MinRestTime - restTime));
            ContractTime = new ViolationAmountValueChange<int>("CT", false, driver, info, (workedHours, driver) => driver.GetTotalContractTimeViolation(workedHours));
            ShiftCount = new ViolationAmountValueChange<int>("SC", false, driver, info, (shiftCount, _) => Math.Max(0, shiftCount - Config.DriverMaxShiftCount));
            Hotels = new ValueChange<Trip>("Hotel", shouldReverse, driver, info);
            InvalidHotels = new ValueChange<Trip>("Invalid hotel", shouldReverse, driver, info);
            NightShifts = new ValueChange<(Trip, Trip)>("Night shift", shouldReverse, driver, info);
            WeekendShifts = new ValueChange<(Trip, Trip)>("Weekend shift", shouldReverse, driver, info);
            TravelTime = new ValueChange<int>("Travel time", shouldReverse, driver, info);
        }

        public TotalInfo ToTotal() {
            return new TotalInfo() {
                Cost = CostDiff,
                CostWithoutPenalty = CostWithoutPenaltyDiff,
                Penalty = PenaltyDiff,
                DriverSatisfaction = DriverSatisfactionDiff,
                Satisfaction = SatisfactionDiff,
                DriverInfo = new DriverInfo() {
                    WorkedTime = ContractTime.GetValueSumDiff(),
                    ShiftCount = ShiftCount.GetValueSumDiff(),
                    HotelCount = Hotels.GetCountDiff(),
                    NightShiftCount = NightShifts.GetCountDiff(),
                    WeekendShiftCount = WeekendShifts.GetCountDiff(),
                    TravelTime = TravelTime.GetValueSumDiff(),
                },
                PenaltyInfo = new PenaltyInfo() {
                    PrecedenceViolationCount = Precedence.GetViolationCountDiff(),
                    ShiftLengthViolationCount = ShiftLength.GetViolationCountDiff(),
                    ShiftLengthViolation = ShiftLength.GetViolationAmountDiff(),
                    RestTimeViolationCount = RestTime.GetViolationCountDiff(),
                    RestTimeViolation = RestTime.GetViolationAmountDiff(),
                    ContractTimeViolationCount = ContractTime.GetViolationCountDiff(),
                    ContractTimeViolation = ContractTime.GetViolationAmountDiff(),
                    ShiftCountViolationAmount = ShiftCount.GetViolationAmountDiff(),
                    InvalidHotelCount = InvalidHotels.GetCountDiff(),
                },
            };
        }

        public void Log() {
            Console.WriteLine("Driver path: {0}", DriverPathString);
            Console.WriteLine(RelevantRangeInfo);
            LogValue("Cost diff", CostDiff);
            LogValue("Cost without penalty diff", CostWithoutPenaltyDiff);
            LogValue("Penalty diff", PenaltyDiff);
            LogValue("Driver satisfaction diff", DriverSatisfactionDiff);
            LogValue("Satisfaction diff", SatisfactionDiff);

            Precedence.Log();
            ShiftLength.Log();
            RestTime.Log();
            ContractTime.Log();
            ShiftCount.Log();
            InvalidHotels.Log();
            NightShifts.Log();
            WeekendShifts.Log();
            TravelTime.Log();
        }

        static void LogValue<T>(string name, T value) {
            if (value is double valueDouble) Console.WriteLine("{0}: {1}", name, ParseHelper.ToString(valueDouble));
            else Console.WriteLine("{0}: {1}", name, value);
        }
    }



    class ValueChange<T> {
        protected readonly string name;
        protected readonly bool shouldReverse;
        protected readonly Driver driver;
        protected readonly SaInfo info;
        protected readonly List<T> oldValues;
        protected readonly List<T> newValues;

        public ValueChange(string name, bool shouldReverse, Driver driver, SaInfo info) {
            this.name = name;
            this.shouldReverse = shouldReverse;
            this.driver = driver;
            this.info = info;
            oldValues = new List<T>();
            newValues = new List<T>();
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

        protected virtual void AddOldInternal(T value) => AddSpecific(value, oldValues);
        protected virtual void AddNewInternal(T value) => AddSpecific(value, newValues);

        protected virtual void AddSpecific(T value, List<T> valueList) {
            valueList.Add(value);
        }

        public int GetCountDiff() {
            return newValues.Count - oldValues.Count;
        }

        public int GetValueSumDiff() {
            if (oldValues is List<int> oldValuesInt && newValues is List<int> newValuesInt) return newValuesInt.Sum() - oldValuesInt.Sum();
            else throw new Exception("Trying to sum over unknown value type");
        }

        public virtual void Log() {
            // Log values
            if (oldValues is List<int> oldValuesInt && newValues is List<int> newValuesInt) LogStringDiff("values", ParseHelper.ToString(oldValuesInt), ParseHelper.ToString(newValuesInt));
            else if (oldValues is List<double> oldValuesDouble && newValues is List<double> newValuesDouble) LogStringDiff("values", ParseHelper.ToString(oldValuesDouble), ParseHelper.ToString(newValuesDouble));
            else if (oldValues is List<(int, int)> oldValuesIntPair && newValues is List<(int, int)> newValuesIntPair) LogStringDiff("values", OperationPart.ParseValuePairs(oldValuesIntPair), OperationPart.ParseValuePairs(newValuesIntPair));
            else if (oldValues is List<Trip> oldValuesTrip && newValues is List<Trip> newValuesTrip) LogStringDiff("values", ParseViolationsList(oldValuesTrip), ParseViolationsList(newValuesTrip));
            else if (oldValues is List<(Trip, Trip)> oldValuesTripPair && newValues is List<(Trip, Trip)> newValuesTripPair) LogStringDiff("values", ParseViolationsList(oldValuesTripPair), ParseViolationsList(newValuesTripPair));
            else throw new Exception("Trying to log unknown value type");

            // Log count
            LogIntDiff("count", oldValues.Count, newValues.Count);
        }

        protected void LogIntDiff(string varName, int? oldValue, int? newValue) {
            Console.WriteLine("{0} {1}: {2} ({3} -> {4})", name, varName, newValue - oldValue, oldValue, newValue);
        }

        protected void LogStringDiff(string varName, string oldValue, string newValue) {
            if (oldValue == "") oldValue = "-";
            if (newValue == "") newValue = "-";
            Console.WriteLine("{0} {1}: {2} -> {3}", name, varName, oldValue, newValue);
        }

        static string ParseViolationsList(List<Trip> tripList) {
            return string.Join(' ', tripList.Select(trip => trip.Index));
        }
        static string ParseViolationsList(List<(Trip, Trip)> tripPairList) {
            return string.Join(' ', tripPairList.Select(tripPair => tripPair.Item1.Index + "-" + tripPair.Item2.Index));
        }
    }

    class ViolationCountValueChange<T> : ValueChange<T> {
        protected readonly List<T> oldViolations, newViolations;
        readonly Func<T, Driver, bool> isViolation;

        public ViolationCountValueChange(string name, bool shouldReverse, Driver driver, SaInfo info, Func<T, Driver, bool> isViolation) : base(name, shouldReverse, driver, info) {
            this.isViolation = isViolation;
            oldViolations = new List<T>();
            newViolations = new List<T>();
        }

        protected override void AddOldInternal(T oldValue) => AddSpecific(oldValue, oldValues, oldViolations);
        protected override void AddNewInternal(T newValue) => AddSpecific(newValue, newValues, newViolations);

        protected void AddSpecific(T value, List<T> valueList, List<T> violationList) {
            valueList.Add(value);

            if (isViolation(value, driver)) {
                violationList.Add(value);
            }
        }

        public override void Log() {
            base.Log();
            LogIntDiff("violation count", oldViolations.Count, newViolations.Count);
        }

        public int GetViolationCountDiff() {
            return newViolations.Count - oldViolations.Count;
        }
    }

    class ViolationAmountValueChange<T> : ViolationCountValueChange<T> {
        int oldViolationAmount, newViolationAmount;
        readonly Func<T, Driver, int> getViolationAmount;

        public ViolationAmountValueChange(string name, bool shouldReverse, Driver driver, SaInfo info, Func<T, Driver, int> getViolationAmount) : base(name, shouldReverse, driver, info, (T value, Driver driver) => getViolationAmount(value, driver) > 0) {
            this.getViolationAmount = getViolationAmount;
        }

        protected override void AddOldInternal(T oldValue) => AddSpecificWithAmount(oldValue, oldValues, oldViolations, ref oldViolationAmount);
        protected override void AddNewInternal(T newValue) => AddSpecificWithAmount(newValue, newValues, newViolations, ref newViolationAmount);

        void AddSpecificWithAmount(T value, List<T> valueList, List<T> violationList, ref int violationAmountVar) {
            AddSpecific(value, valueList, violationList);

            int violationAmount = getViolationAmount(value, driver);
            violationAmountVar += violationAmount;
        }

        public override void Log() {
            base.Log();
            LogIntDiff("violation amount", oldViolationAmount, newViolationAmount);
        }

        public int GetViolationAmountDiff() {
            return newViolationAmount - oldViolationAmount;
        }
    }
}

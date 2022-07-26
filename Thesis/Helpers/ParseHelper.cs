using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Thesis {
    static class ParseHelper {
        public static string ToString(double num, string format = "0.0000") {
            return num.ToString(format, CultureInfo.InvariantCulture);
        }

        public static string ToString(double[] numArray, string format = "0.0000") {
            return string.Join(" ", numArray.Select(num => ToString(num, format)));
        }
        public static string ToString(List<double> numList, string format = "0.0000") {
            return ToString(numList, format);
        }

        public static string ToString(int[] numArray) {
            return string.Join(" ", numArray);
        }
        public static string ToString(List<int> numList) {
            return string.Join(" ", numList);
        }

        public static string LargeNumToString(double num, string format = "0.##") {
            if (num < 1000) {
                return ToString(num, format);
            } else if (num < 1000000) {
                double numThousands = num / 1000;
                return ToString(numThousands, format) + "k";
            } else if (num < 1000000000) {
                double numMillions = num / 1000000;
                return ToString(numMillions, format) + "M";
            } else {
                double numMBllions = num / 1000000000;
                return ToString(numMBllions, format) + "B";
            }
        }

        public static string ActivityToIndexOrUnderscore(Activity activity) {
            if (activity == null) return "_";
            return activity.Index.ToString();
        }

        public static string AssignmentToString(SaInfo info) {
            string[] assignmentStrParts = new string[info.Assignment.Length];
            for (int activityIndex = 0; activityIndex < info.Instance.Activities.Length; activityIndex++) {
                Driver driver = info.Assignment[activityIndex];
                assignmentStrParts[activityIndex] = driver.GetId();
                if (info.IsHotelStayAfterActivity[activityIndex]) assignmentStrParts[activityIndex] += "h";
            }
            return string.Join(" ", assignmentStrParts);
        }

        public static string DriverPathToString(List<Activity> driverPath, SaInfo info) {
            string driverPathStr = "";
            Activity prevActivity = null;
            for (int driverActivityIndex = 0; driverActivityIndex < driverPath.Count; driverActivityIndex++) {
                Activity activity = driverPath[driverActivityIndex];

                if (prevActivity != null) {
                    if (info.Instance.AreSameShift(prevActivity, activity)) {
                        driverPathStr += "-";
                        if (info.IsHotelStayAfterActivity[prevActivity.Index]) driverPathStr += "H-";
                    } else {
                        driverPathStr += "|";
                        if (info.IsHotelStayAfterActivity[prevActivity.Index]) driverPathStr += "H|";
                    }
                }
                driverPathStr += activity.Index;

                prevActivity = activity;
            }
            if (prevActivity != null && info.IsHotelStayAfterActivity[prevActivity.Index]) driverPathStr += "|H";
            return driverPathStr;
        }

        public static string GetPenaltyString(SaTotalInfo totalInfo) {
            return GetPenaltyString(totalInfo.Stats.Penalty, totalInfo.PenaltyInfo, totalInfo.ExternalShiftCountViolationAmount);
        }
        public static string GetPenaltyString(SaDriverInfo driverInfo) {
            return GetPenaltyString(driverInfo.Stats.Penalty, driverInfo.PenaltyInfo);
        }
        public static string GetPenaltyString(double penalty, SaDriverPenaltyInfo penaltyInfo, int externalShiftCountViolationAmount = 0) {
            string penaltyString = string.Format("{0,6}", "-");
            if (penalty > 0) {
                List<string> penaltyTypes = new List<string>();
                if (penaltyInfo.OverlapViolationCount > 0) penaltyTypes.Add("Ov" + penaltyInfo.OverlapViolationCount);
                if (penaltyInfo.ShiftLengthViolationCount > 0) penaltyTypes.Add("Sl" + penaltyInfo.ShiftLengthViolationCount);
                if (penaltyInfo.RestTimeViolationCount > 0) penaltyTypes.Add("Rt" + penaltyInfo.RestTimeViolationCount);
                if (penaltyInfo.ShiftCountViolationAmount > 0) penaltyTypes.Add("Sc" + penaltyInfo.ShiftCountViolationAmount);
                if (penaltyInfo.InvalidHotelCount > 0) penaltyTypes.Add("Ih" + penaltyInfo.InvalidHotelCount);
                if (penaltyInfo.AvailabilityViolationCount > 0) penaltyTypes.Add("Av" + penaltyInfo.AvailabilityViolationCount);
                if (penaltyInfo.QualificationViolationCount > 0) penaltyTypes.Add("Qu" + penaltyInfo.QualificationViolationCount);
                if (externalShiftCountViolationAmount > 0) penaltyTypes.Add("Ec" + externalShiftCountViolationAmount);
                string penaltyTypesStr = string.Join(" ", penaltyTypes);

                penaltyString = string.Format("{0,6} ({1})", LargeNumToString(penalty, "0.0"), penaltyTypesStr);
            };
            return penaltyString;
        }


        /* Parsing data */

        public static string CleanDataString(string rawString) {
            return rawString.Trim();
        }

        public static string[] SplitAndCleanDataStringList(string dataStringList) {
            string[] splitDataStrings = dataStringList.Split(';');
            for (int i = 0; i < splitDataStrings.Length; i++) {
                splitDataStrings[i] = CleanDataString(splitDataStrings[i]);
            }
            return splitDataStrings;
        }

        public static bool DataStringInList(string rawString, string[] list) {
            return list.Contains(CleanDataString(rawString));
        }

        public static long ParseLargeNumString(string largeNumStr) {
            Match plainMatch = Regex.Match(largeNumStr, @"^([\d\.]+)$");
            if (plainMatch.Success) {
                return (long)float.Parse(plainMatch.Groups[1].Value);
            }

            Match thousandMatch = Regex.Match(largeNumStr, @"^(\d+)k$");
            if (thousandMatch.Success) {
                return (long)(float.Parse(thousandMatch.Groups[1].Value) * 1000);
            }

            Match millionMatch = Regex.Match(largeNumStr, @"^(\d+)M$");
            if (millionMatch.Success) {
                return (long)(float.Parse(millionMatch.Groups[1].Value) * 1000000);
            }

            Match billionMatch = Regex.Match(largeNumStr, @"^(\d+)B$");
            if (billionMatch.Success) {
                return (long)(float.Parse(billionMatch.Groups[1].Value) * 1000000000);
            }

            throw new Exception(string.Format("Could not parse large number `{0}`", largeNumStr));
        }

        /* Parsing assignment string */

        public static SaInfo ParseAssignmentString(string assignmentStr, Instance instance) {
            string[] driverStrings = assignmentStr.Split();
            SaInfo info = new SaInfo(instance);
            info.Assignment = new Driver[instance.Activities.Length];
            info.IsHotelStayAfterActivity = new bool[instance.Activities.Length];
            for (int activityIndex = 0; activityIndex < instance.Activities.Length; activityIndex++) {
                (Driver driver, bool isHotelStayAfter) = ParseDriverString(driverStrings[activityIndex], instance);
                info.Assignment[activityIndex] = driver;
                info.IsHotelStayAfterActivity[activityIndex] = isHotelStayAfter;
            }
            info.ProcessDriverPaths();

            TotalCostCalculator.ProcessAssignmentCost(info);

            return info;
        }

        static (Driver, bool) ParseDriverString(string driverStr, Instance instance) {
            Driver driver;
            if (driverStr[0] == 'e') {
                // External driver
                int typeIndex = int.Parse(Regex.Replace(driverStr, @"^e(\d+)\.(\d+)h?$", "$1"));
                int indexInType = int.Parse(Regex.Replace(driverStr, @"^e(\d+)\.(\d+)h?$", "$2"));
                driver = instance.ExternalDriversByType[typeIndex][indexInType];
            } else {
                // Internal driver
                int internalDriverIndex = int.Parse(Regex.Replace(driverStr, @"(\d+)h?", "$1"));
                driver = instance.InternalDrivers[internalDriverIndex];
            }
            bool isHotelStayAfter = Regex.Match(driverStr, @"h$").Success;
            return (driver, isHotelStayAfter);
        }


        /* Debug logging */

        public static void LogDebugValue(string value, string name, bool isDiff, bool isZero, bool shouldLogZeros) {
            string diffStr = isDiff ? " diff" : "";
            if (shouldLogZeros || !isZero) Console.WriteLine("{0}{1}: {2}", name, diffStr, value);
        }

        public static void LogDebugValue(int value, string name, bool isDiff, bool shouldLogZeros) {
            bool isZero = value == 0;
            LogDebugValue(value.ToString(), name, isDiff, isZero, shouldLogZeros);
        }
        public static void LogDebugValue(int? value, string name, bool isDiff, bool shouldLogZeros) {
            string valueStr = value.HasValue ? value.ToString() : "-";
            bool isZero = value == 0;
            LogDebugValue(valueStr, name, isDiff, isZero, shouldLogZeros);
        }

        public static void LogDebugValue(double value, string name, bool isDiff, bool shouldLogZeros) {
            bool isZero = Math.Abs(value) < DevConfig.FloatingPointMargin;
            LogDebugValue(value.ToString(), name, isDiff, isZero, shouldLogZeros);
        }
        public static void LogDebugValue(double? value, string name, bool isDiff, bool shouldLogZeros) {
            string valueStr = value.HasValue ? ToString(value.Value) : "-";
            bool isZero = !value.HasValue || Math.Abs(value.Value) < DevConfig.FloatingPointMargin;
            LogDebugValue(valueStr, name, isDiff, isZero, shouldLogZeros);
        }
    }
}

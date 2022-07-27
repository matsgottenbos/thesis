/*
 * Helper methods for converting values to strings
*/

using System.Globalization;

namespace DriverPlannerShared {
    public static class ToStringHelper {
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

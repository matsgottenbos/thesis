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

        public static string TripToIndexOrUnderscore(Trip trip) {
            if (trip == null) return "_";
            return trip.Index.ToString();
        }

        public static string AssignmentToString(SaInfo info) {
            string[] assignmentStrParts = new string[info.Assignment.Length];
            for (int tripIndex = 0; tripIndex < info.Instance.Trips.Length; tripIndex++) {
                Driver driver = info.Assignment[tripIndex];
                assignmentStrParts[tripIndex] = driver.GetId();
                if (info.IsHotelStayAfterTrip[tripIndex]) assignmentStrParts[tripIndex] += "h";
            }
            return string.Join(" ", assignmentStrParts);
        }

        public static string DriverPathToString(List<Trip> driverPath, SaInfo info) {
            string driverPathStr = "";
            Trip prevTrip = null;
            for (int driverTripIndex = 0; driverTripIndex < driverPath.Count; driverTripIndex++) {
                Trip trip = driverPath[driverTripIndex];

                if (prevTrip != null) {
                    if (info.Instance.AreSameShift(prevTrip, trip)) {
                        driverPathStr += "-";
                        if (info.IsHotelStayAfterTrip[prevTrip.Index]) driverPathStr += "H-";
                    } else {
                        driverPathStr += "|";
                        if (info.IsHotelStayAfterTrip[prevTrip.Index]) driverPathStr += "H|";
                    }
                }
                driverPathStr += trip.Index;

                prevTrip = trip;
            }
            if (prevTrip != null && info.IsHotelStayAfterTrip[prevTrip.Index]) driverPathStr += "|H";
            return driverPathStr;
        }

        public static string GetPenaltyString(SaTotalInfo totalInfo) {
            return GetPenaltyString(totalInfo.Stats.Penalty, totalInfo.PenaltyInfo, totalInfo.ExternalShiftCountViolationAmount);
        }
        public static string GetPenaltyString(SaDriverInfo driverInfo) {
            return GetPenaltyString(driverInfo.Stats.Penalty, driverInfo.PenaltyInfo);
        }
        public static string GetPenaltyString(double penalty, DriverPenaltyInfo penaltyInfo, int externalShiftCountViolationAmount = 0) {
            string penaltyString = string.Format("{0,6}", "-");
            if (penalty > 0) {
                List<string> penaltyTypes = new List<string>();
                if (penaltyInfo.PrecedenceViolationCount > 0) penaltyTypes.Add("Pr" + penaltyInfo.PrecedenceViolationCount);
                if (penaltyInfo.ShiftLengthViolationCount > 0) penaltyTypes.Add("Sl" + penaltyInfo.ShiftLengthViolationCount);
                if (penaltyInfo.RestTimeViolationCount > 0) penaltyTypes.Add("Rt" + penaltyInfo.RestTimeViolationCount);
                if (penaltyInfo.ShiftCountViolationAmount > 0) penaltyTypes.Add("Sc" + penaltyInfo.ShiftCountViolationAmount);
                if (penaltyInfo.InvalidHotelCount > 0) penaltyTypes.Add("Ih" + penaltyInfo.InvalidHotelCount);
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

        public static bool DataStringInList(string rawString, string[] list) {
            return list.Contains(CleanDataString(rawString));
        }

        /* Parsing assignment string */

        public static SaInfo ParseAssignmentString(string assignmentStr, Instance instance) {
            string[] driverStrings = assignmentStr.Split();
            SaInfo info = new SaInfo(instance);
            info.Assignment = new Driver[instance.Trips.Length];
            info.IsHotelStayAfterTrip = new bool[instance.Trips.Length];
            for (int tripIndex = 0; tripIndex < instance.Trips.Length; tripIndex++) {
                (Driver driver, bool isHotelStayAfter) = ParseDriverString(driverStrings[tripIndex], instance);
                info.Assignment[tripIndex] = driver;
                info.IsHotelStayAfterTrip[tripIndex] = isHotelStayAfter;
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

        public static void LogDebugValue(int value, string name, bool isDiff, bool shouldLogZeros) {
            string diffStr = isDiff ? " diff" : "";
            bool isZero = value == 0;
            if (shouldLogZeros || !isZero) Console.WriteLine("{0}{1}: {2}", name, diffStr, value);
        }
        public static void LogDebugValue(double value, string name, bool isDiff, bool shouldLogZeros) {
            string diffStr = isDiff ? " diff" : "";
            bool isZero = Math.Abs(value) < Config.FloatingPointMargin;
            if (shouldLogZeros || !isZero) Console.WriteLine("{0}{1}: {2}", name, diffStr, ToString(value));
        }
        public static void LogDebugValue(int? value, string name, bool isDiff, bool shouldLogZeros) {
            string valueStr = value.HasValue ? value.ToString() : "-";
            string diffStr = isDiff ? " diff" : "";
            bool isZero = value == 0;
            if (shouldLogZeros || !isZero) Console.WriteLine("{0}{1}: {2}", name, diffStr, valueStr);
        }
        public static void LogDebugValue(double? value, string name, bool isDiff, bool shouldLogZeros) {
            string valueStr = value.HasValue ? ToString(value.Value) : "-";
            string diffStr = isDiff ? " diff" : "";
            bool isZero = !value.HasValue || Math.Abs(value.Value) < Config.FloatingPointMargin;
            if (shouldLogZeros || !isZero) Console.WriteLine("{0}{1}: {2}", name, diffStr, valueStr);
        }
    }
}

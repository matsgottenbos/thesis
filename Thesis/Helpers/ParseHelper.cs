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
        public static string ToString(int num) {
            throw new Exception("Redundant parsing of int to string");
        }
        public static string ToString(int[] numArray) {
            return string.Join(" ", numArray);
        }
        public static string ToString(List<int> numArray) {
            return string.Join(" ", numArray);
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

        public static string AssignmentToString(Driver[] assignment, SaInfo info) {
            string[] assignmentStrParts = new string[assignment.Length];
            for (int tripIndex = 0; tripIndex < info.Instance.Trips.Length; tripIndex++) {
                Driver driver = assignment[tripIndex];
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

        public static string GetPenaltyString(SaInfo info) {
            return GetPenaltyString(info.Penalty, info.PenaltyInfo);
        }
        public static string GetPenaltyString(double penalty, PenaltyInfo penaltyInfo) {
            string penaltyString = "-";
            if (penalty > 0) {
                List<string> penaltyTypes = new List<string>();
                if (penaltyInfo.PrecedenceViolationCount > 0) penaltyTypes.Add("Pr " + penaltyInfo.PrecedenceViolationCount);
                if (penaltyInfo.ShiftLengthViolationCount > 0) penaltyTypes.Add("SL " + penaltyInfo.ShiftLengthViolationCount);
                if (penaltyInfo.RestTimeViolationCount > 0) penaltyTypes.Add("RT " + penaltyInfo.RestTimeViolationCount);
                if (penaltyInfo.ContractTimeViolationCount > 0) penaltyTypes.Add("CT " + penaltyInfo.ContractTimeViolationCount);
                if (penaltyInfo.ShiftCountViolationAmount > 0) penaltyTypes.Add("SC " + penaltyInfo.ShiftCountViolationAmount);
                if (penaltyInfo.InvalidHotelCount > 0) penaltyTypes.Add("IH " + penaltyInfo.InvalidHotelCount);
                string penaltyTypesStr = string.Join(", ", penaltyTypes);

                penaltyString = string.Format("{0} ({1})", ToString(penalty, "0"), penaltyTypesStr);
            };
            return penaltyString;
        }


        /* Parsing assignment string */

        public static (Driver[], bool[]) ParseAssignmentString(string assignmentStr, Instance instance) {
            string[] driverStrings = assignmentStr.Split();
            Driver[] assignment = new Driver[instance.Trips.Length];
            bool[] isHotelStayAfterTrip = new bool[instance.Trips.Length];
            for (int tripIndex = 0; tripIndex < instance.Trips.Length; tripIndex++) {
                (Driver driver, bool isHotelStayAfter) = ParseDriverString(driverStrings[tripIndex], instance);
                assignment[tripIndex] = driver;
                isHotelStayAfterTrip[tripIndex] = isHotelStayAfter;
            }
            return (assignment, isHotelStayAfterTrip);
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
    }
}

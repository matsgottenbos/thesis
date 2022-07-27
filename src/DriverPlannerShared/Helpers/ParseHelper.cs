/*
 * Helper methods for converting strings to other values
*/

using System.Text.RegularExpressions;

namespace DriverPlannerShared {
    public static class ParseHelper {
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
    }
}

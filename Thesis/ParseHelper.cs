﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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

        public static string AssignmentToString(Driver[] assignment, SaInfo info) {
            string[] assignmentStrParts = new string[assignment.Length];
            for (int tripIndex = 0; tripIndex < info.Instance.Trips.Length; tripIndex++) {
                Driver driver = assignment[tripIndex];
                assignmentStrParts[tripIndex] = driver.GetId();
                if (info.IsHotelStayAfterTrip[tripIndex]) assignmentStrParts[tripIndex] += "h";
            }
            return string.Join(" ", assignmentStrParts);
        }
    }
}

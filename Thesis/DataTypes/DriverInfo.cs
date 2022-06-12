﻿using System;
using System.Linq;

namespace Thesis {
    class DriverInfo {
        public readonly Instance Instance;
        public double Cost, RawCost, Robustness, Penalty, DriverSatisfaction;
        public double? SatisfactionScore;
        public int WorkedTime, ShiftCount, HotelCount, NightShiftCountByCompanyRules, WeekendShiftCountByCompanyRules, TravelTime, SingleFreeDays, DoubleFreeDays;
        public int[] SharedRouteCounts;
        public PenaltyInfo PenaltyInfo;

        public DriverInfo(Instance instance) {
            Instance = instance;
            PenaltyInfo = new PenaltyInfo();
            SharedRouteCounts = new int[instance.UniqueSharedRouteCount];
        }

        public static DriverInfo operator -(DriverInfo a) {
            return new DriverInfo(a.Instance) {
                Cost = -a.Cost,
                RawCost = -a.RawCost,
                Robustness = -a.Robustness,
                Penalty = -a.Penalty,
                DriverSatisfaction = -a.DriverSatisfaction,
                SatisfactionScore = a.SatisfactionScore.HasValue ? -a.SatisfactionScore.Value : null,
                WorkedTime = -a.WorkedTime,
                ShiftCount = -a.ShiftCount,
                HotelCount = -a.HotelCount,
                NightShiftCountByCompanyRules = -a.NightShiftCountByCompanyRules,
                WeekendShiftCountByCompanyRules = -a.WeekendShiftCountByCompanyRules,
                TravelTime = -a.TravelTime,
                SingleFreeDays = -a.SingleFreeDays,
                DoubleFreeDays = -a.DoubleFreeDays,
                SharedRouteCounts = InvertArray(a.SharedRouteCounts),
                PenaltyInfo = -a.PenaltyInfo,
            };
        }
        public static DriverInfo operator +(DriverInfo a, DriverInfo b) {
            return new DriverInfo(a.Instance) {
                Cost = a.Cost + b.Cost,
                RawCost = a.RawCost + b.RawCost,
                Robustness = a.Robustness + b.Robustness,
                Penalty = a.Penalty + b.Penalty,
                DriverSatisfaction = a.DriverSatisfaction + b.DriverSatisfaction,
                SatisfactionScore = a.SatisfactionScore.HasValue && b.SatisfactionScore.HasValue ? a.SatisfactionScore.Value + b.SatisfactionScore.Value : null,
                WorkedTime = a.WorkedTime + b.WorkedTime,
                ShiftCount = a.ShiftCount + b.ShiftCount,
                HotelCount = a.HotelCount + b.HotelCount,
                NightShiftCountByCompanyRules = a.NightShiftCountByCompanyRules + b.NightShiftCountByCompanyRules,
                WeekendShiftCountByCompanyRules = a.WeekendShiftCountByCompanyRules + b.WeekendShiftCountByCompanyRules,
                TravelTime = a.TravelTime + b.TravelTime,
                SingleFreeDays = a.SingleFreeDays + b.SingleFreeDays,
                DoubleFreeDays = a.DoubleFreeDays + b.DoubleFreeDays,
                SharedRouteCounts = AddArrays(a.SharedRouteCounts, b.SharedRouteCounts),
                PenaltyInfo = a.PenaltyInfo + b.PenaltyInfo,
            };
        }
        public static DriverInfo operator -(DriverInfo a, DriverInfo b) => a + -b;

        public static bool AreEqual(DriverInfo a, DriverInfo b) {
            return (
                IsDoubleEqual(a.Cost, b.Cost) &&
                IsDoubleEqual(a.RawCost, b.RawCost) &&
                IsDoubleEqual(a.Robustness, b.Robustness) &&
                IsDoubleEqual(a.Penalty, b.Penalty) &&
                IsDoubleEqual(a.DriverSatisfaction, b.DriverSatisfaction) &&
                IsDoubleEqual(a.SatisfactionScore, b.SatisfactionScore) &&
                a.WorkedTime == b.WorkedTime &&
                a.ShiftCount == b.ShiftCount &&
                a.HotelCount == b.HotelCount &&
                a.NightShiftCountByCompanyRules == b.NightShiftCountByCompanyRules &&
                a.WeekendShiftCountByCompanyRules == b.WeekendShiftCountByCompanyRules &&
                a.TravelTime == b.TravelTime &&
                a.SingleFreeDays == b.SingleFreeDays &&
                a.DoubleFreeDays == b.DoubleFreeDays &&
                AreArraysEqual(a.SharedRouteCounts, b.SharedRouteCounts) &&
                PenaltyInfo.AreEqual(a.PenaltyInfo, b.PenaltyInfo)
            );
        }

        static bool IsDoubleEqual(double a, double b) {
            return Math.Abs(a - b) < 0.01;
        }
        static bool IsDoubleEqual(double? a, double? b) {
            if (a.HasValue && b.HasValue) {
                return Math.Abs(a.Value - b.Value) < 0.01;
            }
            return !a.HasValue && !b.HasValue;
        }

        static int[] InvertArray(int[] array) {
            int[] invertedArray = new int[array.Length];
            for (int i = 0; i < array.Length; i++) {
                invertedArray[i] = -array[i];
            }
            return invertedArray;
        }

        static int[] AddArrays(int[] array1, int[] array2) {
            int[] addedArray = new int[array1.Length];
            for (int i = 0; i < array1.Length; i++) {
                addedArray[i] = array1[i] + array2[i];
            }
            return addedArray;
        }

        static bool AreArraysEqual(int[] array1, int[] array2) {
            for (int i = 0; i < array1.Length; i++) {
                if (array1[i] != array2[i]) return false;
            }
            return true;
        }

        public void DebugLog(bool isDiff, bool shouldLogZeros = true) {
            ParseHelper.LogDebugValue(Cost, "Cost", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(RawCost, "Raw cost", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(Robustness, "Robustness", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(Penalty, "Penalty", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(DriverSatisfaction, "Driver satisfaction", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(SatisfactionScore, "Satisfaction score", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(WorkedTime, "Worked time", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ShiftCount, "Shift count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(HotelCount, "Hotel count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(NightShiftCountByCompanyRules, "Night shift count (company rules)", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(WeekendShiftCountByCompanyRules, "Weekend shift count (company rules)", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(TravelTime, "Travel time", isDiff, shouldLogZeros);
            PenaltyInfo.DebugLog(isDiff, shouldLogZeros);
        }
    }
}

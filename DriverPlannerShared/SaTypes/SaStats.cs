/*
 * Stores costs, robustness and satisfaction information
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public class SaStats {
        public double Cost, RawCost, Robustness, Penalty, DriverSatisfaction;
        public double? SatisfactionScore;

        public static SaStats operator -(SaStats a) {
            return new SaStats() {
                Cost = -a.Cost,
                RawCost = -a.RawCost,
                Robustness = -a.Robustness,
                Penalty = -a.Penalty,
                DriverSatisfaction = -a.DriverSatisfaction,
                SatisfactionScore = a.SatisfactionScore.HasValue ? -a.SatisfactionScore.Value : null,
            };
        }
        public static SaStats operator +(SaStats a, SaStats b) {
            return new SaStats() {
                Cost = a.Cost + b.Cost,
                RawCost = a.RawCost + b.RawCost,
                Robustness = a.Robustness + b.Robustness,
                Penalty = a.Penalty + b.Penalty,
                DriverSatisfaction = a.DriverSatisfaction + b.DriverSatisfaction,
                SatisfactionScore = a.SatisfactionScore.HasValue && b.SatisfactionScore.HasValue ? a.SatisfactionScore.Value + b.SatisfactionScore.Value : null,
            };
        }
        public static SaStats operator -(SaStats a, SaStats b) => a + -b;

        public static bool AreEqual(SaStats a, SaStats b) {
            return (
                IsDoubleEqual(a.Cost, b.Cost) &&
                IsDoubleEqual(a.RawCost, b.RawCost) &&
                IsDoubleEqual(a.Robustness, b.Robustness) &&
                IsDoubleEqual(a.Penalty, b.Penalty) &&
                IsDoubleEqual(a.DriverSatisfaction, b.DriverSatisfaction) &&
                IsDoubleEqual(a.SatisfactionScore, b.SatisfactionScore)
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

        public void DebugLog(bool isDiff, bool shouldLogZeros = true) {
            ToStringHelper.LogDebugValue(Cost, "Cost", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(RawCost, "Raw cost", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(Robustness, "Robustness", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(Penalty, "Penalty", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(DriverSatisfaction, "Driver satisfaction", isDiff, shouldLogZeros);
            ToStringHelper.LogDebugValue(SatisfactionScore, "Satisfaction score", isDiff, shouldLogZeros);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public class SaTotalInfo {
        public SaStats Stats;
        public SaDriverPenaltyInfo PenaltyInfo;
        public int ExternalShiftCountViolationAmount;

        public SaTotalInfo() {
            Stats = new SaStats();
            PenaltyInfo = new SaDriverPenaltyInfo();
        }


        /* Adding */

        public void AddDriverInfo(SaDriverInfo driverInfo) {
            Stats += driverInfo.Stats;
            PenaltyInfo += driverInfo.PenaltyInfo;
        }

        public void AddExternalDriverTypeInfo(SaExternalDriverTypeInfo externalDriverTypeInfo) {
            Stats.Cost += externalDriverTypeInfo.Cost;
            Stats.Penalty += externalDriverTypeInfo.Penalty;
            ExternalShiftCountViolationAmount += externalDriverTypeInfo.ExternalShiftCountViolationAmount;
        }


        /* Operators */

        public static SaTotalInfo operator -(SaTotalInfo a) {
            return new SaTotalInfo() {
                Stats = -a.Stats,
                PenaltyInfo = -a.PenaltyInfo,
                ExternalShiftCountViolationAmount = -a.ExternalShiftCountViolationAmount,
            };
        }
        public static SaTotalInfo operator +(SaTotalInfo a, SaTotalInfo b) {
            return new SaTotalInfo() {
                Stats = a.Stats + b.Stats,
                PenaltyInfo = a.PenaltyInfo + b.PenaltyInfo,
                ExternalShiftCountViolationAmount = a.ExternalShiftCountViolationAmount + b.ExternalShiftCountViolationAmount,
            };
        }
        public static SaTotalInfo operator -(SaTotalInfo a, SaTotalInfo b) => a + -b;

        public static bool AreEqual(SaTotalInfo a, SaTotalInfo b) {
            return (
                SaStats.AreEqual(a.Stats, b.Stats) &&
                SaDriverPenaltyInfo.AreEqual(a.PenaltyInfo, b.PenaltyInfo) &&
                a.ExternalShiftCountViolationAmount == b.ExternalShiftCountViolationAmount
            );
        }


        /* Logging */

        public void DebugLog(bool isDiff, bool shouldLogZeros = true) {
            Stats.DebugLog(isDiff, shouldLogZeros);
            PenaltyInfo.DebugLog(isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ExternalShiftCountViolationAmount, "External shift count violation amount", isDiff, shouldLogZeros);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public class SaExternalDriverTypeInfo {
        public double Cost, Penalty;
        public int ExternalShiftCount, ExternalShiftCountViolationAmount;

        /* Adding violations */

        public void AddPotentialShiftCountViolation(int shiftCount, ExternalDriverType externalDriverType) {
            ExternalShiftCountViolationAmount += Math.Max(0, externalDriverType.MinShiftCount - shiftCount) + Math.Max(0, shiftCount - externalDriverType.MaxShiftCount);
            Penalty = ExternalShiftCountViolationAmount * SaConfig.ExternalShiftCountPenaltyPerShift;
            Cost = Penalty;
        }


        /* Operators */

        public static SaExternalDriverTypeInfo operator -(SaExternalDriverTypeInfo a) {
            return new SaExternalDriverTypeInfo() {
                Cost = -a.Cost,
                Penalty = -a.Penalty,
                ExternalShiftCount = -a.ExternalShiftCount,
                ExternalShiftCountViolationAmount = -a.ExternalShiftCountViolationAmount,
            };
        }
        public static SaExternalDriverTypeInfo operator +(SaExternalDriverTypeInfo a, SaExternalDriverTypeInfo b) {
            return new SaExternalDriverTypeInfo() {
                Cost = a.Cost + b.Cost,
                Penalty = a.Penalty + b.Penalty,
                ExternalShiftCount = a.ExternalShiftCount + b.ExternalShiftCount,
                ExternalShiftCountViolationAmount = a.ExternalShiftCountViolationAmount + b.ExternalShiftCountViolationAmount,
            };
        }
        public static SaExternalDriverTypeInfo operator -(SaExternalDriverTypeInfo a, SaExternalDriverTypeInfo b) => a + -b;

        public static bool AreEqual(SaExternalDriverTypeInfo a, SaExternalDriverTypeInfo b) {
            return (
                a.Cost == b.Cost &&
                a.Penalty == b.Penalty &&
                a.ExternalShiftCount == b.ExternalShiftCount &&
                a.ExternalShiftCountViolationAmount == b.ExternalShiftCountViolationAmount
            );
        }


        /* Debugging */

        public void DebugLog(bool isDiff, bool shouldLogZeros = true) {
            ParseHelper.LogDebugValue(Cost, "Cost", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(Penalty, "Penalty", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ExternalShiftCount, "External shift count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ExternalShiftCountViolationAmount, "External shift count violation amount", isDiff, shouldLogZeros);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class PenaltyHelper {
        public static float GetPrecedenceBasePenalty(Trip trip1, Trip trip2, SaInfo info, bool debugIsNew) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                if (debugIsNew) SaDebugger.GetCurrentNormalDiff().Precedence.AddNew((trip1, trip2));
                else SaDebugger.GetCurrentNormalDiff().Precedence.AddOld((trip1, trip2));
            }
            #endif

            if (info.Instance.TripSuccession[trip1.Index, trip2.Index]) return 0;
            else return Config.PrecendenceViolationPenalty;
        }

        public static float GetShiftLengthBasePenalty(int shiftLength, bool debugIsNew) {
            int shiftLengthViolation = Math.Max(0, shiftLength - Config.MaxShiftLength);
            float amountBasePenalty = shiftLengthViolation * Config.ShiftLengthViolationPenaltyPerMin;
            float countBasePenalty = shiftLengthViolation > 0 ? Config.ShiftLengthViolationPenalty : 0;

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                if (debugIsNew) SaDebugger.GetCurrentNormalDiff().ShiftLength.AddNew(shiftLength);
                else SaDebugger.GetCurrentNormalDiff().ShiftLength.AddOld(shiftLength);
            }
            #endif

            return amountBasePenalty + countBasePenalty;
        }

        public static float GetRestTimeBasePenaltyWithPickup(Trip shift1FirstTrip, Trip shift1LastTrip, Trip shift2FirstTrip, Driver driver, bool debugIsNew) {
            int restTime = driver.RestTimeWithPickup(shift1FirstTrip, shift1LastTrip, shift2FirstTrip);
            return GetRestTimeBasePenalty(restTime, debugIsNew);
        }
        public static float GetRestTimeBasePenalty(int restTime, bool debugIsNew) {
            float shiftLengthViolation = Math.Max(0, Config.MinRestTime - restTime);
            float amountBasePenalty = shiftLengthViolation * Config.RestTimeViolationPenaltyPerMin;
            float countBasePenalty = shiftLengthViolation > 0 ? Config.RestTimeViolationPenalty : 0;

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                if (debugIsNew) SaDebugger.GetCurrentNormalDiff().RestTime.AddNew(restTime);
                else SaDebugger.GetCurrentNormalDiff().RestTime.AddOld(restTime);
            }
            #endif

            return amountBasePenalty + countBasePenalty;
        }
    }
}

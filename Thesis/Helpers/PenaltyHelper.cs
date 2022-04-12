using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class PenaltyHelper {
        public static float GetPrecedencePenalty(Trip trip1, Trip trip2, SaInfo info, bool debugIsNew) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                if (debugIsNew) SaDebugger.GetCurrentNormalDiff().Precedence.AddNew((trip1, trip2));
                else SaDebugger.GetCurrentNormalDiff().Precedence.AddOld((trip1, trip2));
            }
            #endif

            if (info.Instance.IsValidPrecedence(trip1, trip2)) return 0;
            else return Config.PrecendenceViolationPenalty;
        }

        public static float GetShiftLengthPenalty(int shiftLengthWithoutTravel, int shiftLengthWithTravel, bool debugIsNew) {
            int shiftLengthViolation = Math.Max(0, shiftLengthWithoutTravel - Config.MaxShiftLengthWithoutTravel) + Math.Max(0, shiftLengthWithTravel - Config.MaxShiftLengthWithTravel);
            float countPenalty = shiftLengthViolation > 0 ? Config.ShiftLengthViolationPenalty : 0;
            float amountPenalty = shiftLengthViolation * Config.ShiftLengthViolationPenaltyPerMin;

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                if (debugIsNew) SaDebugger.GetCurrentNormalDiff().ShiftLength.AddNew((shiftLengthWithoutTravel, shiftLengthWithTravel));
                else SaDebugger.GetCurrentNormalDiff().ShiftLength.AddOld((shiftLengthWithoutTravel, shiftLengthWithTravel));
            }
            #endif

            return amountPenalty + countPenalty;
        }

        public static float GetRestTimePenalty(int restTime, bool debugIsNew) {
            float shiftLengthViolation = Math.Max(0, Config.MinRestTime - restTime);
            float countPenalty = shiftLengthViolation > 0 ? Config.RestTimeViolationPenalty : 0;
            float amountPenalty = shiftLengthViolation * Config.RestTimeViolationPenaltyPerMin;

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                if (debugIsNew) SaDebugger.GetCurrentNormalDiff().RestTime.AddNew(restTime);
                else SaDebugger.GetCurrentNormalDiff().RestTime.AddOld(restTime);
            }
            #endif

            return amountPenalty + countPenalty;
        }

        public static float GetShiftCountPenalty(int shiftCount, bool debugIsNew) {
            int shiftCountViolation = Math.Max(0, shiftCount - Config.DriverMaxShiftCount);
            float penalty = shiftCountViolation * Config.ShiftCountViolationPenaltyPerShift;

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                if (debugIsNew) SaDebugger.GetCurrentNormalDiff().ShiftCount.AddNew(shiftCount);
                else SaDebugger.GetCurrentNormalDiff().ShiftCount.AddOld(shiftCount);
            }
            #endif

            return penalty;
        }

        public static float GetHotelPenalty(Trip tripBeforeInvalidHotel, SaInfo info, bool debugIsNew) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                if (debugIsNew) SaDebugger.GetCurrentNormalDiff().Hotels.AddNew(tripBeforeInvalidHotel);
                else SaDebugger.GetCurrentNormalDiff().Hotels.AddOld(tripBeforeInvalidHotel);
            }
            #endif

            return Config.InvalidHotelPenalty;
        }
    }
}

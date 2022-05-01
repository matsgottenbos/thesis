using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class PenaltyHelper {
        public static double GetPrecedencePenalty(Trip trip1, Trip trip2, SaInfo info, bool debugIsNew) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                if (debugIsNew) SaDebugger.GetCurrentNormalDiff().Precedence.AddNew((trip1, trip2));
                else SaDebugger.GetCurrentNormalDiff().Precedence.AddOld((trip1, trip2));
            }
            #endif

            if (info.Instance.IsValidPrecedence(trip1, trip2)) return 0;
            else return Config.PrecendenceViolationPenalty;
        }

        public static double GetShiftLengthPenalty(int shiftLengthWithoutTravel, int shiftLengthWithTravel, bool debugIsNew) {
            int shiftLengthViolation = Math.Max(0, shiftLengthWithoutTravel - Config.MaxShiftLengthWithoutTravel) + Math.Max(0, shiftLengthWithTravel - Config.MaxShiftLengthWithTravel);
            double countPenalty = shiftLengthViolation > 0 ? Config.ShiftLengthViolationPenalty : 0;
            double amountPenalty = shiftLengthViolation * Config.ShiftLengthViolationPenaltyPerMin;

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                if (debugIsNew) SaDebugger.GetCurrentNormalDiff().ShiftLength.AddNew((shiftLengthWithoutTravel, shiftLengthWithTravel));
                else SaDebugger.GetCurrentNormalDiff().ShiftLength.AddOld((shiftLengthWithoutTravel, shiftLengthWithTravel));
            }
            #endif

            return amountPenalty + countPenalty;
        }

        public static double GetRestTimePenalty(int restTime, bool debugIsNew) {
            float shiftLengthViolation = Math.Max(0, Config.MinRestTime - restTime);
            double countPenalty = shiftLengthViolation > 0 ? Config.RestTimeViolationPenalty : 0;
            double amountPenalty = shiftLengthViolation * Config.RestTimeViolationPenaltyPerMin;

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                if (debugIsNew) SaDebugger.GetCurrentNormalDiff().RestTime.AddNew(restTime);
                else SaDebugger.GetCurrentNormalDiff().RestTime.AddOld(restTime);
            }
            #endif

            return amountPenalty + countPenalty;
        }

        public static double GetShiftCountPenalty(int shiftCount, bool debugIsNew) {
            int shiftCountViolation = Math.Max(0, shiftCount - Config.DriverMaxShiftCount);
            double penalty = shiftCountViolation * Config.ShiftCountViolationPenaltyPerShift;

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                if (debugIsNew) SaDebugger.GetCurrentNormalDiff().ShiftCount.AddNew(shiftCount);
                else SaDebugger.GetCurrentNormalDiff().ShiftCount.AddOld(shiftCount);
            }
            #endif

            return penalty;
        }

        public static double GetHotelPenalty(Trip tripBeforeInvalidHotel, SaInfo info, bool debugIsNew) {
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

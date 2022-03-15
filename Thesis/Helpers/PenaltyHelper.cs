using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class PenaltyHelper {
        public static float GetPrecedenceBasePenalty(Trip trip1, Trip trip2, Instance instance, bool debugIsNew) {
            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                if (debugIsNew) SaDebugger.GetCurrentNormalDiff().Precedence.AddNew((trip1, trip2));
                else SaDebugger.GetCurrentNormalDiff().Precedence.AddOld((trip1, trip2));
            }
            #endif

            if (instance.TripSuccession[trip1.Index, trip2.Index]) return 0;
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

        public static float GetRestTimeBasePenalty(Trip shift1FirstTrip, Trip shift1LastTrip, Trip shift2FirstTrip, Driver driver, Instance instance, bool debugIsNew) {
            int restTime = driver.RestTime(shift1FirstTrip, shift1LastTrip, shift2FirstTrip);
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

        public static float GetContractTimeBasePenaltyDiff(int oldWorkedTime, int newWorkedTime, Driver driver) {
            float contractTimeBasePenaltyDiff = 0;

            int oldContractTimeViolation = 0;
            if (oldWorkedTime < driver.MinContractTime) {
                oldContractTimeViolation += driver.MinContractTime - oldWorkedTime;
                contractTimeBasePenaltyDiff -= Config.ContractTimeViolationPenalty;
            } else if (oldWorkedTime > driver.MaxContractTime) {
                oldContractTimeViolation += oldWorkedTime - driver.MaxContractTime;
                contractTimeBasePenaltyDiff -= Config.ContractTimeViolationPenalty;
            }

            int newContractTimeViolation = 0;
            if (newWorkedTime < driver.MinContractTime) {
                newContractTimeViolation += driver.MinContractTime - newWorkedTime;
                contractTimeBasePenaltyDiff += Config.ContractTimeViolationPenalty;
            } else if (newWorkedTime > driver.MaxContractTime) {
                newContractTimeViolation += newWorkedTime - driver.MaxContractTime;
                contractTimeBasePenaltyDiff += Config.ContractTimeViolationPenalty;
            }

            #if DEBUG
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.GetCurrentNormalDiff().ContractTime.Add(oldWorkedTime, newWorkedTime);
            }
            #endif

            contractTimeBasePenaltyDiff += (newContractTimeViolation - oldContractTimeViolation) * Config.ContractTimeViolationPenaltyPerMin;

            return contractTimeBasePenaltyDiff;
        }
    }
}

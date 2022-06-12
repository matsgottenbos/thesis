using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class ShiftInfo {
        public readonly int DrivingTime, MaxShiftLengthWithoutTravel, MaxShiftLengthWithTravel, MinRestTimeAfter;
        public readonly float InternalDrivingCost, ExternalDrivingCost;
        public readonly bool IsNightShiftByLaw, IsNightShiftByCompanyRules, IsWeekendShiftByCompanyRules;

        public ShiftInfo(int drivingTime, int maxShiftLengthWithoutTravel, int maxShiftLengthWithTravel, int minRestTimeAfter, float internalDrivingCost, float externalDrivingCost, bool isNightShiftByLaw, bool isNightShiftByCompanyRules, bool isWeekendShiftByCompanyRules) {
            DrivingTime = drivingTime;
            MaxShiftLengthWithoutTravel = maxShiftLengthWithoutTravel;
            MaxShiftLengthWithTravel = maxShiftLengthWithTravel;
            MinRestTimeAfter = minRestTimeAfter;
            InternalDrivingCost = internalDrivingCost;
            ExternalDrivingCost = externalDrivingCost;
            IsNightShiftByLaw = isNightShiftByLaw;
            IsNightShiftByCompanyRules = isNightShiftByCompanyRules;
            IsWeekendShiftByCompanyRules = isWeekendShiftByCompanyRules;
        }
    }
}

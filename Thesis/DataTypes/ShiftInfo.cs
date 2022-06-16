using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class ShiftInfo {
        public readonly int DrivingTime, MaxShiftLengthWithoutTravel, MaxShiftLengthWithTravel, MinRestTimeAfter;
        public readonly float[] DrivingCostsByDriverType;
        public readonly bool IsNightShiftByLaw, IsNightShiftByCompanyRules, IsWeekendShiftByCompanyRules;

        public ShiftInfo(int drivingTime, int maxShiftLengthWithoutTravel, int maxShiftLengthWithTravel, int minRestTimeAfter, float[] drivingCostsByDriverType, bool isNightShiftByLaw, bool isNightShiftByCompanyRules, bool isWeekendShiftByCompanyRules) {
            DrivingTime = drivingTime;
            MaxShiftLengthWithoutTravel = maxShiftLengthWithoutTravel;
            MaxShiftLengthWithTravel = maxShiftLengthWithTravel;
            MinRestTimeAfter = minRestTimeAfter;
            DrivingCostsByDriverType = drivingCostsByDriverType;
            IsNightShiftByLaw = isNightShiftByLaw;
            IsNightShiftByCompanyRules = isNightShiftByCompanyRules;
            IsWeekendShiftByCompanyRules = isWeekendShiftByCompanyRules;
        }

        public float GetDrivingCost(int driverTypeIndex) {
            return DrivingCostsByDriverType[driverTypeIndex];
        }
    }
}

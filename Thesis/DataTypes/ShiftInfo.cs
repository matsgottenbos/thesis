using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class ShiftInfo {
        public readonly int DrivingTime, MaxShiftLengthWithoutTravel, MaxShiftLengthWithTravel, MinRestTimeAfter;
        public readonly int[] AdministrativeDrivingTimeByDriverType;
        public readonly float[] DrivingCostsByDriverType;
        public readonly bool IsNightShiftByLaw, IsNightShiftByCompanyRules, IsWeekendShiftByCompanyRules;
        public readonly List<ComputedSalaryRateBlock>[] ComputeSalaryRateBlocksByType;

        public ShiftInfo(int drivingTime, int maxShiftLengthWithoutTravel, int maxShiftLengthWithTravel, int minRestTimeAfter, int[] administrativeDrivingTimeByDriverType, float[] drivingCostsByDriverType, List<ComputedSalaryRateBlock>[] computeSalaryRateBlocksByType, bool isNightShiftByLaw, bool isNightShiftByCompanyRules, bool isWeekendShiftByCompanyRules) {
            DrivingTime = drivingTime;
            AdministrativeDrivingTimeByDriverType = administrativeDrivingTimeByDriverType;
            MaxShiftLengthWithoutTravel = maxShiftLengthWithoutTravel;
            MaxShiftLengthWithTravel = maxShiftLengthWithTravel;
            MinRestTimeAfter = minRestTimeAfter;
            DrivingCostsByDriverType = drivingCostsByDriverType;
            ComputeSalaryRateBlocksByType = computeSalaryRateBlocksByType;
            IsNightShiftByLaw = isNightShiftByLaw;
            IsNightShiftByCompanyRules = isNightShiftByCompanyRules;
            IsWeekendShiftByCompanyRules = isWeekendShiftByCompanyRules;
        }

        public float GetDrivingCost(int driverTypeIndex) {
            return DrivingCostsByDriverType[driverTypeIndex];
        }
    }
}

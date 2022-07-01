using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class ShiftInfo {
        public readonly int MainShiftLength, MaxMainShiftLength, MaxFullShiftLength, MinRestTimeAfter;
        public readonly int[] AdministrativeMainShiftLengthByDriverType;
        public readonly float[] MainShiftCostByDriverType;
        public readonly bool IsNightShiftByLaw, IsNightShiftByCompanyRules, IsWeekendShiftByCompanyRules;
        public readonly List<ComputedSalaryRateBlock>[] ComputeSalaryRateBlocksByType;

        public ShiftInfo(int mainShiftLength, int maxMainShiftLength, int maxFullShiftLength, int minRestTimeAfter, int[] administrativeMainShiftLengthByDriverType, float[] mainShiftCostByDriverType, List<ComputedSalaryRateBlock>[] computeSalaryRateBlocksByType, bool isNightShiftByLaw, bool isNightShiftByCompanyRules, bool isWeekendShiftByCompanyRules) {
            MainShiftLength = mainShiftLength;
            AdministrativeMainShiftLengthByDriverType = administrativeMainShiftLengthByDriverType;
            MaxMainShiftLength = maxMainShiftLength;
            MaxFullShiftLength = maxFullShiftLength;
            MinRestTimeAfter = minRestTimeAfter;
            MainShiftCostByDriverType = mainShiftCostByDriverType;
            ComputeSalaryRateBlocksByType = computeSalaryRateBlocksByType;
            IsNightShiftByLaw = isNightShiftByLaw;
            IsNightShiftByCompanyRules = isNightShiftByCompanyRules;
            IsWeekendShiftByCompanyRules = isWeekendShiftByCompanyRules;
        }

        public float GetMainShiftCost(int driverTypeIndex) {
            return MainShiftCostByDriverType[driverTypeIndex];
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    /*
    Terminology:
    Work shift: from first to last activity
    Main shift: work shift + pickup / hotel travel
    Full shift: main shift + home travel
    */

    public class MainShiftInfo {
        public readonly int RealMainShiftLength, MaxFullShiftLength, MinRestTimeAfter, MainShiftLengthViolationAmount;
        public readonly bool IsNightShiftByLaw, IsNightShiftByCompanyRules, IsWeekendShiftByCompanyRules;
        readonly DriverTypeMainShiftInfo[] mainShiftInfoByDriverType;

        public MainShiftInfo(int realMainShiftLength, int maxFullShiftLength, int minRestTimeAfter, int mainShiftLengthViolationAmount, bool isNightShiftByLaw, bool isNightShiftByCompanyRules, bool isWeekendShiftByCompanyRules, DriverTypeMainShiftInfo[] mainShiftInfoByDriverType) {
            RealMainShiftLength = realMainShiftLength;
            MaxFullShiftLength = maxFullShiftLength;
            MinRestTimeAfter = minRestTimeAfter;
            MainShiftLengthViolationAmount = mainShiftLengthViolationAmount;
            IsNightShiftByLaw = isNightShiftByLaw;
            IsNightShiftByCompanyRules = isNightShiftByCompanyRules;
            IsWeekendShiftByCompanyRules = isWeekendShiftByCompanyRules;
            this.mainShiftInfoByDriverType = mainShiftInfoByDriverType;
        }

        public DriverTypeMainShiftInfo ByDriver(Driver driver) {
            return mainShiftInfoByDriverType[driver.SalarySettings.DriverTypeIndex];
        }
    }

    public class DriverTypeMainShiftInfo {
        public readonly int PaidMainShiftLength;
        public readonly float MainShiftCost;
        public readonly List<ComputedSalaryRateBlock> MainShiftSalaryBlocks;

        public DriverTypeMainShiftInfo(int paidMainShiftLength, float mainShiftCost, List<ComputedSalaryRateBlock> mainShiftSalaryBlocks) {
            PaidMainShiftLength = paidMainShiftLength;
            MainShiftCost = mainShiftCost;
            MainShiftSalaryBlocks = mainShiftSalaryBlocks;
        }
    }
}

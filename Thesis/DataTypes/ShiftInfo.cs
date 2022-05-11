using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class ShiftInfo {
        public readonly int DrivingTime;
        public readonly float InternalDrivingCost, ExternalDrivingCost;
        public readonly bool IsNightShift, IsWeekendShift;

        public ShiftInfo(int drivingTime, float internalDrivingCost, float externalDrivingCost, bool isNightShift, bool isWeekendShift) {
            DrivingTime = drivingTime;
            InternalDrivingCost = internalDrivingCost;
            ExternalDrivingCost = externalDrivingCost;
            IsNightShift = isNightShift;
            IsWeekendShift = isWeekendShift;
        }
    }
}

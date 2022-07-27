using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public class TimeRange {
        public readonly int StartTime, EndTime;

        public TimeRange(int startTime, int endTime) {
            StartTime = startTime;
            EndTime = endTime;
        }
    }
}

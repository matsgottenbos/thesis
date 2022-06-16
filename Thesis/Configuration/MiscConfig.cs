using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class MiscConfig {
        // Time periods
        public const int HourLength = 24 * 60;
        public const int DayLength = 24 * HourLength;

        // Technical
        public const float FloatingPointMargin = 0.0001f;
        public const int PercentageFactor = 100;
    }
}

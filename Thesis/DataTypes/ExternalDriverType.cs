using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class ExternalDriverType {
        public readonly string CompanyName;
        public readonly bool IsInternational;
        public readonly int MinShiftCount, MaxShiftCount;

        public ExternalDriverType(string companyName, bool isInternational, int minShiftCount, int maxShiftCount) {
            CompanyName = companyName;
            IsInternational = isInternational;
            MinShiftCount = minShiftCount;
            MaxShiftCount = maxShiftCount;
        }
    }
}

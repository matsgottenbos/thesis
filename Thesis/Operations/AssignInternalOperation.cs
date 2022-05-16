using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class AssignInternalOperation : AbstractAssignOperation {
        public AssignInternalOperation(int tripIndex, InternalDriver newInternalDriver, SaInfo info) : base(tripIndex, newInternalDriver, info) {

        }

        public static AbstractAssignOperation CreateRandom(SaInfo info) {
            int tripIndex = info.Instance.Rand.Next(info.Instance.Trips.Length);
            Driver oldDriver = info.Assignment[tripIndex];

            // Select random internal driver that is not the current driver
            InternalDriver newInternalDriver;
            do {
                int newInternalDriverIndex = info.Instance.Rand.Next(info.Instance.InternalDrivers.Length);
                newInternalDriver = info.Instance.InternalDrivers[newInternalDriverIndex];
            } while (newInternalDriver == oldDriver);

            return new AssignInternalOperation(tripIndex, newInternalDriver, info);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public class AssignInternalOperation : AbstractAssignOperation {
        public AssignInternalOperation(int activityIndex, InternalDriver newInternalDriver, SaInfo info) : base(activityIndex, newInternalDriver, info) { }

        public static AbstractAssignOperation CreateRandom(SaInfo info, XorShiftRandom rand) {
            int activityIndex = rand.Next(info.Instance.Activities.Length);
            Driver oldDriver = info.Assignment[activityIndex];

            // Select random internal driver that is not the current driver
            InternalDriver newInternalDriver;
            do {
                int newInternalDriverIndex = rand.Next(info.Instance.InternalDrivers.Length);
                newInternalDriver = info.Instance.InternalDrivers[newInternalDriverIndex];
            } while (newInternalDriver == oldDriver);

            return new AssignInternalOperation(activityIndex, newInternalDriver, info);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class AssignExternalOperation : AbstractAssignOperation {
        public AssignExternalOperation(int activityIndex, ExternalDriver newExternalDriver, SaInfo info) : base(activityIndex, newExternalDriver, info) { }

        public static AbstractAssignOperation CreateRandom(SaInfo info, XorShiftRandom rand) {
            int activityIndex = rand.Next(info.Instance.Activities.Length);
            Driver oldDriver = info.Assignment[activityIndex];

            // Select random existing driver that is not the same as the current driver
            ExternalDriver newExternalDriver;
            do {
                // Select random external driver type
                int newExternalDriverTypeIndex = rand.Next(info.Instance.ExternalDriversByType.Length);
                ExternalDriver[] externalDriversOfCurrentType = info.Instance.ExternalDriversByType[newExternalDriverTypeIndex];

                // Select random external driver of this type
                int newExternalDriverIndexInType = rand.Next(externalDriversOfCurrentType.Length);
                newExternalDriver = externalDriversOfCurrentType[newExternalDriverIndexInType];
            } while (newExternalDriver == oldDriver);

            return new AssignExternalOperation(activityIndex, newExternalDriver, info);
        }
    }
}

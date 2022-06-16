using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class AssignExternalOperation : AbstractAssignOperation {
        public AssignExternalOperation(int tripIndex, ExternalDriver newExternalDriver, SaInfo info) : base(tripIndex, newExternalDriver, info) { }

        public static AbstractAssignOperation CreateRandom(SaInfo info) {
            int tripIndex = info.Instance.Rand.Next(info.Instance.Trips.Length);
            Driver oldDriver = info.Assignment[tripIndex];

            // Select random existing driver that is not the same as the current driver
            ExternalDriver newExternalDriver;
            do {
                // Select random external driver type
                int newExternalDriverTypeIndex = info.Instance.Rand.Next(info.Instance.ExternalDriversByType.Length);
                ExternalDriver[] externalDriversOfCurrentType = info.Instance.ExternalDriversByType[newExternalDriverTypeIndex];

                // Select random external driver of this type
                int newExternalDriverIndexInType = info.Instance.Rand.Next(externalDriversOfCurrentType.Length);
                newExternalDriver = externalDriversOfCurrentType[newExternalDriverIndexInType];
            } while (newExternalDriver == oldDriver);

            return new AssignExternalOperation(tripIndex, newExternalDriver, info);
        }
    }
}

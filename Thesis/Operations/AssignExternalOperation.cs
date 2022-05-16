using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class AssignExternalOperation : AbstractAssignOperation {
        ExternalDriver newExternalDriver;

        public AssignExternalOperation(int tripIndex, ExternalDriver newExternalDriver, SaInfo info) : base(tripIndex, newExternalDriver, info) {
            this.newExternalDriver = newExternalDriver;
        }

        public static AbstractAssignOperation CreateRandom(SaInfo info) {
            int tripIndex = info.Instance.FastRand.NextInt(info.Instance.Trips.Length);
            Driver oldDriver = info.Assignment[tripIndex];

            // Select random existing driver that is not the same as the current driver
            ExternalDriver newExternalDriver;
            do {
                // Select random external driver type
                int newExternalDriverTypeIndex = info.Instance.FastRand.NextInt(info.Instance.ExternalDriversByType.Length);
                ExternalDriver[] externalDriversOfCurrentType = info.Instance.ExternalDriversByType[newExternalDriverTypeIndex];

                // Select random external driver of this type; equal chance to select each existing or a new driver
                int currentCountOfType = info.ExternalDriverCountsByType[newExternalDriverTypeIndex];
                int maxNewIndexInTypeExclusive = Math.Min(currentCountOfType + 1, externalDriversOfCurrentType.Length);
                int newExternalDriverIndexInType = info.Instance.FastRand.NextInt(maxNewIndexInTypeExclusive);
                newExternalDriver = externalDriversOfCurrentType[newExternalDriverIndexInType];
            } while (newExternalDriver == oldDriver);

            return new AssignExternalOperation(tripIndex, newExternalDriver, info);
        }

        public override void Execute() {
            base.Execute();

            // If this is a new driver of this type, update the corresponding count
            info.ExternalDriverCountsByType[newExternalDriver.ExternalDriverTypeIndex] = Math.Max(info.ExternalDriverCountsByType[newExternalDriver.ExternalDriverTypeIndex], newExternalDriver.IndexInType + 1);
        }
    }
}

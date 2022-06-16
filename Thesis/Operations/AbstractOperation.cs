﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    abstract class AbstractOperation {
        protected readonly SaInfo info;
        protected SaTotalInfo totalInfoDiff;

        public AbstractOperation(SaInfo info) {
            this.info = info;
            totalInfoDiff = new SaTotalInfo();
        }

        public abstract SaTotalInfo GetCostDiff();

        public virtual void Execute() {
            info.TotalInfo += totalInfoDiff;
        }

        protected void UpdateDriverInfo(Driver driver, SaDriverInfo driverInfoDiff) {
            info.DriverInfos[driver.AllDriversIndex] += driverInfoDiff;
        }

        protected void UpdateExternalDriverTypeInfo(Driver driver, SaExternalDriverTypeInfo externalDriverTypeInfo) {
            if (externalDriverTypeInfo != null) info.ExternalDriverTypeInfos[((ExternalDriver)driver).ExternalDriverTypeIndex] += externalDriverTypeInfo;
        }
    }
}

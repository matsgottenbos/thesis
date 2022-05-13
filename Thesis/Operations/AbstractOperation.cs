using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    abstract class AbstractOperation {
        protected readonly SaInfo info;
        protected DriverInfo totalInfoDiff;

        public AbstractOperation(SaInfo info) {
            this.info = info;
        }

        public abstract DriverInfo GetCostDiff();

        public virtual void Execute() {
            info.TotalInfo += totalInfoDiff;
        }
    }
}

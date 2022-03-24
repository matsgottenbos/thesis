using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    abstract class AbstractOperation {
        protected readonly SaInfo info;

        public AbstractOperation(SaInfo info) {
            this.info = info;
        }

        public abstract (double, double, double) GetCostDiff();
        public abstract void Execute();
    }
}

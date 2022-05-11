using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    abstract class AbstractOperation {
        protected readonly SaInfo info;
        protected double costDiff, costWithoutPenaltyDiff, penaltyDiff, satisfactionDiff;

        public AbstractOperation(SaInfo info) {
            this.info = info;
        }

        public abstract (double, double) GetCostDiff();

        public virtual void Execute() {
            info.Cost += costDiff;
            info.CostWithoutPenalty += costWithoutPenaltyDiff;
            info.Penalty += penaltyDiff;
            info.Satisfaction += satisfactionDiff;
        }
    }
}

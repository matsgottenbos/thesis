using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class SaInfo {
        public readonly Instance Instance;
        public readonly Random Rand;
        public readonly XorShiftRandom FastRand;
        public Driver[] Assignment;
        public bool[] IsHotelStayAfterTrip;
        public double Cost, CostWithoutPenalty, Penalty;
        public int[] DriversWorkedTime, ExternalDriverCountsByType;
        public int IterationNum, CycleNum, PrecedenceViolationCount, ShiftLengthViolationCount, RestTimeViolationCount, ContractTimeViolationCount, InvalidHotelCount;
        public float Temperature;

        public SaInfo(Instance instance, Random rand, XorShiftRandom fastRand) {
            Instance = instance;
            Rand = rand;
            FastRand = fastRand;
        }
    }
}

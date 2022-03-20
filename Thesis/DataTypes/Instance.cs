using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class Instance {
        public readonly int[,] TrainTravelTimes, CarTravelTimes;
        public readonly Trip[] Trips;
        public readonly bool[,] TripSuccession;
        public readonly InternalDriver[] InternalDrivers;
        public readonly ExternalDriver[][] ExternalDriversByType;
        public readonly Driver[] AllDrivers;

        public Instance(int[,] trainTravelTimes, int[,] carTravelTimes, Trip[] trips, bool[,] tripSuccession, InternalDriver[] internalDrivers, ExternalDriver[][] externalDriversByType, Driver[] allDrivers) {
            TrainTravelTimes = trainTravelTimes;
            CarTravelTimes = carTravelTimes;
            Trips = trips;
            TripSuccession = tripSuccession;
            InternalDrivers = internalDrivers;
            ExternalDriversByType = externalDriversByType;
            AllDrivers = allDrivers;
        }


        /* Helper methods */

        public int CarTravelTime(Trip trip1, Trip trip2) {
            return CarTravelTimes[trip1.LastStation, trip2.FirstStation];
        }

        int WaitingTime(Trip trip1, Trip trip2) {
            return trip2.StartTime - trip1.EndTime - CarTravelTime(trip1, trip2);
        }

        /** Check if two trips belong to the same shift or not, based on whether their waiting time is within the threshold */
        public bool AreSameShift(Trip trip1, Trip trip2) {
            return WaitingTime(trip1, trip2) <= Config.ShiftWaitingTimeThreshold;
        }
    }
}

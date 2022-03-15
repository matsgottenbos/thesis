using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class AssignmentHelper {
        /** Returns previous trip of driver; returns as first of tuple when in same shift, or second of tuple when in previous shift */
        public static (Trip, Trip) GetPrevTrip(Trip trip, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance) {
            int startTimeThreshold = trip.StartTime - Config.BetweenShiftsMaxStartTimeDiff;
            for (int searchTripIndex = trip.Index - 1; searchTripIndex >= 0; searchTripIndex--) {
                Trip searchTrip = instance.Trips[searchTripIndex];
                if (searchTrip.StartTime < startTimeThreshold) return (null, null);

                // Check if this is the previous trip for this driver
                if (assignment[searchTripIndex] == driver && searchTrip != tripToIgnore) {
                    if (instance.AreSameShift(searchTrip, trip)) return (searchTrip, null); // Found trip is in current shift
                    return (null, searchTrip); // Found trip is in previous shift
                }
            }
            return (null, null);
        }

        /** Returns next trip of driver; returns as first of tuple when in same shift, or second of tuple when in next shift */
        public static (Trip, Trip) GetNextTrip(Trip trip, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance) {
            int startTimeThreshold = trip.StartTime + Config.BetweenShiftsMaxStartTimeDiff;
            for (int searchTripIndex = trip.Index + 1; searchTripIndex < instance.Trips.Length; searchTripIndex++) {
                Trip searchTrip = instance.Trips[searchTripIndex];
                if (searchTrip.StartTime > startTimeThreshold) return (null, null);

                // Check if this is the next trip for this driver
                if (assignment[searchTripIndex] == driver && searchTrip != tripToIgnore) {
                    if (instance.AreSameShift(trip, searchTrip)) return (searchTrip, null); // Found trip is in current shift
                    return (null, searchTrip); // Found trip is in next shift
                }
            }
            return (null, null);
        }

        /** Returns driver's first trip of shift, and last trip of previous shift */
        public static (Trip, Trip) GetFirstTripInternalAndPrevShiftTrip(Trip trip, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance) {
            Trip firstTripInternal = trip;
            int startTimeThreshold = firstTripInternal.StartTime - Config.ShiftMaxStartTimeDiff;
            for (int searchTripIndex = trip.Index - 1; searchTripIndex >= 0; searchTripIndex--) {
                Trip searchTrip = instance.Trips[searchTripIndex];
                if (searchTrip.StartTime < startTimeThreshold) {
                    return (firstTripInternal, null);
                }

                if (assignment[searchTripIndex] == driver && searchTrip != tripToIgnore) {
                    if (instance.AreSameShift(searchTrip, firstTripInternal)) {
                        firstTripInternal = searchTrip;
                        startTimeThreshold = firstTripInternal.StartTime - Config.ShiftMaxStartTimeDiff;
                    } else {
                        return (firstTripInternal, searchTrip);
                    }
                }
            }
            return (firstTripInternal, null);
        }

        /** Returns driver's last trip of shift, and first trip of next shift */
        public static (Trip, Trip) GetLastTripInternalAndNextShiftTrip(Trip trip, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance) {
            Trip lastTripInternal = trip;
            int startTimeThreshold = lastTripInternal.StartTime + Config.ShiftMaxStartTimeDiff;
            for (int searchTripIndex = trip.Index + 1; searchTripIndex < instance.Trips.Length; searchTripIndex++) {
                Trip searchTrip = instance.Trips[searchTripIndex];
                if (searchTrip.StartTime > startTimeThreshold) {
                    return (lastTripInternal, null);
                }

                if (assignment[searchTripIndex] == driver && searchTrip != tripToIgnore) {
                    if (instance.AreSameShift(lastTripInternal, searchTrip)) {
                        lastTripInternal = searchTrip;
                        startTimeThreshold = lastTripInternal.StartTime + Config.ShiftMaxStartTimeDiff;
                    } else {
                        return (lastTripInternal, searchTrip);
                    }
                }
            }
            return (lastTripInternal, null);
        }
    }
}

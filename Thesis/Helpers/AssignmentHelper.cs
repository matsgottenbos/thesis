using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class AssignmentHelper {
        public static int GetAssignedPathTripIndexBefore(Trip assignedTrip, List<Trip> driverPath) {
            int pathTripIndex;
            for (pathTripIndex = 0; pathTripIndex < driverPath.Count; pathTripIndex++) {
                if (driverPath[pathTripIndex].Index >= assignedTrip.Index) break;
            }
            return pathTripIndex - 1;
        }

        /** Returns driver's first trip of shift, and last trip of previous shift */
        public static (Trip, Trip) GetFirstTripInternalAndPrevShiftTrip(Trip trip, int pathTripIndexBefore, List<Trip> driverPath, SaInfo info) {
            Trip firstTripInternal = trip;
            for (int pathTripIndex = pathTripIndexBefore; pathTripIndex >= 0; pathTripIndex--) {
                Trip searchTrip = driverPath[pathTripIndex];
                if (info.Instance.AreSameShift(searchTrip, firstTripInternal)) {
                    firstTripInternal = searchTrip;
                } else {
                    return (firstTripInternal, searchTrip);
                }
            }
            return (firstTripInternal, null);
        }

        public static (Trip, Trip) GetFirstTripInternalAndPrevShiftTrip(Trip trip, List<Trip> driverPath, SaInfo info) {
            int pathTripIndexBefore = info.DriverPathIndices[trip.Index] - 1;
            return GetFirstTripInternalAndPrevShiftTrip(trip, pathTripIndexBefore, driverPath, info);
        }

        /** Returns driver's last trip of shift, and first trip of next shift */
        public static (Trip, Trip) GetLastTripInternalAndNextShiftTrip(Trip trip, int pathTripIndexAfter, List<Trip> driverPath, SaInfo info) {
            Trip lastTripInternal = trip;
            for (int pathTripIndex = pathTripIndexAfter; pathTripIndex < driverPath.Count; pathTripIndex++) {
                Trip searchTrip = driverPath[pathTripIndex];
                if (info.Instance.AreSameShift(lastTripInternal, searchTrip)) {
                    lastTripInternal = searchTrip;
                } else {
                    return (lastTripInternal, searchTrip);
                }
            }
            return (lastTripInternal, null);
        }

        public static (Trip, Trip) GetLastTripInternalAndNextShiftTrip(Trip trip, List<Trip> driverPath, SaInfo info) {
            int pathTripIndexAfter = info.DriverPathIndices[trip.Index] + 1;
            return GetLastTripInternalAndNextShiftTrip(trip, pathTripIndexAfter, driverPath, info);
        }
    }
}

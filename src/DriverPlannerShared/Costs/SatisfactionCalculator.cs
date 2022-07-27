/*
 * Calculates satisfaction score for an assignment
*/

namespace DriverPlannerShared {
    public static class SatisfactionScoreCalculator {
        /* Satisfaction score */

        public static double GetSatisfactionScore(SaInfo info) {
            // Average satisfaction
            double averageDriverSatisfaction = info.TotalInfo.Stats.DriverSatisfaction / info.Instance.RequiredInternalDriverCount;

            // Minimum driver satisfaction
            double minDriverSatisfaction = double.MaxValue;
            for (int driverIndex = 0; driverIndex < info.Instance.InternalDrivers.Length; driverIndex++) {
                // Skip optional drivers
                if (info.Instance.InternalDrivers[driverIndex].IsOptional) continue;

                SaDriverInfo driverInfo = info.DriverInfos[driverIndex];
                if (driverInfo.Stats.DriverSatisfaction < minDriverSatisfaction) minDriverSatisfaction = driverInfo.Stats.DriverSatisfaction;
            }

            // Total satisfaction
            double totalSatisfactionDiff = (averageDriverSatisfaction + minDriverSatisfaction) / 2;
            return totalSatisfactionDiff;
        }

        public static double GetSatisfactionScoreDiff(SaTotalInfo totalInfoDiff, Driver driver1, SaDriverInfo driver1InfoDiff, Driver driver2, SaDriverInfo driver2InfoDiff, SaInfo info) {
            // Average satisfaction
            double averageDriverSatisfactionDiff = totalInfoDiff.Stats.DriverSatisfaction / info.Instance.RequiredInternalDriverCount;

            // Minimum driver satisfaction
            double oldMinDriverSatisfaction = double.MaxValue;
            double newMinDriverSatisfaction = double.MaxValue;
            for (int driverIndex = 0; driverIndex < info.Instance.InternalDrivers.Length; driverIndex++) {
                // Skip optional drivers
                if (info.Instance.InternalDrivers[driverIndex].IsOptional) continue;

                SaDriverInfo oldDriverInfo = info.DriverInfos[driverIndex];

                // Get new minimum
                double newDriverSatisfaction = oldDriverInfo.Stats.DriverSatisfaction;
                if (driverIndex == driver1.AllDriversIndex) newDriverSatisfaction += driver1InfoDiff.Stats.DriverSatisfaction;
                else if (driver2 != null && driverIndex == driver2.AllDriversIndex) newDriverSatisfaction += driver2InfoDiff.Stats.DriverSatisfaction;

                // Update minimums
                if (oldDriverInfo.Stats.DriverSatisfaction < oldMinDriverSatisfaction) oldMinDriverSatisfaction = oldDriverInfo.Stats.DriverSatisfaction;
                if (newDriverSatisfaction < newMinDriverSatisfaction) newMinDriverSatisfaction = newDriverSatisfaction;
            }
            double minDriverSatisfactionDiff = newMinDriverSatisfaction - oldMinDriverSatisfaction;

            // Total satisfaction
            double totalSatisfactionDiff = (averageDriverSatisfactionDiff + minDriverSatisfactionDiff) / 2;
            return totalSatisfactionDiff;
        }


        /* Driver satisfaction helpers */

        public static int GetDuplicateRouteCount(SaDriverInfo driverInfo) {
            int duplicateRouteCount = 0;
            for (int sharedRouteIndex = 0; sharedRouteIndex < driverInfo.SharedRouteCounts.Length; sharedRouteIndex++) {
                int count = driverInfo.SharedRouteCounts[sharedRouteIndex];
                if (count > 1) {
                    duplicateRouteCount += count - 1;
                }
            }
            return duplicateRouteCount;
        }

        public static float GetConsecutiveFreeDaysScore(SaDriverInfo driverInfo) {
            // Criterion score is 100% when there are two consecutive free days, or otherwise 25% per single free day
            if (driverInfo.DoubleFreeDayCount >= 1) return 1;
            return driverInfo.SingleFreeDayCount * 0.25f;
        }
    }
}

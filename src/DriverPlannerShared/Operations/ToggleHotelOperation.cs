namespace DriverPlannerShared {
    public class ToggleHotelOperation : AbstractOperation {
        readonly Activity activity;
        readonly bool isAddition;
        readonly Driver driver;
        readonly SaDriverInfo driverInfo;
        SaDriverInfo driverInfoDiff;

        public ToggleHotelOperation(int activityIndex, bool isAddition, SaInfo info) : base(info) {
            activity = info.Instance.Activities[activityIndex];
            this.isAddition = isAddition;
            driver = info.Assignment[activityIndex];
            driverInfo = info.DriverInfos[driver.AllDriversIndex];
        }

        public override SaTotalInfo GetCostDiff() {
#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                string templateStr = isAddition ? "Add hotel stay to activity {0} with driver {1}" : "Remove hotel stay from activity {0} with driver {1}";
                SaDebugger.GetCurrentOperation().Description = string.Format(templateStr, activity.Index, driver.GetId());
            }
#endif

            // Get cost diffs
            if (isAddition) {
                driverInfoDiff = CostDiffCalculator.GetAddHotelDriverCostDiff(activity, driver, driverInfo, info);
            } else {
                driverInfoDiff = CostDiffCalculator.GetRemoveHotelDriverCostDiff(activity, driver, driverInfo, info);
            }

            // Store cost diffs in total diff object
            totalInfoDiff.AddDriverInfo(driverInfoDiff);

            // Determine satisfaction score diff
            totalInfoDiff.Stats.SatisfactionScore = SatisfactionScoreCalculator.GetSatisfactionScoreDiff(totalInfoDiff, driver, driverInfoDiff, null, null, info);

            return totalInfoDiff;
        }

        public override void Execute() {
            info.IsHotelStayAfterActivity[activity.Index] = isAddition;
            UpdateDriverInfo(driver, driverInfoDiff);
        }

        public static ToggleHotelOperation CreateRandom(SaInfo info, XorShiftRandom rand) {
            int activityIndex = rand.Next(info.Instance.Activities.Length);

            bool isAddition = !info.IsHotelStayAfterActivity[activityIndex];

            return new ToggleHotelOperation(activityIndex, isAddition, info);
        }
    }
}

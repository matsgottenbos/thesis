using System;

namespace DriverPlannerShared {
    public class SalaryRateBlock {
        public readonly int StartTime;
        public readonly float SalaryRate;
        public readonly float ContinuingRate; // A shift starting in this rate block will use the continuing rate as a minimum for the entire shift

        public SalaryRateBlock(int startTime, float salaryRate, float continuingRate) {
            StartTime = startTime;
            SalaryRate = salaryRate;
            ContinuingRate = continuingRate;
        }

        public static SalaryRateBlock CreateByHours(float startTimeHours, float hourlySalaryRate, bool isContinuingRate = false) {
            return new SalaryRateBlock(
                (int)Math.Round(startTimeHours * DevConfig.HourLength),
                hourlySalaryRate / DevConfig.HourLength,
                isContinuingRate ? hourlySalaryRate / DevConfig.HourLength : 0f
            );
        }
    }
}

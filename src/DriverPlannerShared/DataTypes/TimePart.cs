namespace DriverPlannerShared {
    public class TimePart {
        public readonly int StartTime;
        public readonly bool IsSelected;

        public TimePart(int startTime, bool isWeekend) {
            StartTime = startTime;
            IsSelected = isWeekend;
        }
    }
}

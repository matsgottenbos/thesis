using System;

namespace Thesis {
    class DriverInfo {
        public int WorkedTime, ShiftCount, HotelCount, NightShiftCount, WeekendShiftCount;

        public static DriverInfo operator -(DriverInfo a) {
            return new DriverInfo() {
                WorkedTime = -a.WorkedTime,
                ShiftCount = -a.ShiftCount,
                HotelCount = -a.HotelCount,
                NightShiftCount = -a.NightShiftCount,
                WeekendShiftCount = -a.WeekendShiftCount,
            };
        }
        public static DriverInfo operator +(DriverInfo a, DriverInfo b) {
            return new DriverInfo() {
                WorkedTime = a.WorkedTime + b.WorkedTime,
                ShiftCount = a.ShiftCount + b.ShiftCount,
                HotelCount = a.HotelCount + b.HotelCount,
                NightShiftCount = a.NightShiftCount + b.NightShiftCount,
                WeekendShiftCount = a.WeekendShiftCount + b.WeekendShiftCount,
            };
        }
        public static DriverInfo operator -(DriverInfo a, DriverInfo b) => a + -b;

        public static bool AreEqual(DriverInfo a, DriverInfo b) {
            return (
                a.WorkedTime == b.WorkedTime &&
                a.ShiftCount == b.ShiftCount &&
                a.HotelCount == b.HotelCount &&
                a.NightShiftCount == b.NightShiftCount &&
                a.WeekendShiftCount == b.WeekendShiftCount
            );
        }

        public void DebugLog(bool isDiff, bool shouldLogZeros = true) {
            ParseHelper.LogDebugValue(WorkedTime, "Worked time", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(ShiftCount, "Shift count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(HotelCount, "Hotel count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(NightShiftCount, "Night shift count", isDiff, shouldLogZeros);
            ParseHelper.LogDebugValue(WeekendShiftCount, "Weekend shift count", isDiff, shouldLogZeros);
        }
    }
}

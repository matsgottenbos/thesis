namespace DriverPlannerShared {
    public class ExternalDriverType {
        public readonly string CompanyName;
        public readonly bool IsInternational, IsHotelAllowed;
        public readonly int MinShiftCount, MaxShiftCount;

        public ExternalDriverType(string companyName, bool isInternational, bool isHotelAllowed, int minShiftCount, int maxShiftCount) {
            CompanyName = companyName;
            IsInternational = isInternational;
            IsHotelAllowed = isHotelAllowed;
            MinShiftCount = minShiftCount;
            MaxShiftCount = maxShiftCount;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public abstract class Driver {
        public readonly int AllDriversIndex;
        public readonly bool IsInternational;
        public readonly bool IsHotelAllowed;
        public readonly int[] homeTravelTimes, homeTravelDistances;
        readonly bool[] activityQualifications;
        protected Instance instance;
        public readonly SalarySettings SalarySettings;

        public Driver(int allDriversIndex, bool isInternational, bool isHotelAllowed, int[] homeTravelTimes, int[] homeTravelDistances, bool[] activityQualifications, SalarySettings salarySettings) {
            AllDriversIndex = allDriversIndex;
            IsInternational = isInternational;
            IsHotelAllowed = isHotelAllowed;
            this.homeTravelTimes = homeTravelTimes;
            this.homeTravelDistances = homeTravelDistances;
            this.activityQualifications = activityQualifications;
            SalarySettings = salarySettings;
        }

        public void SetInstance(Instance instance) {
            this.instance = instance;
        }

        public abstract string GetId();

        public int HomeTravelTimeToStart(Activity activity) {
            return homeTravelTimes[activity.StartStationAddressIndex];
        }

        public int HomeTravelDistanceToStart(Activity activity) {
            return homeTravelDistances[activity.StartStationAddressIndex];
        }

        public abstract bool IsAvailableDuringRange(int rangeStartTime, int rangeEndTime);

        public bool IsQualifiedForActivity(Activity activity) {
            return activityQualifications[activity.Index];
        }

        public abstract float GetPaidTravelCost(int travelTime, int travelDistance);


        public abstract double GetSatisfaction(SaDriverInfo driverInfo);
    }
}

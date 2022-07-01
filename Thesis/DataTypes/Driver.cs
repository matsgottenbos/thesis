using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    abstract class Driver {
        public readonly int AllDriversIndex;
        public readonly bool IsInternational;
        public readonly bool IsHotelAllowed;
        readonly int[] homeTravelTimes, homeTravelDistances;
        readonly bool[,] trackProficiencies;
        protected Instance instance;
        public readonly SalarySettings SalarySettings;

        public Driver(int allDriversIndex, bool isInternational, bool isHotelAllowed, int[] homeTravelTimes, int[] homeTravelDistances, bool[,] trackProficiencies, SalarySettings salarySettings) {
            AllDriversIndex = allDriversIndex;
            IsInternational = isInternational;
            IsHotelAllowed = isHotelAllowed;
            this.homeTravelTimes = homeTravelTimes;
            this.homeTravelDistances = homeTravelDistances;
            this.trackProficiencies = trackProficiencies;
            SalarySettings = salarySettings;
        }

        public void SetInstance(Instance instance) {
            this.instance = instance;
        }

        public abstract string GetId();

        public float DrivingCost(Activity shiftFirstActivity, Activity shiftLastActivity) {
            return instance.ShiftInfo(shiftFirstActivity, shiftLastActivity).GetDrivingCost(SalarySettings.DriverTypeIndex);
        }

        public int HomeTravelTimeToStart(Activity activity) {
            return homeTravelTimes[activity.StartStationAddressIndex];
        }

        public int HomeTravelDistanceToStart(Activity activity) {
            return homeTravelDistances[activity.StartStationAddressIndex];
        }

        public bool IsQualifiedForActivity(Activity activity) {
            return trackProficiencies[activity.StartStationAddressIndex, activity.EndStationAddressIndex];
        }

        public abstract float GetPaidTravelCost(int travelTime, int travelDistance);


        public abstract double GetSatisfaction(SaDriverInfo driverInfo, SaInfo info);
    }
}

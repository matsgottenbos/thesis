using MathNet.Numerics.Distributions;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class Instance {
        readonly int timeframeLength;
        public readonly int UniqueSharedRouteCount;
        readonly int[,] plannedCarTravelTimes, expectedCarTravelTimes, carTravelDistances;
        public readonly Activity[] Activities;
        public readonly string[] StationNames, StationCountries;
        readonly ShiftInfo[,] shiftInfos;
        readonly float[,] activitySuccessionRobustness;
        readonly bool[,] activitySuccession, activitiesAreSameShift;
        public readonly InternalDriver[] InternalDrivers;
        public readonly ExternalDriverType[] ExternalDriverTypes;
        public readonly ExternalDriver[][] ExternalDriversByType;
        public readonly Driver[] AllDrivers, DataAssignment;

        public Instance(XorShiftRandom rand, RawActivity[] rawActivities) {
            XSSFWorkbook stationAddressesBook = ExcelHelper.ReadExcelFile(Path.Combine(AppConfig.InputFolder, "stationAddresses.xlsx"));
            XSSFWorkbook settingsBook = ExcelHelper.ReadExcelFile(Path.Combine(AppConfig.InputFolder, "settings.xlsx"));

            (StationNames, plannedCarTravelTimes, expectedCarTravelTimes, carTravelDistances) = DataProcessor.GetStationNamesAndExpectedCarTravelInfo();
            StationCountries = DataProcessor.GetStationCountries(stationAddressesBook, StationNames);
            (Activities, activitySuccession, activitySuccessionRobustness, activitiesAreSameShift, timeframeLength, UniqueSharedRouteCount) = DataProcessor.ProcessRawActivities(stationAddressesBook, rawActivities, StationNames, expectedCarTravelTimes);
            shiftInfos = DataProcessor.GetShiftInfos(Activities, timeframeLength);
            InternalDrivers = DataProcessor.CreateInternalDrivers(settingsBook, StationCountries);
            Dictionary<(string, bool), ExternalDriver[]> externalDriversByTypeDict;
            (ExternalDriverTypes, ExternalDriversByType, externalDriversByTypeDict) = DataProcessor.CreateExternalDrivers(settingsBook, StationCountries, InternalDrivers.Length);
            DataAssignment = DataProcessor.GetDataAssignment(settingsBook, Activities, InternalDrivers, externalDriversByTypeDict);

            // Create all drivers array
            List<Driver> allDriversList = new List<Driver>();
            allDriversList.AddRange(InternalDrivers);
            for (int i = 0; i < ExternalDriversByType.Length; i++) {
                allDriversList.AddRange(ExternalDriversByType[i]);
            }
            AllDrivers = allDriversList.ToArray();

            // Pass instance object to drivers
            for (int driverIndex = 0; driverIndex < AllDrivers.Length; driverIndex++) {
                AllDrivers[driverIndex].SetInstance(this);
            }
        }


        /* Helper methods */

        public ShiftInfo ShiftInfo(Activity activity1, Activity activity2) {
            return shiftInfos[activity1.Index, activity2.Index];
        }

        public bool IsValidSuccession(Activity activity1, Activity activity2) {
            return activitySuccession[activity1.Index, activity2.Index];
        }

        public float ActivitySuccessionRobustness(Activity activity1, Activity activity2) {
            return activitySuccessionRobustness[activity1.Index, activity2.Index];
        }

        public int PlannedCarTravelTime(Activity activity1, Activity activity2) {
            return plannedCarTravelTimes[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex];
        }

        public int ExpectedCarTravelTime(Activity activity1, Activity activity2) {
            return expectedCarTravelTimes[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex];
        }

        public int CarTravelDistance(Activity activity1, Activity activity2) {
            return carTravelDistances[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex];
        }

        public int PlannedTravelTimeViaHotel(Activity activity1, Activity activity2) {
            return plannedCarTravelTimes[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex] + RulesConfig.HotelExtraTravelTime;
        }

        public int ExpectedTravelTimeViaHotel(Activity activity1, Activity activity2) {
            return expectedCarTravelTimes[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex] + RulesConfig.HotelExtraTravelTime;
        }

        public int PlannedHalfTravelTimeViaHotel(Activity activity1, Activity activity2) {
            return (plannedCarTravelTimes[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex] + RulesConfig.HotelExtraTravelTime) / 2;
        }

        public int ExpectedHalfTravelTimeViaHotel(Activity activity1, Activity activity2) {
            return (expectedCarTravelTimes[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex] + RulesConfig.HotelExtraTravelTime) / 2;
        }

        public int HalfTravelDistanceViaHotel(Activity activity1, Activity activity2) {
            return (carTravelDistances[activity1.EndStationAddressIndex, activity2.StartStationAddressIndex] + RulesConfig.HotelExtraTravelDistance) / 2;
        }

        public int RestTimeWithTravelTime(Activity activity1, Activity activity2, int travelTime) {
            return activity2.StartTime - activity1.EndTime - travelTime;
        }

        public int RestTimeViaHotel(Activity activity1, Activity activity2) {
            return RestTimeWithTravelTime(activity1, activity2, ExpectedTravelTimeViaHotel(activity1, activity2));
        }

        /** Check if two activites belong to the same shift or not, based on whether their waiting time is within the threshold */
        public bool AreSameShift(Activity activity1, Activity activity2) {
            return activitiesAreSameShift[activity1.Index, activity2.Index];
        }
    }

    class ComputedSalaryRateBlock {
        public int RateStartTime, RateEndTime, SalaryStartTime, SalaryEndTime, SalaryDuration;
        public float SalaryRate, MainShiftCostInRate;
        public bool UsesContinuingRate;

        public ComputedSalaryRateBlock(int rateStartTime, int rateEndTime, int salaryStartTime, int salaryEndTime, int salaryDuration, float salaryRate, bool usesContinuingRate, float mainSshiftCostInRate) {
            RateStartTime = rateStartTime;
            RateEndTime = rateEndTime;
            SalaryStartTime = salaryStartTime;
            SalaryEndTime = salaryEndTime;
            SalaryDuration = salaryDuration;
            SalaryRate = salaryRate;
            UsesContinuingRate = usesContinuingRate;
            MainShiftCostInRate = mainSshiftCostInRate;
        }
    }
}

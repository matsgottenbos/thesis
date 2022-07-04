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
        public readonly int UniqueSharedRouteCount, RequiredInternalDriverCount;
        readonly int[,] plannedCarTravelTimes, expectedCarTravelTimes, carTravelDistances;
        public readonly Activity[] Activities;
        public readonly string[] StationNames;
        readonly SalarySettings[] SalarySettingsByDriverType;
        readonly MainShiftInfo[,] mainShiftInfos;
        readonly float[,] activitySuccessionRobustness;
        readonly bool[,] activitySuccession, activitiesAreSameShift;
        public readonly InternalDriver[] InternalDrivers;
        public readonly ExternalDriverType[] ExternalDriverTypes;
        public readonly ExternalDriver[][] ExternalDriversByType;
        public readonly Driver[] AllDrivers, DataAssignment;

        public Instance(RawActivity[] rawActivities) {
            XSSFWorkbook stationAddressesBook = ExcelHelper.ReadExcelFile(Path.Combine(AppConfig.InputFolder, "stationAddresses.xlsx"));
            XSSFWorkbook settingsBook = ExcelHelper.ReadExcelFile(Path.Combine(AppConfig.InputFolder, "settings.xlsx"));

            (StationNames, plannedCarTravelTimes, expectedCarTravelTimes, carTravelDistances) = DataMiscProcessor.GetStationNamesAndExpectedCarTravelInfo();
            (string[] borderStationNames, string[] borderRegionStationNames) = DataMiscProcessor.GetBorderAndBorderRegionStationNames(stationAddressesBook);
            (Activities, activitySuccession, activitySuccessionRobustness, activitiesAreSameShift, timeframeLength, UniqueSharedRouteCount) = DataActivityProcessor.ProcessRawActivities(stationAddressesBook, rawActivities, StationNames, borderStationNames, borderRegionStationNames, expectedCarTravelTimes);
            SalarySettingsByDriverType = DataSalaryProcessor.GetSalarySettingsByDriverType(timeframeLength);
            mainShiftInfos = DataShiftProcessor.GetMainShiftInfos(SalarySettingsByDriverType, timeframeLength);
            (InternalDrivers, RequiredInternalDriverCount) = DataDriverProcessor.CreateInternalDrivers(settingsBook, Activities);
            Dictionary<(string, bool), ExternalDriver[]> externalDriversByDataTypeDict;
            (ExternalDriverTypes, ExternalDriversByType, externalDriversByDataTypeDict) = DataDriverProcessor.CreateExternalDrivers(settingsBook, Activities, InternalDrivers.Length);
            DataAssignment = DataMiscProcessor.GetDataAssignment(settingsBook, Activities, InternalDrivers, externalDriversByDataTypeDict);

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

        public MainShiftInfo MainShiftInfo(int mainShiftStartTime, int realMainShiftEndTime) {
            int roundedStartTime = (int)Math.Round((float)mainShiftStartTime / MiscConfig.RoundedTimeStepSize);
            int roundedEndTime = (int)Math.Round((float)realMainShiftEndTime / MiscConfig.RoundedTimeStepSize);
            return mainShiftInfos[roundedStartTime, roundedEndTime];
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
        public float SalaryRate, CostInRate;
        public bool UsesContinuingRate;

        public ComputedSalaryRateBlock(int rateStartTime, int rateEndTime, int salaryStartTime, int salaryEndTime, int salaryDuration, float salaryRate, bool usesContinuingRate, float mainSshiftCostInRate) {
            RateStartTime = rateStartTime;
            RateEndTime = rateEndTime;
            SalaryStartTime = salaryStartTime;
            SalaryEndTime = salaryEndTime;
            SalaryDuration = salaryDuration;
            SalaryRate = salaryRate;
            UsesContinuingRate = usesContinuingRate;
            CostInRate = mainSshiftCostInRate;
        }
    }
}

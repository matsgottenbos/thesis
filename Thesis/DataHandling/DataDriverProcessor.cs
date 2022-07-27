using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class DataDriverProcessor {
        public static (InternalDriver[], int) CreateInternalDrivers(XSSFWorkbook driversBook, Activity[] activities) {
            ExcelSheet internalDriverSettingsSheet = new ExcelSheet("Internal drivers", driversBook);
            ExcelSheet unavailabilitySettingsSheet = new ExcelSheet("Internal driver unavailability", driversBook);

            (int[][] internalDriversHomeTravelTimes, int[][] internalDriversHomeTravelDistances, string[] travelInfoInternalDriverNames, _) = TravelInfoImporter.ImportBipartiteTravelInfo(Path.Combine(DevConfig.IntermediateFolder, "internalTravelInfo.csv"));

            // Process driver unavailability
            Dictionary<string, List<TimeRange>> internalDriversActivityAvailabilities = new Dictionary<string, List<TimeRange>>();
            unavailabilitySettingsSheet.ForEachRow(unavailabilitySettingsRow => {
                string driverName = unavailabilitySettingsSheet.GetStringValue(unavailabilitySettingsRow, "Internal driver name");
                int? startDayIndex = unavailabilitySettingsSheet.GetIntValue(unavailabilitySettingsRow, "Unavailable from | Day") - 1;
                int? startDayHour = unavailabilitySettingsSheet.GetIntValue(unavailabilitySettingsRow, "Unavailable from | Hour");
                int? endDayIndex = unavailabilitySettingsSheet.GetIntValue(unavailabilitySettingsRow, "Unavailable until | Day") - 1;
                int? endDayHour = unavailabilitySettingsSheet.GetIntValue(unavailabilitySettingsRow, "Unavailable until | Hour");
                if (driverName == null || !startDayIndex.HasValue || !startDayHour.HasValue || !endDayIndex.HasValue || !endDayHour.HasValue) return;

                int startTime = startDayIndex.Value * DevConfig.DayLength + startDayHour.Value * DevConfig.HourLength;
                int endTime = endDayIndex.Value * DevConfig.DayLength + endDayHour.Value * DevConfig.HourLength;
                TimeRange unavailability = new TimeRange(startTime, endTime);

                if (internalDriversActivityAvailabilities.ContainsKey(driverName)) {
                    internalDriversActivityAvailabilities[driverName].Add(unavailability);
                } else {
                    internalDriversActivityAvailabilities.Add(driverName, new List<TimeRange>() { unavailability });
                }
            });

            // Process drivers
            List<InternalDriver> internalDrivers = new List<InternalDriver>();
            int requiredInternalDriverCount = 0;
            internalDriverSettingsSheet.ForEachRow(internalDriverSettingsRow => {
                string driverName = internalDriverSettingsSheet.GetStringValue(internalDriverSettingsRow, "Internal driver name");
                string countryQualificationsStr = internalDriverSettingsSheet.GetStringValue(internalDriverSettingsRow, "Country qualifications");
                int? contractTime = internalDriverSettingsSheet.GetIntValue(internalDriverSettingsRow, "Contract hours per week") * DevConfig.HourLength;
                bool? isOptional = internalDriverSettingsSheet.GetBoolValue(internalDriverSettingsRow, "Is optional?");
                if (driverName == null || countryQualificationsStr == null || !contractTime.HasValue || !isOptional.HasValue) return;
                if (contractTime.Value == 0) return;

                if (!isOptional.Value) {
                    requiredInternalDriverCount++;
                }

                // Travel info
                int travelInfoInternalDriverIndex = Array.IndexOf(travelInfoInternalDriverNames, driverName);
                if (travelInfoInternalDriverIndex == -1) {
                    throw new Exception(string.Format("Could not find internal driver `{0}` in internal travel info", driverName));
                }
                int[] homeTravelTimes = internalDriversHomeTravelTimes[travelInfoInternalDriverIndex];
                int[] homeTravelDistance = internalDriversHomeTravelDistances[travelInfoInternalDriverIndex];

                // Availability
                TimeRange[] driverUnavailabilities;
                if (internalDriversActivityAvailabilities.ContainsKey(driverName)) {
                    // Driver has availabilities
                    driverUnavailabilities = internalDriversActivityAvailabilities[driverName].ToArray();
                } else {
                    // Driver has no unavailabilities
                    driverUnavailabilities = Array.Empty<TimeRange>();
                }

                // Qualifications
                string[] countryQualifications = countryQualificationsStr.Split(", ");
                bool[] activityQualifications = DetermineActivityQualificationsFromCountryQualifications(countryQualifications, activities);
                bool isInternational = countryQualifications.Length > 1;

                // Salary info
                InternalSalarySettings salaryInfo = isInternational ? SalaryConfig.InternalInternationalSalaryInfo : SalaryConfig.InternalNationalSalaryInfo;

                // Satisfaction criteria names
                string[] singleModeCriterionNames = new string[] { "Travel time", "Contract time accuracy", "Expected delays", "Consecutive free days", "Resting time" };
                string[] multipleModeCriterionNames = new string[] { "Route variation", "Shift lengths", "Night shifts", "Weekend shifts", "Hotel stays" };
                string[] allCriterionNames = singleModeCriterionNames.Concat(multipleModeCriterionNames).ToArray();

                // Satisfaction criteria
                List<SatisfactionCriterion> satisfactionCriteriaList = new List<SatisfactionCriterion>();
                float maxCriterionWeight = 0;
                for (int criterionIndex = 0; criterionIndex < allCriterionNames.Length; criterionIndex++) {
                    string criterionName = allCriterionNames[criterionIndex];

                    AbstractSatisfactionCriterionInfo criterionInfo;
                    if (multipleModeCriterionNames.Contains(criterionName)) {
                        // Criterion with multiple mode
                        string criterionModeColumnName = string.Format("Satisfaction criterion mode | {0}", criterionName);
                        string criterionMode = internalDriverSettingsSheet.GetStringValue(internalDriverSettingsRow, criterionModeColumnName);
                        criterionInfo = Array.Find(RulesConfig.SatisfactionCriterionInfos, searchCriterionInfo => searchCriterionInfo.Name == criterionName && searchCriterionInfo.Mode == criterionMode);
                        if (criterionInfo == null) {
                            throw new Exception(string.Format("Could not find satisfaction criterion `{0}` with mode `{1}`", criterionName, criterionMode));
                        }
                    } else {
                        // Criterion with single mode
                        criterionInfo = Array.Find(RulesConfig.SatisfactionCriterionInfos, searchCriterionInfo => searchCriterionInfo.Name == criterionName);
                        if (criterionInfo == null) {
                            throw new Exception(string.Format("Unknown satisfaction criterion `{0}`", criterionName));
                        }
                    }

                    string criterionWeightColumnName = string.Format("Satisfaction criterion weight | {0}", criterionName);
                    float criterionWeight = internalDriverSettingsSheet.GetFloatValue(internalDriverSettingsRow, criterionWeightColumnName).Value;
                    maxCriterionWeight = Math.Max(maxCriterionWeight, criterionWeight);

                    satisfactionCriteriaList.Add(new SatisfactionCriterion(criterionInfo, criterionWeight));
                }
                SatisfactionCriterion[] satisfactionCriteria = satisfactionCriteriaList.ToArray();

                // Set satisfaction criteria max weight
                for (int criterionIndex = 0; criterionIndex < satisfactionCriteria.Length; criterionIndex++) {
                    satisfactionCriteria[criterionIndex].SetMaxWeight(maxCriterionWeight);
                }

                int internalDriverIndex = internalDrivers.Count;
                internalDrivers.Add(new InternalDriver(internalDriverIndex, internalDriverIndex, driverName, isInternational, isOptional.Value, homeTravelTimes, homeTravelDistance, driverUnavailabilities, activityQualifications, contractTime.Value, salaryInfo, satisfactionCriteria));
            });
            return (internalDrivers.ToArray(), requiredInternalDriverCount);
        }

        public static (ExternalDriverType[], ExternalDriver[][], Dictionary<(string, bool), ExternalDriver[]>) CreateExternalDrivers(XSSFWorkbook driversBook, Activity[] activities, int internalDriverCount) {
            ExcelSheet externalDriverCompanySettingsSheet = new ExcelSheet("External driver companies", driversBook);

            (int[][] externalDriversHomeTravelTimes, int[][] externalDriversHomeTravelDistances, string[] travelInfoExternalCompanyNames, _) = TravelInfoImporter.ImportBipartiteTravelInfo(Path.Combine(DevConfig.IntermediateFolder, "externalTravelInfo.csv"));

            List<ExternalDriverType> externalDriverTypes = new List<ExternalDriverType>();
            List<ExternalDriver[]> externalDriversByType = new List<ExternalDriver[]>();
            Dictionary<(string, bool), ExternalDriver[]> externalDriversByTypeDict = new Dictionary<(string, bool), ExternalDriver[]>();
            int allDriverIndex = internalDriverCount;
            int externalDriverTypeIndex = 0;
            externalDriverCompanySettingsSheet.ForEachRow(externalDriverCompanySettingsRow => {
                string externalDriverTypeName = externalDriverCompanySettingsSheet.GetStringValue(externalDriverCompanySettingsRow, "External driver type name");
                string companyName = externalDriverCompanySettingsSheet.GetStringValue(externalDriverCompanySettingsRow, "Company name in data");
                string countryQualificationsStr = externalDriverCompanySettingsSheet.GetStringValue(externalDriverCompanySettingsRow, "Country qualifications");
                bool? isHotelAllowed = externalDriverCompanySettingsSheet.GetBoolValue(externalDriverCompanySettingsRow, "Allows hotel stays?");
                int? minShiftCount = externalDriverCompanySettingsSheet.GetIntValue(externalDriverCompanySettingsRow, "Minimum shift count");
                int? maxShiftCount = externalDriverCompanySettingsSheet.GetIntValue(externalDriverCompanySettingsRow, "Maximum shift count");
                if (companyName == null || externalDriverTypeName == null || countryQualificationsStr == null || !isHotelAllowed.HasValue || !minShiftCount.HasValue || !maxShiftCount.HasValue) return;
                if (maxShiftCount.Value == 0) return;

                string[] countryQualifications = countryQualificationsStr.Split(", ");
                bool isInternational = countryQualifications.Length > 1;

                int travelInfoExternalCompanyIndex = Array.IndexOf(travelInfoExternalCompanyNames, externalDriverTypeName);
                if (travelInfoExternalCompanyIndex == -1) {
                    throw new Exception(string.Format("Could not find external driver type `{0}` in external travel info", externalDriverTypeName));
                }
                int[] homeTravelTimes = externalDriversHomeTravelTimes[travelInfoExternalCompanyIndex];
                int[] homeTravelDistances = externalDriversHomeTravelDistances[travelInfoExternalCompanyIndex];

                // Determine qualifications
                bool[] activityQualifications = DetermineActivityQualificationsFromCountryQualifications(countryQualifications, activities);

                // Add external driver type
                externalDriverTypes.Add(new ExternalDriverType(externalDriverTypeName, isInternational, isHotelAllowed.Value, minShiftCount.Value, maxShiftCount.Value));

                // Add drivers of this type
                ExternalDriver[] currentTypeNationalDrivers = new ExternalDriver[maxShiftCount.Value];
                for (int indexInType = 0; indexInType < maxShiftCount; indexInType++) {
                    ExternalDriver newExternalDriver = new ExternalDriver(allDriverIndex, externalDriverTypeIndex, indexInType, companyName, externalDriverTypeName, isInternational, isHotelAllowed.Value, homeTravelTimes, activityQualifications, homeTravelDistances, SalaryConfig.ExternalNationalSalaryInfo);
                    currentTypeNationalDrivers[indexInType] = newExternalDriver;
                    allDriverIndex++;
                }
                externalDriversByType.Add(currentTypeNationalDrivers);
                externalDriverTypeIndex++;

                // Add to dictionary
                externalDriversByTypeDict.Add((companyName, isInternational), currentTypeNationalDrivers);
            });

            return (externalDriverTypes.ToArray(), externalDriversByType.ToArray(), externalDriversByTypeDict);
        }

        static bool[] DetermineActivityQualificationsFromCountryQualifications(string[] driverCountryQualifications, Activity[] activities) {
            bool[] activityQualifications = new bool[activities.Length];
            for (int activityIndex = 0; activityIndex < activities.Length; activityIndex++) {
                Activity activity = activities[activityIndex];
                activityQualifications[activityIndex] = activity.RequiredCountryQualifications.All(countryQualification => driverCountryQualifications.Contains(countryQualification));
            }
            return activityQualifications;
        }
    }
}

using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class DataDriverProcessor {
        public static InternalDriver[] CreateInternalDrivers(XSSFWorkbook settingsBook, string[] stationCountries) {
            ExcelSheet internalDriverSettingsSheet = new ExcelSheet("Internal drivers", settingsBook);

            (int[][] internalDriversHomeTravelTimes, int[][] internalDriversHomeTravelDistances, string[] travelInfoInternalDriverNames, _) = TravelInfoImporter.ImportBipartiteTravelInfo(Path.Combine(AppConfig.IntermediateFolder, "internalTravelInfo.csv"));

            List<InternalDriver> internalDrivers = new List<InternalDriver>();
            internalDriverSettingsSheet.ForEachRow(internalDriverSettingsRow => {
                string driverName = internalDriverSettingsSheet.GetStringValue(internalDriverSettingsRow, "Internal driver name");
                int? contractTime = internalDriverSettingsSheet.GetIntValue(internalDriverSettingsRow, "Hours per week") * MiscConfig.HourLength;
                string countryQualificationsStr = internalDriverSettingsSheet.GetStringValue(internalDriverSettingsRow, "Country qualifications");
                if (driverName == null || !contractTime.HasValue || countryQualificationsStr == null) return;
                if (contractTime.Value == 0) return;

                string[] countryQualifications = countryQualificationsStr.Split(", ");
                bool isInternational = countryQualifications.Length > 1;

                int travelInfoInternalDriverIndex = Array.IndexOf(travelInfoInternalDriverNames, driverName);
                if (travelInfoInternalDriverIndex == -1) {
                    throw new Exception(string.Format("Could not find internal driver `{0}` in internal travel info", driverName));
                }
                int[] homeTravelTimes = internalDriversHomeTravelTimes[travelInfoInternalDriverIndex];
                int[] homeTravelDistance = internalDriversHomeTravelDistances[travelInfoInternalDriverIndex];

                // Determine track proficiencies
                // Temp: use country knowledge while route knowledge is not available
                bool[,] trackProficiencies = DetermineTrackProficienciesFromCountryQualifications(countryQualifications, stationCountries);

                InternalSalarySettings salaryInfo = isInternational ? SalaryConfig.InternalInternationalSalaryInfo : SalaryConfig.InternalNationalSalaryInfo;

                int internalDriverIndex = internalDrivers.Count;
                internalDrivers.Add(new InternalDriver(internalDriverIndex, internalDriverIndex, driverName, isInternational, homeTravelTimes, homeTravelDistance, trackProficiencies, contractTime.Value, salaryInfo));
            });
            return internalDrivers.ToArray();
        }

        // TODO: use this when route knowledge data is available
        static bool[][,] ParseInternalDriverTrackProficiencies(ExcelSheet routeKnowledgeTable, string[] internalDriverNames, string[] stationNames) {
            bool[][,] internalDriverProficiencies = new bool[internalDriverNames.Length][,];
            for (int driverIndex = 0; driverIndex < internalDriverNames.Length; driverIndex++) {
                internalDriverProficiencies[driverIndex] = new bool[stationNames.Length, stationNames.Length];

                // Everyone is proficient when staying in the same location
                for (int stationIndex = 0; stationIndex < stationNames.Length; stationIndex++) {
                    internalDriverProficiencies[driverIndex][stationIndex, stationIndex] = true;
                }
            }

            routeKnowledgeTable.ForEachRow(routeKnowledgeRow => {
                // Get station indices
                string station1Name = routeKnowledgeTable.GetStringValue(routeKnowledgeRow, "OriginLocationName");
                int station1Index = Array.IndexOf(stationNames, station1Name);
                string station2Name = routeKnowledgeTable.GetStringValue(routeKnowledgeRow, "DestinationLocationName");
                int station2Index = Array.IndexOf(stationNames, station2Name);
                if (station1Index == -1 || station2Index == -1) return;

                // Get driver index
                string driverName = routeKnowledgeTable.GetStringValue(routeKnowledgeRow, "EmployeeName");
                int driverIndex = Array.IndexOf(internalDriverNames, driverName);
                if (driverIndex == -1) return;

                internalDriverProficiencies[driverIndex][station1Index, station2Index] = true;
                internalDriverProficiencies[driverIndex][station2Index, station1Index] = true;
            });

            return internalDriverProficiencies;
        }

        public static (ExternalDriverType[], ExternalDriver[][], Dictionary<(string, bool), ExternalDriver[]>) CreateExternalDrivers(XSSFWorkbook settingsBook, string[] stationCountries, int internalDriverCount) {
            ExcelSheet externalDriverCompanySettingsSheet = new ExcelSheet("External driver companies", settingsBook);

            (int[][] externalDriversHomeTravelTimes, int[][] externalDriversHomeTravelDistances, string[] travelInfoExternalCompanyNames, _) = TravelInfoImporter.ImportBipartiteTravelInfo(Path.Combine(AppConfig.IntermediateFolder, "externalTravelInfo.csv"));

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

                string[] countryQualifications = countryQualificationsStr.Split(", ");
                bool isInternational = countryQualifications.Length > 1;

                int travelInfoExternalCompanyIndex = Array.IndexOf(travelInfoExternalCompanyNames, companyName);
                if (travelInfoExternalCompanyIndex == -1) {
                    throw new Exception(string.Format("Could not find external company `{0}` in external travel info", companyName));
                }
                int[] homeTravelTimes = externalDriversHomeTravelTimes[travelInfoExternalCompanyIndex];
                int[] homeTravelDistances = externalDriversHomeTravelDistances[travelInfoExternalCompanyIndex];

                // Determine track proficiencies
                bool[,] trackProficiencies = DetermineTrackProficienciesFromCountryQualifications(countryQualifications, stationCountries);

                // Add external driver type
                string typeName = string.Format("{0}", companyName);
                externalDriverTypes.Add(new ExternalDriverType(typeName, isInternational, isHotelAllowed.Value, minShiftCount.Value, maxShiftCount.Value));

                // Add drivers of this type
                ExternalDriver[] currentTypeNationalDrivers = new ExternalDriver[maxShiftCount.Value];
                for (int indexInType = 0; indexInType < maxShiftCount; indexInType++) {
                    ExternalDriver newExternalDriver = new ExternalDriver(allDriverIndex, externalDriverTypeIndex, indexInType, companyName, externalDriverTypeName, isInternational, isHotelAllowed.Value, homeTravelTimes, trackProficiencies, homeTravelDistances, SalaryConfig.ExternalNationalSalaryInfo);
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

        static bool[,] DetermineTrackProficienciesFromCountryQualifications(string[] countryQualifications, string[] stationCountries) {
            bool[,] trackProficiencies = new bool[stationCountries.Length, stationCountries.Length];
            for (int station1Index = 0; station1Index < stationCountries.Length; station1Index++) {
                for (int station2Index = 0; station2Index < stationCountries.Length; station2Index++) {
                    trackProficiencies[station1Index, station2Index] = countryQualifications.Contains(stationCountries[station1Index]) && countryQualifications.Contains(stationCountries[station2Index]);
                }
            }
            return trackProficiencies;
        }
    }
}

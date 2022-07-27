/*
 * Process salaries based on settings
*/

namespace DriverPlannerShared {
    public static class DataSalaryProcessor {
        public static SalarySettings[] GetSalarySettingsByDriverType(int timeframeLength) {
            SalarySettings[] salarySettingsByDriverType = new SalarySettings[] {
                SalaryConfig.InternalNationalSalaryInfo,
                SalaryConfig.InternalInternationalSalaryInfo,
                SalaryConfig.ExternalNationalSalaryInfo,
                SalaryConfig.ExternalInternationalSalaryInfo,
            };
            for (int driverTypeIndex = 0; driverTypeIndex < salarySettingsByDriverType.Length; driverTypeIndex++) {
                SalarySettings typeSalarySettings = salarySettingsByDriverType[driverTypeIndex];
                SalaryRateBlock[] processedSalaryRates = ProcessSalaryRates(typeSalarySettings, timeframeLength);
                typeSalarySettings.Init(driverTypeIndex, processedSalaryRates);
            }
            return salarySettingsByDriverType;
        }

        static SalaryRateBlock[] ProcessSalaryRates(SalarySettings salarySettings, int timeframeLength) {
            // Repeat salary rate to cover entire week
            int timeframeDayCount = (int)Math.Floor((float)timeframeLength / DevConfig.DayLength) + 1;
            List<SalaryRateBlock> processedSalaryRates = new List<SalaryRateBlock>();
            int weekPartIndex = 0;
            bool isCurrentlyWeekend = RulesConfig.WeekPartsForWeekend[weekPartIndex].IsSelected;
            for (int dayIndex = 0; dayIndex < timeframeDayCount; dayIndex++) {
                for (int salaryRateIndex = 0; salaryRateIndex < salarySettings.WeekdaySalaryRates.Length; salaryRateIndex++) {
                    int rateStartTime = dayIndex * DevConfig.DayLength + salarySettings.WeekdaySalaryRates[salaryRateIndex].StartTime;

                    while (weekPartIndex + 1 < RulesConfig.WeekPartsForWeekend.Length && RulesConfig.WeekPartsForWeekend[weekPartIndex + 1].StartTime <= rateStartTime) {
                        weekPartIndex++;
                        isCurrentlyWeekend = RulesConfig.WeekPartsForWeekend[weekPartIndex].IsSelected;

                        SalaryRateBlock previousSalaryRateInfo = salaryRateIndex > 0 ? salarySettings.WeekdaySalaryRates[salaryRateIndex - 1] : new SalaryRateBlock(-1, 0, 0);
                        if (isCurrentlyWeekend) {
                            // Start weekend within previous salary rate
                            processedSalaryRates.Add(new SalaryRateBlock(RulesConfig.WeekPartsForWeekend[weekPartIndex].StartTime, salarySettings.WeekendSalaryRate, previousSalaryRateInfo.ContinuingRate));
                        } else {
                            // End weekend within previous salary rate
                            processedSalaryRates.Add(new SalaryRateBlock(RulesConfig.WeekPartsForWeekend[weekPartIndex].StartTime, previousSalaryRateInfo.SalaryRate, previousSalaryRateInfo.ContinuingRate));
                        }
                    }

                    // Start current salary rate
                    float currentSalaryRate = isCurrentlyWeekend ? salarySettings.WeekendSalaryRate : salarySettings.WeekdaySalaryRates[salaryRateIndex].SalaryRate;
                    processedSalaryRates.Add(new SalaryRateBlock(rateStartTime, currentSalaryRate, salarySettings.WeekdaySalaryRates[salaryRateIndex].ContinuingRate));
                }
            }
            return processedSalaryRates.ToArray();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class DataShiftProcessor {
        public static MainShiftInfo[,] GetMainShiftInfos(SalarySettings[] salarySettingsByDriverType, int timeframeLength) {
            int timeframeLengthWithFinalTravel = timeframeLength + 24 * 60;
            int roundedTimeStepCount = timeframeLengthWithFinalTravel / DevConfig.RoundedTimeStepSize;

            MainShiftInfo[,] mainShiftInfos = new MainShiftInfo[roundedTimeStepCount, roundedTimeStepCount];
            for (int startTimeStepIndex = 0; startTimeStepIndex < roundedTimeStepCount; startTimeStepIndex++) {
                for (int endTimeStepIndex = startTimeStepIndex; endTimeStepIndex < roundedTimeStepCount; endTimeStepIndex++) {
                    int mainShiftStartTime = startTimeStepIndex * DevConfig.RoundedTimeStepSize;
                    int realMainShiftEndTime = endTimeStepIndex * DevConfig.RoundedTimeStepSize;
                    int realMainShiftLength = realMainShiftEndTime - mainShiftStartTime;

                    // Determine shift info for driver types
                    DriverTypeMainShiftInfo[] mainShiftInfoByDriverType = new DriverTypeMainShiftInfo[salarySettingsByDriverType.Length];
                    for (int driverTypeIndex = 0; driverTypeIndex < salarySettingsByDriverType.Length; driverTypeIndex++) {
                        mainShiftInfoByDriverType[driverTypeIndex] = GetDriverTypeShiftInfo(mainShiftStartTime, realMainShiftEndTime, salarySettingsByDriverType[driverTypeIndex]);
                    }

                    // Get time in night and weekend
                    (int mainShiftTimeAtNight, int mainShiftTimeInWeekend) = GetShiftNightWeekendTime(mainShiftStartTime, realMainShiftEndTime, timeframeLength);

                    bool isNightShiftByLaw = RulesConfig.IsNightShiftByLawFunc(mainShiftTimeAtNight, realMainShiftLength);
                    bool isNightShiftByCompanyRules = RulesConfig.IsNightShiftByCompanyRulesFunc(mainShiftTimeAtNight, realMainShiftLength);
                    bool isWeekendShiftByCompanyRules = RulesConfig.IsWeekendShiftByCompanyRulesFunc(mainShiftTimeInWeekend, realMainShiftLength);

                    int maxMainShiftLength, maxFullShiftLength, minRestTimeAfter;
                    if (isNightShiftByLaw) {
                        maxMainShiftLength = RulesConfig.MaxMainNightShiftLength;
                        maxFullShiftLength = RulesConfig.MaxFullNightShiftLength;
                        minRestTimeAfter = RulesConfig.MinRestTimeAfterNightShift;
                    } else {
                        maxMainShiftLength = RulesConfig.MaxMainDayShiftLength;
                        maxFullShiftLength = RulesConfig.MaxFullDayShiftLength;
                        minRestTimeAfter = RulesConfig.MinRestTimeAfterDayShift;
                    }

                    int maxShiftLengthViolationAmount = Math.Max(0, realMainShiftLength - maxMainShiftLength);

                    mainShiftInfos[startTimeStepIndex, endTimeStepIndex] = new MainShiftInfo(realMainShiftLength, maxFullShiftLength, minRestTimeAfter, maxShiftLengthViolationAmount, isNightShiftByLaw, isNightShiftByCompanyRules, isWeekendShiftByCompanyRules, mainShiftInfoByDriverType);
                }
            }

            return mainShiftInfos;
        }

        static DriverTypeMainShiftInfo GetDriverTypeShiftInfo(int mainShiftStartTime, int realMainShiftEndTime, SalarySettings salarySettings) {
            // Determine shift times, while keeping in mind the minimum paid time
            int minPaidMainShiftLength = Math.Max(salarySettings.MinPaidShiftTime, realMainShiftEndTime - mainShiftStartTime);
            int minPaidMainShiftEndTime = mainShiftStartTime + minPaidMainShiftLength;

            (float mainShiftCost, List<ComputedSalaryRateBlock> mainShiftSalaryBlocks) = GetSalaryInTimeRange(mainShiftStartTime, minPaidMainShiftEndTime, salarySettings);

            return new DriverTypeMainShiftInfo(minPaidMainShiftLength, mainShiftCost, mainShiftSalaryBlocks);
        }

        static (float, List<ComputedSalaryRateBlock>) GetSalaryInTimeRange(int mainShiftStartTime, int mainShiftEndTime, SalarySettings salarySettings) {
            // Determine shift cost from the different salary rates; final block is skipped since we copied beyond timeframe length
            float? shiftContinuingRate = null;
            float cost = 0;
            List<ComputedSalaryRateBlock> salaryBlocks = new List<ComputedSalaryRateBlock>();
            for (int salaryRateIndex = 0; salaryRateIndex < salarySettings.ProcessedSalaryRates.Length - 1; salaryRateIndex++) {
                SalaryRateBlock salaryRateInfo = salarySettings.ProcessedSalaryRates[salaryRateIndex];
                SalaryRateBlock nextSalaryRateInfo = salarySettings.ProcessedSalaryRates[salaryRateIndex + 1];
                int timeInRate = GetTimeInRange(mainShiftStartTime, mainShiftEndTime, salaryRateInfo.StartTime, nextSalaryRateInfo.StartTime);

                if (timeInRate == 0) continue;

                // If the shift starts in a continuing rate, store this continuing rate
                if (!shiftContinuingRate.HasValue) {
                    shiftContinuingRate = salaryRateInfo.ContinuingRate;
                }

                float applicableSalaryRate = Math.Max(salaryRateInfo.SalaryRate, shiftContinuingRate.Value);
                float costInRate = timeInRate * applicableSalaryRate;
                cost += costInRate;

                int salaryStartTime = Math.Max(salaryRateInfo.StartTime, mainShiftStartTime);
                int salaryEndTime = Math.Min(nextSalaryRateInfo.StartTime, mainShiftEndTime);
                bool usesContinuingRate = shiftContinuingRate.Value > salaryRateInfo.SalaryRate;

                ComputedSalaryRateBlock prevComputedSalaryBlock = salaryBlocks.Count > 0 ? salaryBlocks[^1] : null;
                if (prevComputedSalaryBlock == null || prevComputedSalaryBlock.SalaryRate != applicableSalaryRate || prevComputedSalaryBlock.UsesContinuingRate != usesContinuingRate) {
                    salaryBlocks.Add(new ComputedSalaryRateBlock(salaryRateInfo.StartTime, nextSalaryRateInfo.StartTime, salaryStartTime, salaryEndTime, timeInRate, applicableSalaryRate, usesContinuingRate, costInRate));
                } else {
                    prevComputedSalaryBlock.RateEndTime = nextSalaryRateInfo.StartTime;
                    prevComputedSalaryBlock.SalaryEndTime = salaryEndTime;
                    prevComputedSalaryBlock.SalaryDuration += timeInRate;
                    prevComputedSalaryBlock.CostInRate += costInRate;
                }
            }

            return (cost, salaryBlocks);
        }

        static (int, int) GetShiftNightWeekendTime(int mainShiftStartTime, int mainShiftEndTime, int timeframeLength) {
            // Repeat day parts for night info to cover entire week
            int timeframeDayCount = (int)Math.Floor((float)timeframeLength / DevConfig.DayLength) + 1;
            List<TimePart> weekPartsForNight = new List<TimePart>();
            for (int dayIndex = 0; dayIndex < timeframeDayCount; dayIndex++) {
                for (int i = 0; i < RulesConfig.DayPartsForNight.Length; i++) {
                    int rateStartTime = dayIndex * DevConfig.DayLength + RulesConfig.DayPartsForNight[i].StartTime;
                    weekPartsForNight.Add(new TimePart(rateStartTime, RulesConfig.DayPartsForNight[i].IsSelected));
                }
            }

            // Determine shift times at night
            int mainShiftTimeAtNight = GetTimeInSelectedTimeParts(mainShiftStartTime, mainShiftEndTime, weekPartsForNight.ToArray());
            int mainShiftTimeInWeekend = GetTimeInSelectedTimeParts(mainShiftStartTime, mainShiftEndTime, RulesConfig.WeekPartsForWeekend);

            return (mainShiftTimeAtNight, mainShiftTimeInWeekend);
        }

        // Note that last part is not counted, since this should be the end of the timeframe
        static int GetTimeInSelectedTimeParts(int startTime, int endTime, TimePart[] timeParts) {
            int timeInParts = 0;
            for (int weekPartIndex = 0; weekPartIndex < timeParts.Length - 1; weekPartIndex++) {
                TimePart weekPart = timeParts[weekPartIndex];
                if (!weekPart.IsSelected) continue;

                TimePart nextWeekPart = timeParts[weekPartIndex + 1];
                int timeInPart = GetTimeInRange(startTime, endTime, weekPart.StartTime, nextWeekPart.StartTime);

                timeInParts += timeInPart;
            }
            return timeInParts;
        }

        static int GetTimeInRange(int startTime, int endTime, int rangeStartTime, int rangeEndTime) {
            int timeBeforeRange = Math.Max(0, rangeStartTime - startTime);
            int timeAfterRange = Math.Max(0, endTime - rangeEndTime);
            int timeInRange = Math.Max(0, endTime - startTime - timeBeforeRange - timeAfterRange);
            return timeInRange;
        }
    }
}

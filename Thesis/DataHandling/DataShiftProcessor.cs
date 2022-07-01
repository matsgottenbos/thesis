using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class DataShiftProcessor {
        public static ShiftInfo[,] GetShiftInfos(Activity[] activities, int timeframeLength) {
            ShiftInfo[,] shiftInfos = new ShiftInfo[activities.Length, activities.Length];
            for (int firstActivityIndex = 0; firstActivityIndex < activities.Length; firstActivityIndex++) {
                for (int lastActivityIndex = 0; lastActivityIndex < activities.Length; lastActivityIndex++) {
                    Activity shiftFirstActivity = activities[firstActivityIndex];
                    Activity shiftLastActivity = activities[lastActivityIndex];

                    // Determine shift times
                    int mainShiftStartTime = shiftFirstActivity.StartTime;
                    int mainShiftEndTime = shiftLastActivity.EndTime;
                    int mainShiftLength = Math.Max(0, mainShiftEndTime - mainShiftStartTime);

                    // Determine shift costs for driver types
                    SalarySettings[] salarySettingsByDriverType = new SalarySettings[] {
                        SalaryConfig.InternalNationalSalaryInfo,
                        SalaryConfig.InternalInternationalSalaryInfo,
                        SalaryConfig.ExternalNationalSalaryInfo,
                        SalaryConfig.ExternalInternationalSalaryInfo,
                    };
                    int[] administrativeMainShiftLengthByDriverType = new int[salarySettingsByDriverType.Length];
                    float[] mainShiftCostByDriverType = new float[salarySettingsByDriverType.Length];
                    List<ComputedSalaryRateBlock>[] computeSalaryRateBlocksByType = new List<ComputedSalaryRateBlock>[salarySettingsByDriverType.Length];
                    for (int driverTypeIndex = 0; driverTypeIndex < salarySettingsByDriverType.Length; driverTypeIndex++) {
                        SalarySettings typeSalarySettings = salarySettingsByDriverType[driverTypeIndex];
                        typeSalarySettings.SetDriverTypeIndex(driverTypeIndex);
                        (administrativeMainShiftLengthByDriverType[driverTypeIndex], mainShiftCostByDriverType[driverTypeIndex], computeSalaryRateBlocksByType[driverTypeIndex]) = GetMainShiftCost(shiftFirstActivity, shiftLastActivity, typeSalarySettings, timeframeLength);
                    }

                    // Get time in night and weekend
                    (int mainShiftTimeAtNight, int mainShiftTimeInWeekend) = GetShiftNightWeekendTime(shiftFirstActivity, shiftLastActivity, timeframeLength);

                    bool isNightShiftByLaw = RulesConfig.IsNightShiftByLawFunc(mainShiftTimeAtNight, mainShiftLength);
                    bool isNightShiftByCompanyRules = RulesConfig.IsNightShiftByCompanyRulesFunc(mainShiftTimeAtNight, mainShiftLength);
                    bool isWeekendShiftByCompanyRules = RulesConfig.IsWeekendShiftByCompanyRulesFunc(mainShiftTimeInWeekend, mainShiftLength);

                    int maxMainShiftLength, maxFullShiftLength, minRestTimeAfter;
                    if (isNightShiftByLaw) {
                        maxMainShiftLength = RulesConfig.NightMaxMainShiftLength;
                        maxFullShiftLength = RulesConfig.NightMaxFullShiftLength;
                        minRestTimeAfter = RulesConfig.NightShiftMinRestTime;
                    } else {
                        maxMainShiftLength = RulesConfig.NormalMaxMainShiftLength;
                        maxFullShiftLength = RulesConfig.NormalMaxFullShiftLength;
                        minRestTimeAfter = RulesConfig.NormalShiftMinRestTime;
                    }

                    shiftInfos[firstActivityIndex, lastActivityIndex] = new ShiftInfo(mainShiftLength, maxMainShiftLength, maxFullShiftLength, minRestTimeAfter, administrativeMainShiftLengthByDriverType, mainShiftCostByDriverType, computeSalaryRateBlocksByType, isNightShiftByLaw, isNightShiftByCompanyRules, isWeekendShiftByCompanyRules);
                }
            }

            return shiftInfos;
        }

        static (int, float, List<ComputedSalaryRateBlock>) GetMainShiftCost(Activity shiftFirstActivity, Activity shiftLastActivity, SalarySettings salaryInfo, int timeframeLength) {
            // Repeat salary rate to cover entire week
            int timeframeDayCount = (int)Math.Floor((float)timeframeLength / MiscConfig.DayLength) + 1;
            List<SalaryRateBlock> processedSalaryRates = new List<SalaryRateBlock>();
            int weekPartIndex = 0;
            bool isCurrentlyWeekend = RulesConfig.WeekPartsForWeekend[weekPartIndex].IsSelected;
            for (int dayIndex = 0; dayIndex < timeframeDayCount; dayIndex++) {
                for (int salaryRateIndex = 0; salaryRateIndex < salaryInfo.WeekdaySalaryRates.Length; salaryRateIndex++) {
                    int rateStartTime = dayIndex * MiscConfig.DayLength + salaryInfo.WeekdaySalaryRates[salaryRateIndex].StartTime;

                    while (weekPartIndex + 1 < RulesConfig.WeekPartsForWeekend.Length && RulesConfig.WeekPartsForWeekend[weekPartIndex + 1].StartTime <= rateStartTime) {
                        weekPartIndex++;
                        isCurrentlyWeekend = RulesConfig.WeekPartsForWeekend[weekPartIndex].IsSelected;

                        SalaryRateBlock previousSalaryRateInfo = salaryRateIndex > 0 ? salaryInfo.WeekdaySalaryRates[salaryRateIndex - 1] : new SalaryRateBlock(-1, 0, 0);
                        if (isCurrentlyWeekend) {
                            // Start weekend within previous salary rate
                            processedSalaryRates.Add(new SalaryRateBlock(RulesConfig.WeekPartsForWeekend[weekPartIndex].StartTime, salaryInfo.WeekendSalaryRate, previousSalaryRateInfo.ContinuingRate));
                        } else {
                            // End weekend within previous salary rate
                            processedSalaryRates.Add(new SalaryRateBlock(RulesConfig.WeekPartsForWeekend[weekPartIndex].StartTime, previousSalaryRateInfo.SalaryRate, previousSalaryRateInfo.ContinuingRate));
                        }
                    }

                    // Start current salary rate
                    float currentSalaryRate = isCurrentlyWeekend ? salaryInfo.WeekendSalaryRate : salaryInfo.WeekdaySalaryRates[salaryRateIndex].SalaryRate;
                    processedSalaryRates.Add(new SalaryRateBlock(rateStartTime, currentSalaryRate, salaryInfo.WeekdaySalaryRates[salaryRateIndex].ContinuingRate));
                }
            }

            // Determine shift times, while keeping in mind the minimum paid time
            int mainShiftStartTime = shiftFirstActivity.StartTime;
            int realMainShiftEndTime = shiftLastActivity.EndTime;
            int administrativeMainShiftLength = Math.Max(salaryInfo.MinPaidShiftTime, realMainShiftEndTime - mainShiftStartTime);
            int administrativeMainShiftEndTime = mainShiftStartTime + administrativeMainShiftLength;

            // Determine shift cost from the different salary rates; final block is skipped since we copied beyond timeframe length
            float? shiftContinuingRate = null;
            float mainShiftCost = 0;
            List<ComputedSalaryRateBlock> computeSalaryRateBlocks = new List<ComputedSalaryRateBlock>();
            for (int salaryRateIndex = 0; salaryRateIndex < processedSalaryRates.Count - 1; salaryRateIndex++) {
                SalaryRateBlock salaryRateInfo = processedSalaryRates[salaryRateIndex];
                SalaryRateBlock nextSalaryRateInfo = processedSalaryRates[salaryRateIndex + 1];
                int mainShiftTimeInRate = GetTimeInRange(mainShiftStartTime, administrativeMainShiftEndTime, salaryRateInfo.StartTime, nextSalaryRateInfo.StartTime);

                if (mainShiftTimeInRate == 0) continue;

                // If the shift starts in a continuing rate, store this continuing rate
                if (!shiftContinuingRate.HasValue) {
                    shiftContinuingRate = salaryRateInfo.ContinuingRate;
                }

                float applicableSalaryRate = Math.Max(salaryRateInfo.SalaryRate, shiftContinuingRate.Value);
                float mainShiftCostInRate = mainShiftTimeInRate * applicableSalaryRate;
                mainShiftCost += mainShiftCostInRate;

                int salaryStartTime = Math.Max(salaryRateInfo.StartTime, mainShiftStartTime);
                int salaryEndTime = Math.Min(nextSalaryRateInfo.StartTime, administrativeMainShiftEndTime);
                bool usesContinuingRate = shiftContinuingRate.Value > salaryRateInfo.SalaryRate;

                ComputedSalaryRateBlock prevComputedSalaryBlock = computeSalaryRateBlocks.Count > 0 ? computeSalaryRateBlocks[^1] : null;
                if (prevComputedSalaryBlock == null || prevComputedSalaryBlock.SalaryRate != applicableSalaryRate || prevComputedSalaryBlock.UsesContinuingRate != usesContinuingRate) {
                    computeSalaryRateBlocks.Add(new ComputedSalaryRateBlock(salaryRateInfo.StartTime, nextSalaryRateInfo.StartTime, salaryStartTime, salaryEndTime, mainShiftTimeInRate, applicableSalaryRate, usesContinuingRate, mainShiftCostInRate));
                } else {
                    prevComputedSalaryBlock.RateEndTime = nextSalaryRateInfo.StartTime;
                    prevComputedSalaryBlock.SalaryEndTime = salaryEndTime;
                    prevComputedSalaryBlock.SalaryDuration += mainShiftTimeInRate;
                    prevComputedSalaryBlock.MainShiftCostInRate += mainShiftCostInRate;
                }
            }

            return (administrativeMainShiftLength, mainShiftCost, computeSalaryRateBlocks);
        }

        static (int, int) GetShiftNightWeekendTime(Activity shiftFirstActivity, Activity shiftLastActivity, int timeframeLength) {
            // Repeat day parts for night info to cover entire week
            int timeframeDayCount = (int)Math.Floor((float)timeframeLength / MiscConfig.DayLength) + 1;
            List<TimePart> weekPartsForNight = new List<TimePart>();
            for (int dayIndex = 0; dayIndex < timeframeDayCount; dayIndex++) {
                for (int i = 0; i < RulesConfig.DayPartsForNight.Length; i++) {
                    int rateStartTime = dayIndex * MiscConfig.DayLength + RulesConfig.DayPartsForNight[i].StartTime;
                    weekPartsForNight.Add(new TimePart(rateStartTime, RulesConfig.DayPartsForNight[i].IsSelected));
                }
            }

            // Determine shift times, while keeping in mind the minimum paid time
            int mainShiftStartTime = shiftFirstActivity.StartTime;
            int mainShiftEndTime = shiftLastActivity.EndTime;

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

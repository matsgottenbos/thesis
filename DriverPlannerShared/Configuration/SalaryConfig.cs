using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public class SalaryConfig {
        /* Salary rates for driver types */
        /// <summary>Internal national driver salary info.</summary>
        public static InternalSalarySettings InternalNationalSalaryInfo { get; private set; }
        /// <summary>Internal international driver salary info.</summary>
        public static InternalSalarySettings InternalInternationalSalaryInfo { get; private set; }
        /// <summary>External national driver salary info.</summary>
        public static ExternalSalarySettings ExternalNationalSalaryInfo { get; private set; }
        /// <summary>External international driver salary info.</summary>
        public static ExternalSalarySettings ExternalInternationalSalaryInfo { get; private set; }

        public static void Init(XSSFWorkbook settingsBook) {
            ExcelSheet salariesSettingsSheet = new ExcelSheet("Salaries", settingsBook);
            ExcelSheet weekdaySalaryBlocksSettingsSheet = new ExcelSheet("Weekday salary blocks", settingsBook);

            InternalNationalSalaryInfo = ProcessInternalDriverTypeSalaryInfo("Internal national", salariesSettingsSheet, weekdaySalaryBlocksSettingsSheet);
            InternalInternationalSalaryInfo = ProcessInternalDriverTypeSalaryInfo("Internal international", salariesSettingsSheet, weekdaySalaryBlocksSettingsSheet);
            ExternalNationalSalaryInfo = ProcessExternalDriverTypeSalaryInfo("External national", salariesSettingsSheet, weekdaySalaryBlocksSettingsSheet);
            ExternalInternationalSalaryInfo = ProcessExternalDriverTypeSalaryInfo("External international", salariesSettingsSheet, weekdaySalaryBlocksSettingsSheet);
        }

        static InternalSalarySettings ProcessInternalDriverTypeSalaryInfo(string driverType, ExcelSheet salariesSettingsSheet, ExcelSheet weekdaySalaryBlocksSettingsSheet) {
            SalaryRateBlock[] weekdaySalaryRates = ProcessWeekdaySalaryRates(driverType, weekdaySalaryBlocksSettingsSheet);

            Dictionary<string, ICell> valueCellPerDriverTypeSetting = GetDriverTypeSettingsValueCellsAsDict(driverType, salariesSettingsSheet);
            float weekendHourlyRate = ExcelSheet.GetFloatValue(valueCellPerDriverTypeSetting["Weekend rate"]).Value;
            float travelTimeHourlyRate = ExcelSheet.GetFloatValue(valueCellPerDriverTypeSetting["Travel time rate"]).Value;
            float minPaidShiftTimeHours = ExcelSheet.GetFloatValue(valueCellPerDriverTypeSetting["Minimum paid shift time"]).Value;
            float unpaidTravelTimePerShiftHours = ExcelSheet.GetFloatValue(valueCellPerDriverTypeSetting["Unpaid travel time per shift"]).Value;

            return InternalSalarySettings.CreateByHours(weekdaySalaryRates, weekendHourlyRate, travelTimeHourlyRate, minPaidShiftTimeHours, unpaidTravelTimePerShiftHours);
        }

        static ExternalSalarySettings ProcessExternalDriverTypeSalaryInfo(string driverType, ExcelSheet salariesSettingsSheet, ExcelSheet weekdaySalaryBlocksSettingsSheet) {
            SalaryRateBlock[] weekdaySalaryRates = ProcessWeekdaySalaryRates(driverType, weekdaySalaryBlocksSettingsSheet);

            Dictionary<string, ICell> valueCellPerDriverTypeSetting = GetDriverTypeSettingsValueCellsAsDict(driverType, salariesSettingsSheet);
            float weekendHourlyRate = ExcelSheet.GetFloatValue(valueCellPerDriverTypeSetting["Weekend rate"]).Value;
            float travelDistanceRate = ExcelSheet.GetFloatValue(valueCellPerDriverTypeSetting["Travel distance rate"]).Value;
            float minPaidShiftTimeHours = ExcelSheet.GetFloatValue(valueCellPerDriverTypeSetting["Minimum paid shift time"]).Value;
            int unpaidTravelDistancePerShift = ExcelSheet.GetIntValue(valueCellPerDriverTypeSetting["Unpaid travel distance per shift"]).Value;

            return ExternalSalarySettings.CreateByHours(weekdaySalaryRates, weekendHourlyRate, travelDistanceRate, minPaidShiftTimeHours, unpaidTravelDistancePerShift);
        }

        static SalaryRateBlock[] ProcessWeekdaySalaryRates(string driverType, ExcelSheet weekdaySalaryBlocksSettingsSheet) {
            List<SalaryRateBlock> weekdaySalaryRatesList = new List<SalaryRateBlock>();

            weekdaySalaryBlocksSettingsSheet.ForEachRow(weekdaySalaryBlocksSettingsRow => {
                string rowDriverType = weekdaySalaryBlocksSettingsSheet.GetStringValue(weekdaySalaryBlocksSettingsRow, "Driver type");
                if (rowDriverType != driverType) return;

                float startTimeHours = weekdaySalaryBlocksSettingsSheet.GetFloatValue(weekdaySalaryBlocksSettingsRow, "Start hour of part").Value;
                float hourlySalaryRate = weekdaySalaryBlocksSettingsSheet.GetFloatValue(weekdaySalaryBlocksSettingsRow, "Salary rate").Value;
                bool isContinuingRate = weekdaySalaryBlocksSettingsSheet.GetBoolValue(weekdaySalaryBlocksSettingsRow, "Is continuing rate").Value;

                weekdaySalaryRatesList.Add(SalaryRateBlock.CreateByHours(startTimeHours, hourlySalaryRate, isContinuingRate));
            });

            return weekdaySalaryRatesList.ToArray();
        }

        static Dictionary<string, ICell> GetDriverTypeSettingsValueCellsAsDict(string driverType, ExcelSheet sheet) {
            Dictionary<string, ICell> valueCellPerSetting = new Dictionary<string, ICell>();
            sheet.ForEachRow(row => {
                string rowDriverType = sheet.GetStringValue(row, "Driver type");
                if (rowDriverType != driverType) return;

                string name = sheet.GetStringValue(row, "Salary setting");
                if (name == null) return;

                ICell valueCell = row.GetCell(sheet.GetColumnIndex("Value"));
                if (valueCell == null) throw new Exception(string.Format("Value of setting `{0}` was empty", name));

                valueCellPerSetting.Add(name, valueCell);
            });
            return valueCellPerSetting;
        }
    }
}

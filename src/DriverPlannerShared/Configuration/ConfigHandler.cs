using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace DriverPlannerShared {
    public static class ConfigHandler {
        public static void InitAllConfigs() {
            XSSFWorkbook settingsBook = ExcelHelper.ReadExcelFile(Path.Combine(DevConfig.InputFolder, "Settings.xlsx"));
            AppConfig.Init(settingsBook);
            RulesConfig.Init(settingsBook);
            AlgorithmConfig.Init(settingsBook);
            SalaryConfig.Init(settingsBook);
        }

        public static Dictionary<string, ICell> GetSettingsValueCellsAsDict(ExcelSheet sheet) {
            Dictionary<string, ICell> valueCellPerSetting = new Dictionary<string, ICell>();
            sheet.ForEachRow(row => {
                string name = sheet.GetStringValue(row, "Setting name");
                if (name == null) return;

                ICell valueCell = row.GetCell(sheet.GetColumnIndex("Value"));
                if (valueCell == null) throw new Exception(string.Format("Value of setting `{0}` in sheet `{1}` was empty", name, sheet.SheetName));

                valueCellPerSetting.Add(name, valueCell);
            });
            return valueCellPerSetting;
        }

        public static int HourToMinuteValue(float hourValue) {
            return (int)(hourValue * DevConfig.HourLength);
        }
    }
}

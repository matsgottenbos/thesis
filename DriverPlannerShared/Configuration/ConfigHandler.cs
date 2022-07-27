﻿using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public static class ConfigHandler {
        public static void InitAllConfigs() {
            XSSFWorkbook settingsBook = ExcelHelper.ReadExcelFile(Path.Combine(DevConfig.InputFolder, "settings.xlsx"));
            AppConfig.Init(settingsBook);
            RulesConfig.Init(settingsBook);
            SaConfig.Init(settingsBook);
            SalaryConfig.Init(settingsBook);
        }

        public static Dictionary<string, ICell> GetSettingsValueCellsAsDict(ExcelSheet sheet) {
            Dictionary<string, ICell> valueCellPerSetting = new Dictionary<string, ICell>();
            sheet.ForEachRow(row => {
                string name = sheet.GetStringValue(row, "Setting name");
                if (name == null) return;

                ICell valueCell = row.GetCell(sheet.GetColumnIndex("Value"));
                if (valueCell == null) throw new Exception(string.Format("Value of setting `{0}` was empty", name));

                valueCellPerSetting.Add(name, valueCell);
            });
            return valueCellPerSetting;
        }

        public static int HourToMinuteValue(float hourValue) {
            return (int)(hourValue * DevConfig.HourLength);
        }
    }
}
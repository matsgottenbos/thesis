using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class ExcelHelper {
        public static XSSFWorkbook ReadExcelFile(string filePath) {
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            XSSFWorkbook excelBook = new XSSFWorkbook(fileStream);
            fileStream.Close();
            return excelBook;
        }
    }

    class ExcelSheet {
        readonly string sheetName;
        readonly ISheet sheet;
        readonly Dictionary<string, int> columnNamesToIndices;

        public ExcelSheet(string sheetName, XSSFWorkbook excelBook) {
            this.sheetName = sheetName;
            sheet = excelBook.GetSheet(sheetName);
            if (sheet == null) throw new Exception($"Unknown sheet `{sheetName}`");

            // Parse column headers
            IRow headerRow = sheet.GetRow(0);
            columnNamesToIndices = new Dictionary<string, int>();
            for (int colIndex = 0; colIndex < headerRow.LastCellNum; colIndex++) {
                string headerName = headerRow.GetCell(colIndex).StringCellValue;
                columnNamesToIndices.Add(headerName, colIndex);
            }
        }

        int GetColumnIndex(string columnName) {
            if (!columnNamesToIndices.ContainsKey(columnName)) {
                throw new Exception(string.Format("Could not find column `{0}` in Excel sheet `{1}`", columnName, sheetName));
            }
            return columnNamesToIndices[columnName];
        }

        public void ForEachRow(Action<IRow> rowFunc) {
            for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++) {
                IRow row = sheet.GetRow(rowIndex);
                if (row == null || row.Cells.All(cell => cell.CellType == CellType.Blank)) continue; // Skip empty rows

                rowFunc(row);
            }
        }

        public string GetStringValue(IRow row, string columnName) {
            string stringValue = row.GetCell(GetColumnIndex(columnName))?.StringCellValue;
            if (stringValue == null || stringValue == "") return null;
            return ParseHelper.CleanDataString(stringValue);
        }

        public int? GetIntValue(IRow row, string columnName) {
            ICell cell = row.GetCell(GetColumnIndex(columnName));
            if (cell == null) return null;
            return (int)Math.Round(row.GetCell(GetColumnIndex(columnName)).NumericCellValue);
        }

        public bool? GetBoolValue(IRow row, string columnName) {
            ICell cell = row.GetCell(GetColumnIndex(columnName));
            if (cell == null) return null;
            return row.GetCell(GetColumnIndex(columnName)).BooleanCellValue;
        }

        public DateTime? GetDateValue(IRow row, string columnName) {
            return row.GetCell(GetColumnIndex(columnName))?.DateCellValue;
        }
    }
}

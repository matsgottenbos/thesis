/*
 * Helper methods for reading Excel books
*/

using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public static class ExcelHelper {
        public static XSSFWorkbook ReadExcelFile(string filePath) {
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            XSSFWorkbook excelBook = new XSSFWorkbook(fileStream);
            fileStream.Close();
            return excelBook;
        }
    }

    public class ExcelSheet {
        readonly string sheetName;
        readonly ISheet sheet;
        readonly Dictionary<string, int> columnNamesToIndices;
        readonly int firstContentRowIndex;

        public ExcelSheet(string sheetName, XSSFWorkbook excelBook) {
            this.sheetName = sheetName;
            sheet = excelBook.GetSheet(sheetName);
            if (sheet == null) throw new Exception($"Unknown sheet `{sheetName}`");

            // Parse column headers
            IRow headerRow = sheet.GetRow(0);
            IRow headerRow2 = sheet.GetRow(1);
            columnNamesToIndices = new Dictionary<string, int>();
            bool hasSecondHeaderRow = false;
            for (int colIndex = 0; colIndex < headerRow.LastCellNum; colIndex++) {
                ICell headerCell1 = headerRow.GetCell(colIndex);
                ICell headerCell2 = headerRow2.GetCell(colIndex);
                string headerName;
                if (headerCell1.IsMergedCell && headerCell2.StringCellValue != "") {
                    hasSecondHeaderRow = true;

                    // Get origin cell of merged cell
                    int headerCell1MergeOriginColIndex = colIndex;
                    while (headerCell1.StringCellValue == "") {
                        headerCell1MergeOriginColIndex--;
                        headerCell1 = headerRow.GetCell(headerCell1MergeOriginColIndex);
                    }

                    // Header row is a merged cell, so combine it with the second header row
                    headerName = string.Format("{0} | {1}", headerCell1.StringCellValue, headerCell2.StringCellValue);
                } else {
                    headerName = headerCell1.StringCellValue;
                }
                columnNamesToIndices.Add(headerName, colIndex);
            }
            firstContentRowIndex = hasSecondHeaderRow ? 2 : 1;
        }

        public int GetColumnIndex(string columnName) {
            if (!columnNamesToIndices.ContainsKey(columnName)) {
                throw new Exception(string.Format("Could not find column `{0}` in Excel sheet `{1}`", columnName, sheetName));
            }
            return columnNamesToIndices[columnName];
        }

        public void ForEachRow(Action<IRow> rowFunc) {
            for (int rowIndex = firstContentRowIndex; rowIndex <= sheet.LastRowNum; rowIndex++) {
                IRow row = sheet.GetRow(rowIndex);
                if (row == null || row.Cells.All(cell => cell.CellType == CellType.Blank)) continue; // Skip empty rows

                rowFunc(row);
            }
        }

        public string GetStringValue(IRow row, string columnName) {
            return GetStringValue(row.GetCell(GetColumnIndex(columnName)));
        }
        public static string GetStringValue(ICell cell) {
            string stringValue = cell?.StringCellValue;
            if (stringValue == null || stringValue == "") return null;
            return ParseHelper.CleanDataString(stringValue);
        }

        public int? GetIntValue(IRow row, string columnName) {
            return GetIntValue(row.GetCell(GetColumnIndex(columnName)));
        }
        public static int? GetIntValue(ICell cell) {
            if (cell == null) return null;
            return (int)Math.Round(cell.NumericCellValue);
        }

        public float? GetFloatValue(IRow row, string columnName) {
            return GetFloatValue(row.GetCell(GetColumnIndex(columnName)));
        }
        public static float? GetFloatValue(ICell cell) {
            if (cell == null) return null;
            return (float)cell.NumericCellValue;
        }

        public bool? GetBoolValue(IRow row, string columnName) {
            return GetBoolValue(row.GetCell(GetColumnIndex(columnName)));
        }
        public static bool? GetBoolValue(ICell cell) {
            if (cell == null) return null;
            return cell.BooleanCellValue;
        }

        public DateTime? GetDateValue(IRow row, string columnName) {
            return GetDateValue(row.GetCell(GetColumnIndex(columnName)));
        }
        public static DateTime? GetDateValue(ICell cell) {
            return cell?.DateCellValue;
        }
    }
}

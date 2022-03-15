using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class DataImporter {
        public static void Import() {
            // Import Excel sheet
            FileStream fileStream = new FileStream(Path.Combine(Config.DataFolder, "data.xlsx"), FileMode.Open, FileAccess.Read);
            XSSFWorkbook excelBook = new XSSFWorkbook(fileStream);
            fileStream.Close();

            ISheet dutiesSheet = excelBook.GetSheet("Duties");

            // Read headers
            IRow headerRow = dutiesSheet.GetRow(0);
            Dictionary<string, int> colIndices = new Dictionary<string, int>();
            for (int colIndex = 0; colIndex < headerRow.LastCellNum; colIndex++) {
                string headerName = headerRow.GetCell(colIndex).StringCellValue;
                colIndices.Add(headerName, colIndex);
            }

            // Temp config
            DateTime planningStartDate = new DateTime(2021, 12, 25);
            DateTime planningNextDate = planningStartDate.AddDays(7);

            // Parse trip start and end dates
            List<DateTime> tripStartDates = new List<DateTime>();
            List<DateTime> tripEndDates = new List<DateTime>();
            for (int rowIndex = 1; rowIndex <= dutiesSheet.LastRowNum; rowIndex++) {
                IRow row = dutiesSheet.GetRow(rowIndex);
                if (row == null) continue; // Skip empty rows

                // Save values
                DateTime startDate = row.GetCell(colIndices["PlannedStart"]).DateCellValue;
                if (startDate < planningStartDate || startDate > planningNextDate) continue; // Skip trips outside planning timeframe
                tripStartDates.Add(startDate);
                DateTime endDate = row.GetCell(colIndices["PlannedEnd"]).DateCellValue;
                tripEndDates.Add(endDate);
            }

            Trip[] trips = new Trip[tripStartDates.Count];
            for (int tripIndex = 0; tripIndex < tripStartDates.Count; tripIndex++) {
                int startTime = (int)Math.Round((tripStartDates[tripIndex] - planningStartDate).TotalMinutes);
                int endTime = (int)Math.Round((tripEndDates[tripIndex] - planningStartDate).TotalMinutes);
                int duration = endTime - startTime;

                // TODO: add stations
                trips[tripIndex] = new Trip(-1, new List<int> { -1, -1 }, startTime, endTime, duration);
            }

            Console.ReadLine();
        }
    }
}

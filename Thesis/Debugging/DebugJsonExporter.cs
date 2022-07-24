using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Thesis {
    class DebugJsonExporter {
        readonly Instance instance;
        string outputSubfolderPath;

        public DebugJsonExporter(Instance instance) {
            this.instance = instance;
            outputSubfolderPath = CreateOutputFolder("debug");

            ExportAssignment("15 13 15 1 15 13 13 4 10 1 1 3 6 12 4 12 1 4 e3.2 e3.2 14 10 14 10 1 e1.1 10 22 7 2 3h 2 8 16h e1.1 5 2 5 5 8 e3.5 8h 25 26 26 9 26 9 15 9 0 13 e3.5 4 0 e6.1 1 6 e6.1 1 0 4 4 9 10 9 14 12 13 3 e8.4 e8.0h 12h 16 19 16 10 1 15 21 e8.9 1 0 4h e8.9 16 21 3 16 16 e1.3 22 21 22 e1.3 2 5 2 8 5 2 8 2 24 9 6 24 23 9 6 17 14 e8.2 e1.0 7 17 6 10 10 7 13 12 9 4 6 6 13 15 6h 10 15 14 7 7 e8.2 25 13 15 12 26 e8.0 12 15 1 1 1 1 1 21 11 19 11 19 2 16 11 2 7 11 e6.0 e3.1 e3.4 8 e6.4 0 3 3 5 6 5 7 16 4 0 16 3 e3.1 5 e3.4 e6.0 6 6 2 e8.7 5 e8.7 6 e3.4 14 4 3 14 0 14 5 6 14 9 14 6 13 9 13 9 13 15 13 23 24 23 17 17 15 11 23 1 10 7 15 12 8 12 11 1 15 10 15 10 12 e8.0 e3.3 11 16 7 10 8 12 0 5 12 0 20 16 e8.0 e3.3 5 e3.5 16 e3.3 0 20 20 9 9 18 18 9 18 6 3 2 2 14 e8.5 2 4 4 2 e8.8 14 4 3 2 2 6 2 6 14 4 14 13 5 5 5 e8.8 11 1 11 5 1 13 1 11 1 e6.3 e6.2 12 16 10 0 8 7 16 0 8 3 e1.2 e3.6 e6.5 20 18 20 3 18 20 8 e3.6 e6.5 8 e1.2 e3.6 e6.5 0 e6.5 11 11");
        }

        void ExportAssignment(string assignmentStr) {
            SaInfo info = ParseHelper.ParseAssignmentString(assignmentStr, instance);
            JsonOutputHelper.ExportAssignmentInfoJson(outputSubfolderPath, info);
        }

        public static string CreateOutputFolder(string debugName) {
            string dateStr = string.Format("{0}-{1}", DateTime.Now.ToString("yyyy-MM-dd-HH-mm"), debugName);
            string outputSubfolderPath = Path.Combine(DevConfig.OutputFolder, dateStr);
            Directory.CreateDirectory(outputSubfolderPath);
            return outputSubfolderPath;
        }
    }
}

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

            ExportAssignment("10 10 10 16 9 16 7 7 16 5 e5.0 e5.0 9 5 7 16 5 8 16 0 e5.0 7 5 7h 5 8 0 3 8 8 5 e5.0 11 e4.16 0 1 1 2 0 8 1 2 1 e4.16 2 e4.16 1 e4.13 2 e4.13 2 e4.13 4 4 6 4 6 e0.11 6 4 12 15 6 4 6 12 13 4 e0.11 13 4 12 5 5 15 16 15 5 e2.11 13 4 8 e0.3 12 5 4h 3 5 13 7 16 12 12 16 8 7 6h 13 16 5 12h e2.11 7 7 16 7 e6.3 3 3 3 e6.3 1 2 2 1 2 2 10 1h 9 10 14 15 9 4 9 15 e4.16 10 9 10 4 e0.5 11 8 10 15 4 4 10 8 14 14 9 14 8 9 0 10h 15 11 15 11 6 6 8 8 12 e5.0 0 e0.5 11 6 0 8 6 6 0 6 e5.0 12 13 13 13 13 1 e0.9 1 e0.9 1 e0.11 e0.9 16 3 10 5 10 2 5 e3.0 e0.9 5 1h 10 14 10 e0.11 14 e5.3 4 16 16 4 7 16 e2.0h 8 14 e3.0 8 e3.0 2 10 3 e5.3 2 16 5 16 5 3h 14 5 2 7 8 e3.0 16h 4 e5.3 15 2 7 15 7 15 15 9 e4.2 9 e2.5 9 e4.2 6 1 3 0 3 0 6 11 3 e1.2 1 12 12 11 6 6h 16 14 e1.2 e4.11 1 13 1 11 14 13 0 11 16 12 e5.4 1 3 13 0 10 e1.2 10 16 e4.11 14 16 7 10 13 13 e2.0 10 e5.4 e2.6 10 7 7 7 e2.6 6 6 9 9 6 5 9 3 9 9 15 15 0 8 15 2 15 3 2 9 8 5 15h 11h 14 9 9h 2 16 4 5 5h 0 16 13 12 8 12 8 3 2 4 4 14 13 14 0 0h 4 12 16 10 12 10 12 4h 13h 12h 10 10 e2.6 7 11 4 4 15 14 1 4 14 1 2 5 2 5 14 9 1 0 0 13 13 0 2 5 13 14 6 1 1 1 4 14 14 4 2 0 14 1 5 6 2 6 13 6 12 12");
        }

        void ExportAssignment(string assignmentStr) {
            SaInfo info = ParseHelper.ParseAssignmentString(assignmentStr, instance);
            JsonAssignmentHelper.ExportAssignmentInfoJson(outputSubfolderPath, info);
        }

        public static string CreateOutputFolder(string debugName) {
            string dateStr = string.Format("{0}-{1}", DateTime.Now.ToString("yyyy-MM-dd-HH-mm"), debugName);
            string outputSubfolderPath = Path.Combine(AppConfig.OutputFolder, dateStr);
            Directory.CreateDirectory(outputSubfolderPath);
            return outputSubfolderPath;
        }
    }
}

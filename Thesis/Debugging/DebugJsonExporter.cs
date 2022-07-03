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

            ExportAssignment("4 10 4 10 4h 10 10 8 1 2 8 14 1 1 2 1 e1.3 2 8 e6.2 5 6 5 6 e1.3 2 0h e1.3 e1.3 6 e6.2 16 1h 13 6 5 13 e1.3 6 5 15 e6.2 5h 13 3 3 15 3 15 3h 7 7 8 7 8 e0.0 8 12 2 e2.3 14 8 2 4 14 e0.0 e2.3 14 2 8 12 1 11 4 12 9 e6.0 4 2 e0.12 14 5 4 1 5 11 2 8h e6.0 e6.0 0 2 9h e2.3 11 0 5 e6.4 16 5 e6.0 15 e2.6 e6.4 e6.4 0 e2.6 15 e0.11 e0.11 e0.7 e0.7 e0.11 13 e0.7 8 13 6 e0.7 8 3 8 e3.2 e3.1 13 12 3 8 10 10 12 e3.2 3 3 12 10 8 8 2 4 10 2 13 13 e3.2 e3.1 e3.2 5 14 14 13h 12 9 e5.4 10 2 5 14 7 4 14 7 14 e5.4 e5.4 1 1 1 1 3 15 3 15 3 e5.5 15 3 e5.5 3 15 11 e3.3 e6.4 e5.3 10 15 15 11 15 e4.5 11 11 6 16 16 6 5 e3.3 13 9 e6.4 e5.5 e6.4 4 0 e1.3 11h 4 13 10 13 10 e1.3 16 10h 4 0 9 e6.4 13 6 5 16 4 0 16 0 4 12 14 12 14 12h 14 e0.4 e3.2 1 3 7 3 7 7 8 3 2 1 e3.2 11 8 7 7 10 7 2 8 1 8 1 13 15 8 1 13 10 11 e1.0 15 10 8 1 15 2 15 e3.5 16h 2 e3.5 e3.5 15 13 13 e5.3 15 e1.0 13 e5.4 e3.5 e6.5 e6.5 e5.4 5 5 12 12 e2.9 5 12 1 12 6 9 9 e2.9 4 9 6 1 6 7 4 12 e4.13h e0.9 1 7 7 9 e0.10 16 6 6 e2.9h 9 6 0 4 0 4 0 16 16 6 e6.2 e6.2 0 e0.10 16 14 7h 13 14 13 14 16 e6.2 14 13 13 e2.4 e3.6 e4.13 6 6 e2.9 10 2 7 10 2 e3.2 e1.3 0 15 10 6 2 12 12 3 3 12 0 8 3 10 e1.3 e3.2 12 12 15 e1.3 2 15 15 7 2 0 2 3 e1.3 8 11 8 11 11");
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

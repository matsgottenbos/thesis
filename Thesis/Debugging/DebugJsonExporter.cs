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

            ExportAssignment("17 e6.1 17 9 e6.1 17 17 3 20 5 e6.1 9 5 16 3 10 8 e0.7 3 7 0 4 0 18 21 5 9 3 e5.4 0 16 15 16 20 e5.4 e0.7 19 7 20h 7 10 19 21 7 4 16 19 4 18h 0h e6.4 7 7 e6.4 21 8h 7 e6.4 19 e6.4 2 e6.4 2 e2.1 2 e2.0 e2.1 e2.0 e3.3 13 e2.1 e3.3 e2.1 2 6 6 17 e1.1 6 17 e2.1 17 14 13 20 17 14 17 e3.3 13 20 14 15 1 e3.4 e1.1 20 14 11 17 17 8 3 21 12 8 17 16 12 8 1 16 6h 12 7 1 15 18 e3.4 8h 18 20 14 15 1 14 1 11 11 e3.4 21 14 18 1h e6.2 e6.2 16 18 7 21 12 7 18 3 0 e4.11 18 0 0 0 e6.2 19 2 e4.11 2 19 19 2 e4.11 2 e4.3h 2 19 13 19 6 13 5 6 8 6 10 5 4 13 8 6 11 e0.11 17 11 13 10 9 11 6 6 11 e2.4 6h 11 e2.4 15 8 e2.0 10h e2.0 5 3 4 5 12 e2.0 21 e2.4 3 11 15 3 9h 21 12 20 1 e2.0 3 12 21 21 1 e6.1 e6.1 0 20 0 2 2 2 0 e1.3 6 6 19 e4.3 19 e1.3 16 2 19 5 9 4 e1.3 19 4 e1.3 13 9 10 8 16 4 e1.3 8 6 13 13 13 8 e1.3 e5.3 8 16 19 e4.3 5 16 e3.6 e3.2 14 3 10 10 e4.3 18 14h e3.2 4 e3.2 9 3 15 18 e3.2 10 13 3 e3.6 e3.2 8 18 3 e5.3 3 15 7 7 2 7 2 6 6 6 17 12 16 12 7h 4 17 e3.1 6 6 20 e5.0 0 17 6h 9 14 e3.1 20 11 20 17 1 16 12 19 17 12 14h 11 11 20 11 11 4h 0 9 11h 1 0 9h 10 10 10 10 8 10 10 13 13 4 8 13 2 18 13 5 7 7 12 4 15 7 5 6 2 13h 14 11 7 15 18 3 0 7 7 21 18h 12 7h 0 17 17 2 4 5h 12h 0 17 19 15 19 0 14h 19 19 0 6 21 17 3 17 19 19 0 21 11h 9 1h 9 21h 19 9 9 16 18 13 13 12 5 18 13 5 11 11 5 14 7 14 14 11 18 10 13 20 5 18 14 14 7 7 1 7 21 20 14 21 1 21");
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

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
            Init("debug");

            ExportAssignment("e6.5 e2.1 e6.5 12 e6.5 17 17 e2.7 e5.2 e3.4 e2.1 19 0 11 e0.0 21 6 2 9 18 20 5 8 2 7 e3.4 14 e0.0 12 4 17 1 21 e5.2 9 0 5 8 18 20 11 19 2 20 6 21 5 6 14 13 7 4 4 7 10 2 20 1 8 1 e1.2 13 e1.2 10 e1.2 e0.4 10 e0.4 17 e0.13 15 17 15 e1.2 15 15 e1.0 e0.8 15 e1.0 e4.4 e4.11 15 e5.1 2 3 7 e1.0 17 e5.1 e0.13 7 11 0 19 e0.8 e0.13 15 8 e1.0 e1.0 2 18 9 1 2 e1.0 16 11 2 e4.15 16 3 11 e4.11 e4.15 14 e4.4 7 0 5 0 9 1 4 9 4 19 19 7 9 8 5 4 11 11 16 18 5 9 e4.17 5 4 16 14 e4.15 4 14 14 e4.17 5 e6.3 20 e0.11 20 e6.3 20 20 17 20 e0.11 10 e5.3 e6.3 20 19 e6.3 6 19 7 19 e5.3 6 15 3 7 2 11 21 0 12 17 e5.3 3 8 2 19 8 10 19 8 10 6 7 11 15 11 2 5 4 3 9 13 11 e2.8 21 4 12 0h 5 11 1 8 5 13 16 9 4 4 5 e0.5 16 1 e2.8 1 e0.5 e0.5 e0.5 20 e1.0 7 7 e5.3 7 e5.3 e1.0 2 0 e3.0 12 e1.0 7 20 17 14 20 18 20 6 10 2 15 20 21 e4.16 17 18 e5.3 21 e4.0 e2.8 4 3 12 0 12 e3.0 7 12 6 e4.11 8 18 15 15 6h 17 14 17 10 e4.16 e4.0 21 17 4 15 19 3 e4.11 e6.5 21 8 e2.8 8 e6.5 19 e3.2 e6.5 e3.2 5 14 14 14 20 15 13 10 11 6 15 e4.7 14 18 9 5 1 10 1 16 11 14h 9 15 9 10 21 11 20 e4.14 15 18 13 10 10 13 10 18 6 16 21 1 9 16 e2.0 16 16 2 2 e2.0 2 2 2 2 12 e6.2 2 4 19 2 7 10 10 12 e0.14 e2.3 10 13 10 8 14 13 e6.2 10 17 16 14 0 12 12 20 7 19 10 0 4 4 9 3 19 7 0 20 13 17 16 8 17 16 13 15 14 14 20 e2.3 20 16 16 8 14 0 15 3 15 20 9 15 15 e0.12 13 21 e3.3 e4.16 16 e3.3 12 16 3 3 16 13 e3.3 13 13 3 e3.3 12 21 18 16 e3.3 13 13 e0.12 e0.12 18 e2.0 16 5 3 5 e2.0 5");
        }

        void Init(string debugName) {
            string dateStr = string.Format("{0}-{1}", DateTime.Now.ToString("yyyy-MM-dd-HH-mm"), debugName);
            outputSubfolderPath = Path.Combine(AppConfig.OutputFolder, dateStr);
            Directory.CreateDirectory(outputSubfolderPath);
        }

        void ExportAssignment(string assignmentStr) {
            SaInfo info = ParseHelper.ParseAssignmentString(assignmentStr, instance);
            JsonAssignmentHelper.ExportAssignmentInfoJson(outputSubfolderPath, info);
        }
    }
}

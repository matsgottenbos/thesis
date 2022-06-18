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

            ExportAssignment("15 15 e0.14 e3.6 15 14 5 15 e6.5 23 10 e4.17 7 9h 23 20 e4.1 e5.1h 0 e3.6 17 0 e1.0 2h e5.3 23 5h e1.2 7 12 11 e4.16 16 20 e3.2 11 7 e5.3 e0.8 23 e4.13 e0.9 0 12 e1.2h e4.13 e2.1 16 1 7 20 e6.4 e6.2 3 e4.13 11 e6.4 e2.1 12 18 e5.5 21 e6.2 2 e1.3 e5.5 e6.1 e5.2 18h e5.5 17 e2.0 e4.1 22 17 e1.3h 17 15 e1.1 17 e6.1 22 9 e3.1 e3.5 2h e1.2 0 9 e3.2 e5.2h e3.3 e2.11 22 15 e1.1h 6 e6.1 5 8 0 20 e5.1 8 e5.3 5 e6.4 e2.6 9 14 13 e3.1 e3.6 12 e2.2h e3.4 13 e2.11 e4.0 13 e1.2 e5.3 20 e3.2 e5.0 5 e3.5 12 e6.0 9h e3.3 e4.4h e1.2h 23 20 e3.4 e5.1 20 e4.0 e5.4 12 e3.6 14h 23 e6.4 12 e0.2 e1.0 e5.4h e4.12 23 21 2 e5.2 18 e1.0 e4.3 18 18 e1.3 21 e4.16 6 4 7 e2.4 e4.5 e1.3 4 e4.12 10 e4.4 e2.2 10 8 e0.9 e1.1 6 e1.2 3 6 10 12 12 19 1 9 e0.3 4h e2.2 e6.4 e1.3h 13 0 12 e6.5 6 7h 8h 10h e1.2 19 e0.10 22 11 3 e6.2 e3.5 3h e1.2h e2.2 19 e1.0 e3.3 22h e1.0 0h e4.6 e0.12 e5.2 14 11 e5.4 e1.0 e5.2h e4.3 e3.5 e5.4 21 e5.3 1 15 21 e2.5 e4.3 15 17 3 1 5 4 e1.1 8 20 13 e5.3 e1.1 e5.5 16 0 20 4 15 18 5 e1.2 10 16 7 13 e2.5 e5.5 5 17h e1.3 0 22 8 e6.5 20 e2.10 e3.3h 5h 13 2 18h 7h 20 11 e5.4 10 e0.0 9 22 16h e4.14 11 e6.5h e5.4 22 14 e5.2 2 22h 1 e1.1 e3.6 1 e2.1 23 e4.4 10 e3.5 19 11 6 9 11 e0.2h e6.1 16 10 5 22 11 10 5 18 6 e3.3 e0.7 9 e2.4 e3.2h 7 e4.15 20 16 17 5h 7 e0.0 9 4 e2.4 e4.15 14 18 22 7 20 e4.13 0 7h e6.5 17h e1.1 e3.5 0 e3.5 0 e4.16 e1.3 e2.5 5 e1.1 11 e1.3 e3.1 e0.2 3 10 21 e5.4 e1.0 e1.3 e6.1 22 3 11 8 15 22 10 e1.0 3 23 e3.2 e5.5 6 e6.1 e2.4 e4.12 16 3 e1.3h 7 e0.5 e2.10h 22 23 15 21 e6.4 8 12 e6.3 7 21 6h e5.4 7 e1.0 16 17 21 21h 12 e4.11h 23 16 e0.7 e0.5 4 e2.3 4h e2.8 21 e5.0 18 1 e4.11 e1.2 8 e2.10 6 e1.3 e2.8 16 18 e2.3 1 1 e2.11 8 e1.3 20 6 2 18 12 e2.7 e6.5 18 20 2 e3.3 4 e0.13 4 e3.3 4");
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

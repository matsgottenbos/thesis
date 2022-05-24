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

            ExportAssignment("e0.5 9 e2.8 e1.4 e3.18 6 e1.4 e1.6 e3.17 9 e0.1 6 6 e1.6 1 14 e3.16 e3.18 3 e0.1 1 e2.8 9 3 3 e3.12 6 e1.2 3 14 6 2 e2.1 0 2 7 e2.0 14 e3.5 11 3 e3.9 1 e3.8 6 3 10 0 8 7 3h e2.3 7 11 2 8 10h 0 e3.19 7 e2.3 e4.1 e2.5 0 e3.0 0 5 e4.1 e0.2 5 13 5h 4 10 e2.4 4 e3.1 e3.14 e1.1 10 e3.13 13 e3.19 3 e1.4 4 13 2 e0.4h 2 12 e3.15 e2.0 e1.2 e1.5 e2.2 e1.1 e0.1 e2.5 10 e2.5h 6 e2.4 13h 6 12 e2.0 2h 12 4 e1.2h e2.6 3 e1.1 4h 3 6 12 e2.0h 3 6 e3.17 e0.2 e1.3h e3.11 9 e3.1 e4.5h e1.0 e3.13h 9 e3.2 2 e4.0 e2.7 e4.3 e3.18 5 11 e3.16 13 e0.5 11 1 4 e3.5 4 13 e3.7 e3.6 e3.12 1 1 e1.0 1 e1.5 e0.4 14 e1.0h e1.1 13 5 14 4 e3.16 11h 5 e0.3h 7 e0.6 e4.4 e4.3 e2.1h e4.1 e1.5 4 13 e2.8 5h e2.5 e4.3 14 0 e1.6 e2.0 13 e2.2h 1 6 1 6 e3.8h e0.5 e3.0 4 7 1 13h 14 7 e4.4h e1.4 0 e2.8h 8 e1.5h 14h e2.5h 6 e1.2h 8 7 e4.5h e3.4 e1.6 0 6 8 e0.0 e3.14h e3.4 e1.3 e3.3h e4.2 e4.2h 9 e2.3 8 9 e1.3h e0.0h 12 e3.5 e0.3 13 12 e3.13 e4.4 e2.3 13 12 13 9h e3.15 e3.7h 13 11 e3.5h 12 e3.6 e2.7h e2.5 12 5 e3.15 e0.2 e3.8h e2.2 13 e1.0h 12h e1.5 e4.2 11h e3.2 e0.2 10 e2.8 e2.1 e4.5 e0.7h e4.3h 10 e4.2 5 e2.6 0 10 e2.4 14 4 0 e1.2 e4.2h e3.9h e4.0 e2.6 0 e0.6 14 10 10 e3.10 5 e3.17 e0.6 4 6 0 10 3 e1.3 e3.3 14 6 e4.1h e1.2 e4.0 4 e3.4 0h 3 e1.4 4 8 e3.7 4 6 2 e3.16 2 e3.14 e1.0 e0.0h e3.18 7 8 e3.12 2 e3.5 e1.0 e0.3 9 6 3 2 1 3 e3.2h e3.8 1 9 12 e3.11 e4.3 e3.5 1 e4.4 12 11 7 1 9 e3.18 e3.6 e3.18h 7 e4.4 1 12 e4.3h 7 1 e0.1 e0.4 e2.7 e0.1 e1.6h 11 1 9 2 12h e2.6 7 e2.2h e0.7 e3.17 5 e4.4 e1.2 e3.17 5 1h 0 e0.5h e0.6 5 5 e1.1 11 e4.2 e4.2 e2.3 e3.19 e4.0 5h e0.6 e3.1h 0 e0.7 e2.8 e3.4h 0 e4.1 e3.12h e0.7 e3.9 e3.10h e3.0 e2.5 0h e2.8h e2.3 e4.1 e2.4 e4.1h 10 e0.0 e3.18h 12 e3.5 e3.13 2 8 e3.2 7 4 e2.4 e3.11 e1.6 e3.6 e3.6h e4.3 4 e1.0 10 4 7 e0.3 13 e2.0 10 e4.0 8 e4.3 e2.1 e1.1h 7 12 2 e0.5 2 e4.3 4 1 e3.14 8 13 e3.15 e1.6 12 4 e0.7 e3.11 e4.5h 5 8 13 e0.6 4 2 e2.1h 2 8 10h e3.12 e0.3h e3.1 e1.3 e0.2 12h 1 e4.0 e1.5 1 8h e3.11h e3.2 e4.4h e0.2 7 e3.0h 14 3 e1.6h 5 e3.16h e0.6 1 e2.2 e3.7h 5 1 13h e2.6 5 14 e1.3 e2.8 e0.1h 3h e3.8 5 e3.10h e3.18 e3.3h 14 e4.1 e0.4 e2.4h e2.3h e3.4 e3.8 e3.9h 0 e3.19 6 14 0 6 e3.6 14 e0.4h e4.1 0 6 10 e3.2 e4.5 e4.0 e2.7h e4.3 9 e3.15 e1.6 0 11 e3.17 e0.7 e3.1 e3.6 11 e1.5 6 e1.1 8 e4.4 11 9 9 e1.1 11 e1.6 e0.6 6 e3.15 1 e3.14 e3.2 e2.1 4 e0.3 8 10 9 e2.6 10 e0.2 13 12 1 e0.6 3 9 11 7 10 7 4 8 12 3 13 13 e1.1 e3.11 e1.4 1 e0.3 7 e2.3 11 e3.16 e1.3 14 4 e1.4 7 3 e0.4 12 13 14 e0.0 e1.0 7 13 e3.3 e3.7 e2.3 12 14 e3.19 e4.2 e3.13 e2.4 7 e1.4 e2.0 e3.10 e0.1 e3.0 14 e2.7 e0.7 e2.4 e0.5 e3.9 e4.2");
        }

        void Init(string debugName) {
            string dateStr = string.Format("{0}-{1}", DateTime.Now.ToString("yyyy-MM-dd-HH-mm"), debugName);
            outputSubfolderPath = Path.Combine(Config.OutputFolder, dateStr);
            Directory.CreateDirectory(outputSubfolderPath);
        }

        void ExportAssignment(string assignmentStr) {
            SaInfo info = ParseHelper.ParseAssignmentString(assignmentStr, instance);
            JsonHelper.ExportSolutionJson(outputSubfolderPath, info);
        }
    }
}

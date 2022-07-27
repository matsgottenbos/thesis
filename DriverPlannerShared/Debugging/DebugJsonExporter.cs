using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public class DebugJsonExporter {
        readonly Instance instance;
        string outputSubfolderPath;

        public DebugJsonExporter(Instance instance) {
            this.instance = instance;
            outputSubfolderPath = CreateOutputFolder("debug");

            //ExportAssignment("...");
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

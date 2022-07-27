using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverPlannerShared {
    public class DebugPastDataExporter {
        public DebugPastDataExporter(Instance instance) {
            SaInfo info = new SaInfo(instance);
            info.Assignment = instance.DataAssignment;
            info.IsHotelStayAfterActivity = new bool[instance.Activities.Length];
            info.ProcessDriverPaths(true);

            TotalCostCalculator.ProcessAssignmentCost(info);

            string outputSubfolderPath = DebugJsonExporter.CreateOutputFolder("pastData");
            JsonOutputHelper.ExportAssignmentInfoJson(outputSubfolderPath, info);
        }
    }
}

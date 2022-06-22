using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class DebugPastDataExporter {
        public DebugPastDataExporter(Instance instance) {
            SaInfo info = new SaInfo(instance);
            info.Assignment = instance.DataAssignment;
            info.IsHotelStayAfterTrip = new bool[instance.Trips.Length];
            info.ProcessDriverPaths(true);

            TotalCostCalculator.ProcessAssignmentCost(info);

            string outputSubfolderPath = DebugJsonExporter.CreateOutputFolder("pastData");
            JsonAssignmentHelper.ExportAssignmentInfoJson(outputSubfolderPath, info);
        }
    }
}

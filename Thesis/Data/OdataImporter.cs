using RailCube.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class OdataImporter {
        public static void Import() {
            string serviceRoot = "https://odata-v26.railcubecloud.com/odata";
            Default.Container context = new Default.Container(new Uri(serviceRoot));
            context.SendingRequest2 += SendAuthorization;

            DateTime dutyStartMin = DateTime.Today.AddDays(-7);
            DateTime dutyStartMax = DateTime.Today;

            Console.WriteLine("{0,-40} {1,-40} {2,-40} {3,-26} {4,-26}", "Duty name", "From location", "To location", "Start date", "End date");
            for (int i = 0; i < 176; i++) Console.Write("-");
            Console.WriteLine();

            IEnumerable<DutyModel> duties = context.Duties.Where(duty => duty.PlannedStart >= dutyStartMin && duty.PlannedStart <= dutyStartMax).OrderBy(duty => duty.PlannedStart);
            foreach (DutyModel duty in duties) {
                if (duty.FirstTransportOriginName == duty.LastTransportDestinationName) continue;
                Console.WriteLine("{0,-40} {1,-40} {2,-40} {3,-26} {4,-26}", duty.DutyNo, duty.FirstTransportOriginName, duty.LastTransportDestinationName, ParseDate(duty.PlannedStart), ParseDate(duty.PlannedEnd));
            }
        }

        static void SendAuthorization(object sender, Microsoft.OData.Client.SendingRequest2EventArgs e) {
            string username = "opsrsh01@rig";
            string password = "Bu@maN2099a";
            string authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", username, password)));
            e.RequestMessage.SetHeader("Authorization", "Basic " + authHeaderValue);
        }

        static string ParseDate(DateTimeOffset dateOffset) {
            return dateOffset.ToString("ddd dd/MM/yyyy HH:mm");
        }
    }
}

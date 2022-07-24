using Newtonsoft.Json.Linq;
using RailCube.WebApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class OdataImporter {
        public static void Import() {
            Default.Container context = ConnectToOdata();

            DateTime activityStartMin = DateTime.Today.AddDays(-378);
            DateTime activityStartMax = DateTime.Today.AddDays(-14);

            IEnumerable<DutyActivityModel> activities = context.DutyActivities.Where(activity => activity.PlannedStart >= activityStartMin && activity.PlannedStart <= activityStartMax).OrderBy(duty => duty.PlannedStart);
            foreach (DutyActivityModel activity in activities) {
                string name = activity.DutyNo;
                string description = activity.ActivityDescriptionEN;
                string startLocationCode = activity.OriginLocationCode;
                string endLocationCode = activity.DestinationLocationCode;

                // ...
            }

            throw new NotImplementedException();
        }

        public static Default.Container ConnectToOdata() {
            string serviceRoot = "https://odata-v29.railcubecloud.com/odata";
            Default.Container context = new Default.Container(new Uri(serviceRoot));
            context.SendingRequest2 += SendAuthorization;
            return context;
        }

        static void SendAuthorization(object sender, Microsoft.OData.Client.SendingRequest2EventArgs e) {
            string authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", DevConfig.OdataUsername, DevConfig.OdataPassword)));
            e.RequestMessage.SetHeader("Authorization", "Basic " + authHeaderValue);
        }

        static string ParseDate(DateTimeOffset dateOffset) {
            return dateOffset.ToString("ddd dd/MM/yyyy HH:mm");
        }
        static string ParseDate(DateTimeOffset? dateOffset) {
            return dateOffset.HasValue ? ParseDate(dateOffset.Value) : "-";
        }

        static string ParseTime(TimeSpan timeSpan) {
            if (timeSpan.TotalSeconds == 0) return "-";
            string prefix = timeSpan.TotalSeconds > 0 ? "" : "-";
            return prefix + timeSpan.ToString(@"hh\:mm");
        }
        static string ParseTime(TimeSpan? timeSpan) {
            return timeSpan.HasValue ? ParseTime(timeSpan.Value) : "-";
        }
    }
}

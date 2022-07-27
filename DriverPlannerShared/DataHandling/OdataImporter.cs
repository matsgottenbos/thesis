/*
 * Import data from the RailCube OData API
*/

using RailCube.WebApi.Models;
using System.Text;

namespace DriverPlannerShared {
    public static class OdataImporter {
        public static Instance Import() {
            Default.Container odataContext = ConnectToOdata();

            RawActivity[] rawActivities = ParseRawActivities(odataContext);
            return new Instance(rawActivities);
        }

        static RawActivity[] ParseRawActivities(Default.Container odataContext) {
            IEnumerable<DutyActivityModel> activities = odataContext.DutyActivities.Where(activity => activity.PlannedStart >= AppConfig.PlanningStartDate && activity.PlannedStart < AppConfig.PlanningEndDate).OrderBy(duty => duty.PlannedStart);

            List<RawActivity> rawActivities = new List<RawActivity>();
            foreach (DutyActivityModel activity in activities) {
                // Skip if non-included railway undertaking
                string railwayUndertaking = ParseHelper.CleanDataString(activity.RailwayUndertaking);
                if (railwayUndertaking == "" || !AppConfig.IncludedRailwayUndertakings.Contains(railwayUndertaking)) continue;

                // Get duty, activity and project name name
                string dutyName = ParseHelper.CleanDataString(activity.DutyNo);
                string activityType = ParseHelper.CleanDataString(activity.ActivityDescriptionEN);
                string dutyId = activity.DutyID?.ToString();
                string projectName = ParseHelper.CleanDataString(activity.Project ?? "");
                string trainNumber = ParseHelper.CleanDataString(activity.TrainNo ?? "");

                // Filter to configured activity descriptions
                if (!ParseHelper.DataStringInList(activityType, AppConfig.IncludedActivityTypes)) continue;

                // Get start and end stations
                string startStationDataName = ParseHelper.CleanDataString(activity.OriginLocationName);
                string endStationDataName = ParseHelper.CleanDataString(activity.DestinationLocationName);
                string startStationCountry = ParseHelper.CleanDataString(activity.OriginCountry);
                string endStationCountry = ParseHelper.CleanDataString(activity.DestinationCountry);
                if (startStationDataName == "" || endStationDataName == "" || startStationCountry == "" || endStationCountry == "") continue;

                // Get required country qualifications
                string[] requiredCountryQualifications;
                if (startStationCountry == endStationCountry) requiredCountryQualifications = new string[] { startStationCountry };
                else requiredCountryQualifications = new string[] { startStationCountry, endStationCountry };

                // Get start and end time
                DateTime? startTimeRaw = activity.PlannedStart.DateTime;
                if (startTimeRaw == null || startTimeRaw < AppConfig.PlanningStartDate || startTimeRaw > AppConfig.PlanningEndDate) continue; // Skip activities outside planning timeframe
                int startTime = (int)Math.Round((startTimeRaw - AppConfig.PlanningStartDate).Value.TotalMinutes);
                DateTime? endTimeRaw = activity.PlannedEnd.DateTime;
                if (endTimeRaw == null) continue; // Skip row if required values are empty
                int endTime = (int)Math.Round((endTimeRaw - AppConfig.PlanningStartDate).Value.TotalMinutes);

                // Skip activities longer than max shift length
                if (endTime - startTime > RulesConfig.MaxMainNightShiftLength) continue;

                // Get company and employee assigned in data
                string assignedCompanyName = ParseHelper.CleanDataString(activity.EmployeeWorksFor);
                string assignedEmployeeName = ParseHelper.CleanDataString(activity.EmployeeName);

                rawActivities.Add(new RawActivity(dutyName, activityType, dutyId, projectName, trainNumber, startStationDataName, endStationDataName, requiredCountryQualifications, startTime, endTime, assignedCompanyName, assignedEmployeeName));
            };

            return rawActivities.ToArray();
        }

        public static Default.Container ConnectToOdata() {
            string serviceRoot = "https://odata-v29.railcubecloud.com/odata";
            Default.Container context = new Default.Container(new Uri(serviceRoot));
            context.SendingRequest2 += SendAuthorization;
            return context;
        }

        static void SendAuthorization(object sender, Microsoft.OData.Client.SendingRequest2EventArgs e) {
            string authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", AppConfig.OdataUsername, AppConfig.OdataPassword)));
            e.RequestMessage.SetHeader("Authorization", "Basic " + authHeaderValue);
        }
    }
}

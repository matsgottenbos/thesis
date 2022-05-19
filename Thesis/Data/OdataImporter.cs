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
            string serviceRoot = "https://odata-v26.railcubecloud.com/odata";
            Default.Container context = new Default.Container(new Uri(serviceRoot));
            context.SendingRequest2 += SendAuthorization;

            DateTime activityStartMin = DateTime.Today.AddDays(-378);
            DateTime activityStartMax = DateTime.Today.AddDays(-14);

            List<ActivityDelayInfo> delayInfos = new List<ActivityDelayInfo>();

            IEnumerable<DutyActivityModel> activities = context.DutyActivities.Where(activity => activity.PlannedStart >= activityStartMin && activity.PlannedStart <= activityStartMax).OrderBy(duty => duty.PlannedStart);
            foreach (DutyActivityModel activity in activities) {
                string name = activity.DutyNo;
                string description = activity.ActivityDescriptionEN;
                string startLocationCode = activity.OriginLocationCode;
                string endLocationCode = activity.DestinationLocationCode;

                TimeSpan startDelaySpan = activity.ActualStart.HasValue ? activity.ActualStart.Value - activity.PlannedStart : new TimeSpan();
                int startDelay = (int)startDelaySpan.TotalMinutes;
                TimeSpan endDelaySpan = activity.ActualEnd.HasValue ? activity.ActualEnd.Value - activity.PlannedEnd : new TimeSpan();
                int endDelay = (int)endDelaySpan.TotalMinutes;
                int durationDelay = endDelay - startDelay;
                TimeSpan plannedDurationSpan = activity.PlannedEnd - activity.PlannedStart;
                int plannedDuration = (int)Math.Round(plannedDurationSpan.TotalMinutes / 30) * 30;

                int delayInfoIndex = delayInfos.FindIndex(delayInfo => delayInfo.Name == name && delayInfo.Description == description && delayInfo.StartLocationCode == startLocationCode && delayInfo.EndLocationCode == endLocationCode);
                if (delayInfoIndex == -1) {
                    ActivityDelayInfo delayInfo = new ActivityDelayInfo(name, description, startLocationCode, endLocationCode, plannedDuration);
                    delayInfo.AddDelays(startDelay, endDelay, durationDelay);
                    delayInfos.Add(delayInfo);
                } else {
                    ActivityDelayInfo delayInfo = delayInfos[delayInfoIndex];
                    delayInfo.AddDelays(startDelay, endDelay, durationDelay);
                }
            }

            delayInfos = delayInfos.Where(delayInfo => delayInfo.OccurrenceCount >= 5).ToList();

            delayInfos.Sort((a, b) => {
                if (a.Name == b.Name) return a.Description.CompareTo(b.Description);
                return a.Name.CompareTo(b.Name);
            });

            JObject jsonObj = new JObject();
            jsonObj["activities"] = new JArray(delayInfos.Select(delayInfo => delayInfo.ToJson()));

            string jsonStr = jsonObj.ToString();
            string fileName = Path.Combine(Config.OutputFolder, "delays.json");
            File.WriteAllText(fileName, jsonStr);
            Console.WriteLine("Export done");

            //string formatStr = "{0,-50} {1,-40} {2,-14} {3,-14} {4,-17} {5,-50}";
            //Console.WriteLine(formatStr, "Activity name", "Description", "From location", "To location", "Planned duration", "Duration delays");
            //for (int i = 0; i < 230; i++) Console.Write("-");
            //Console.WriteLine();

            //foreach (ActivityDelayInfo delayInfo in delayInfos) {
            //    string durationDelaysStr = ActivityDelayInfo.DelayValuesToString(delayInfo.RoundedDurationDelays);
            //    Console.WriteLine(formatStr, delayInfo.Name, delayInfo.Description, delayInfo.StartLocationCode, delayInfo.EndLocationCode, delayInfo.PlannedDuration, durationDelaysStr);
            //}



            //string formatStr = "{0,-50} {1,-40} {2,-14} {3,-14} {4,11} {5,11} {6,15}";
            //Console.WriteLine(formatStr, "Activity name", "Description", "From location", "To location", "Start delay", "End delay", "Duration delay");
            //for (int i = 0; i < 230; i++) Console.Write("-");
            //Console.WriteLine();

            //IEnumerable<DutyActivityModel> activities = context.DutyActivities.Where(activity => activity.PlannedStart >= activityStartMin && activity.PlannedStart <= activityStartMax).OrderBy(duty => duty.PlannedStart);
            //foreach (DutyActivityModel activity in activities) {
            //    TimeSpan startDelay = activity.ActualStart.HasValue ? activity.ActualStart.Value - activity.PlannedStart : new TimeSpan();
            //    TimeSpan endDelay = activity.ActualEnd.HasValue ? activity.ActualEnd.Value - activity.PlannedEnd : new TimeSpan();
            //    TimeSpan durationDelay = endDelay - startDelay;
            //    Console.WriteLine(formatStr, activity.DutyNo, activity.ActivityDescriptionEN, activity.OriginLocationCode, activity.DestinationLocationCode, ParseTime(startDelay), ParseTime(endDelay), ParseTime(durationDelay));
            //}

            Console.ReadLine();
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

    class ActivityDelayInfo {
        public string Name, Description, StartLocationCode, EndLocationCode;
        public int PlannedDuration, OccurrenceCount;
        public List<int> StartDelays, EndDelays, DurationDelays;
        public List<DelayValue> RoundedStartDelays, RoundedEndDelays, RoundedDurationDelays;

        public ActivityDelayInfo(string name, string description, string startLocationCode, string endLocationCode, int plannedDuration) {
            Name = name;
            Description = description.Length <= 40 ? description : description.Substring(0, 37) + "...";
            StartLocationCode = startLocationCode;
            EndLocationCode = endLocationCode;
            StartDelays = new List<int>();
            EndDelays = new List<int>();
            DurationDelays = new List<int>();
            RoundedStartDelays = new List<DelayValue>();
            RoundedEndDelays = new List<DelayValue>();
            RoundedDurationDelays = new List<DelayValue>();
            PlannedDuration = plannedDuration;
        }

        public void AddDelays(int startDelay, int endDelay, int durationDelay) {
            OccurrenceCount++;
            AddSpecificDelay(startDelay, StartDelays, RoundedStartDelays);
            AddSpecificDelay(endDelay, EndDelays, RoundedEndDelays);
            AddSpecificDelay(durationDelay, DurationDelays, RoundedDurationDelays);
        }

        static void AddSpecificDelay(int delay, List<int> delayValues, List<DelayValue> roundedDelayValues) {
            delayValues.Add(delay);
            delayValues.Sort();

            int roundedDelay = (int)Math.Round((float)delay / 30) * 30;

            int roundedDelayValuesIndex = roundedDelayValues.FindIndex(delayValue => delayValue.Delay == roundedDelay);
            if (roundedDelayValuesIndex == -1) {
                roundedDelayValues.Add(new DelayValue(roundedDelay, 1));
            } else {
                roundedDelayValues[roundedDelayValuesIndex].Count++;
            }
            roundedDelayValues.Sort((a, b) => a.Delay.CompareTo(b.Delay));
        }

        public static string DelayValuesToString(List<DelayValue> delayValues) {
            return string.Join(", ", delayValues);
        }

        public JObject ToJson() {
            JObject jObject = new JObject();
            jObject["name"] = Name;
            jObject["description"] = Description;
            jObject["startLocationCode"] = StartLocationCode;
            jObject["endLocationCode"] = EndLocationCode;
            jObject["plannedDuration"] = PlannedDuration;
            jObject["occurrenceCount"] = OccurrenceCount;
            jObject["startDelays"] = new JArray(StartDelays);
            jObject["endDelays"] = new JArray(EndDelays);
            jObject["durationDelays"] = new JArray(DurationDelays);
            return jObject;
        }
    }

    class DelayValue {
        public int Delay, Count;

        public DelayValue(int delay, int count) {
            Delay = delay;
            Count = count;
        }

        public override string ToString() {
            if (Count == 1) return Delay.ToString();
            return string.Format("{0}x{1}", Count, Delay);
        }
    }
}

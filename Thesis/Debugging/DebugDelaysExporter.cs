using Newtonsoft.Json.Linq;
using RailCube.WebApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class DebugDelaysExporter {
        public static void Run() {
            Default.Container context = OdataImporter.ConnectToOdata();

            DateTime activityStartMin = DateTime.Today.AddDays(-378);
            DateTime activityStartMax = DateTime.Today.AddDays(-14);

            List<ActivityDelayInfo> delayInfos = new List<ActivityDelayInfo>();

            IEnumerable<DutyActivityModel> activities = context.DutyActivities.Where(activity => activity.PlannedStart >= activityStartMin && activity.PlannedStart <= activityStartMax).OrderBy(duty => duty.PlannedStart);
            foreach (DutyActivityModel activity in activities) {
                // Include only the selected railway undertakings
                if (!DataConfig.ExcelIncludedRailwayUndertakings.Contains(activity.RailwayUndertaking)) continue;

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

            delayInfos.Sort((a, b) => {
                if (a.Name == b.Name) return a.Description.CompareTo(b.Description);
                return a.Name.CompareTo(b.Name);
            });

            JObject jsonObj = new JObject();
            jsonObj["activities"] = new JArray(delayInfos.Select(delayInfo => delayInfo.ToJson()));

            string jsonStr = jsonObj.ToString();
            string fileName = Path.Combine(AppConfig.OutputFolder, "delays.json");
            File.WriteAllText(fileName, jsonStr);
            Console.WriteLine("Export done");
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

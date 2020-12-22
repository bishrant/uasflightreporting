using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Survey123EmailNotification.Helpers
{
    public class FlightPurposes
    {
        public string wildfire = "wildfire suppression";
        public string pilotTraining = "pilot training";
        public string polotTraining = "pilot training or proficiency";
        public string emergencyResponse = "emergency response";
        public string educationalVideo = "educational video";
        public string forestHealth = "forest health evaluation";
        public string forestOperations = "forest operations evaluation";
        public string treeAssessment = "individual tree assessment/data collection";
        public string lawEnforcement = "law enforcement activities";
        public string maintenance = "maintenance test flight";
        public string prescribedBurning = "prescribed burning";
        public string promotionalVideo = "promotional video";
        public string reforestation = "reforestation surveys";
        public string trainingCourses = "training courses (TFS led class training)";
        public string vegetationDelineation = "vegetation delineation/identification";
        public string other = "other";

        public string getFlightPurpose(dynamic feature)
        {
            FlightPurposes flightPurposes = new FlightPurposes();
            string flightPurposeJson = JsonConvert.SerializeObject(flightPurposes);
            var fPurpose = Convert.ToString(feature.attributes["flightPurpose"]);
            var flightPurpose = JObject.Parse(flightPurposeJson)[fPurpose].ToString();
            
            if (Convert.ToString(fPurpose) == "other")
            {
                flightPurpose += " (" + Convert.ToString(feature.attributes["flightPurposeOthers"]) + ")";
            }
            if (Convert.ToString(fPurpose) == "wildfire")
            {
                flightPurpose += " (" + Convert.ToString(feature.attributes["wildfireName"]) + ")";
            }
            if (Convert.ToString(fPurpose) == "lawEnforcement")
            {
                flightPurpose += " (" + Convert.ToString(feature.attributes["lawFlight"]) + ")";

            }
            return flightPurpose;
        }

    }

}

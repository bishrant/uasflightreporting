using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using Survey123EmailNotification.Helpers;

namespace Survey123EmailNotification.Controllers
{
    [Route("api/arcgis")]
    [ApiController]
    public class ArcGISController : Controller
    {
        HttpClient client = new HttpClient();
        IdObfuscator obfuscator = new IdObfuscator();
        AGOLFeature agol = new AGOLFeature();
        public ArcGISController()  { }

        public string config(string name) {
            return AppSettings.Configuration.GetSection(name).Value;
        }

        string arcGISUrl = AppSettings.Configuration.GetSection("ArcGISURL").Value;
        string featureService = AppSettings.Configuration.GetSection("featureService").Value;

        /* get details about specified feature id */
        [HttpGet("{encyptedFeatureId}")]
        public async Task<Dictionary<string, string>> details(string encyptedFeatureId) {
            int featureId = obfuscator.decryptId(encyptedFeatureId);
            
            var token = await agol.GetToken(arcGISUrl);
            var response = await client.GetAsync(featureService + "0/query?objectids=" + featureId + "&f=json&&outFields=*&token=" + token);
            var jsonString = await response.Content.ReadAsStringAsync();

            dynamic f = JsonConvert.DeserializeObject<object>(jsonString);
            var dateUtils = new DateUtils();
            var totalFeatures = Convert.ToInt32(f.features.Count);
            string msg;
            string email;
            
            if (totalFeatures > 0) {
                var date = dateUtils.GetDateFromUnix(f.features[0].attributes.missionDate);
                email = Convert.ToString(f.features[0].attributes.email);
                msg = "RPIC Name: " + f.features[0].attributes.pilotName + "<br>" +
                "Flight Date: " + date.ToString("MM/dd/yyyy") + "<br>";
            } else {
                msg = "";
                email = "";
            }

            return new Dictionary<string, string>() {
                {"email", email},
                {"featureDetails", msg}
            };
        }


        public async Task<Dictionary<string, string>> GetFeatureDetails(int featureId, string token)
        {
           
            var response = await client.GetAsync(featureService + "0/query?objectids=" + featureId + "&f=json&&outFields=*&token=" + token);
            var jsonString = await response.Content.ReadAsStringAsync();

            dynamic f = JsonConvert.DeserializeObject<object>(jsonString);
            var dateUtils = new DateUtils();
            var totalFeatures = Convert.ToInt32(f.features.Count);

            if (totalFeatures > 0)
            {
                var date = dateUtils.GetDateFromUnix(f.features[0].attributes.missionDate);
                Dictionary<string, string> output = new Dictionary<string, string>()
                {
                    { "pilotName", Convert.ToString(f.features[0].attributes.pilotName)},
                    { "missionDate", date.ToString("MM/dd/yyyy")}
                };
                return output;
            } else {
                return null;
            }
        }

        [HttpPost()]
        public async Task<Dictionary<string, bool>> saveCommitteeDecision([FromBody] DecisionPost content)
        {

            var agol = new AGOLFeature();
            var token = await agol.GetToken(arcGISUrl);
            var agolFeature = new AGOLFeature();
            int featureId = obfuscator.decryptId(content.featureId);
            var updateJSON = agolFeature.createUpdateJSONCommittee(featureId, content.missionNumber, content.committeeDecision, content.committeeMemberName, content.committeeRemarks);
            var updateJson = agolFeature.createUpdateCommitteeJSON(token, updateJSON);

            var response = await client.PostAsync(featureService + "0/applyEdits", updateJson);
            var jsonString = await response.Content.ReadAsStringAsync();
            dynamic updateResponse = JsonConvert.DeserializeObject<object>(jsonString);
            var success = Convert.ToBoolean(updateResponse.updateResults[0].success);
            if (success) {
                var featureDetails = await GetFeatureDetails(featureId, token);

                var smtp = new SmtpEmailClass();
                var msgSubject = "UAS flight authorization request " + content.committeeDecision;
                var msgBody = featureDetails["pilotName"] + ":<br><br>";
                msgBody += "Your UAS flight for " + featureDetails["missionDate"] +" has been " + content.committeeDecision + " by " + content.committeeMemberName +".<br>";
                if (content.committeeDecision == "approved") {
                    msgBody += "<br>Mission number " + content.missionNumber + " has been assigned for this UAS flight." + "<br>";
                }
                if (content.committeeRemarks != "") {
                    msgBody += "<br>Additional remarks from the committee: " + content.committeeRemarks +"<br>";
                }
                msgBody += "<br><br>Thank you,<br>UAS Committee<br>Texas A&M Forest Service";
                success = await smtp.SendEmailWithDecision(config("CommitteeEmail"), content.email, msgSubject, msgBody);
                Log.Information("Committee decision sent " + Convert.ToString(config("CommitteeEmail")) + Convert.ToString(content.email) + Convert.ToString(msgSubject) + Convert.ToString(msgBody));
                Log.Information(" Committee decision sent success " + Convert.ToString(success));
            }
            var res = new Dictionary<string, bool> {
                { "success", success }
            };
            return  res;
            }

        public class DecisionPost  {
            public string featureId { get; set; }
            public string missionNumber { get; set; }
            public string committeeDecision { get; set; }
            public string committeeMemberName { get; set; }
            public string committeeRemarks { get; set; }
            public string email { get; set; }
        }
    }
}
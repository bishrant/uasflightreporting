using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Survey123EmailNotification.Helpers
{
    public class AGOLFeature
    {
        public class UpdateDecision
        {
            public UpdateDecisionAttributes attributes { get; set; }
        }
        public class UpdateDecisionAttributes
        {
            public int objectid { get; set; }
            public string missionNumber { get; set; }
            public string committeeMemberName { get; set; }
            public string committeeDecision { get; set; }
            public string committeeRemarks { get; set; }
        }

        public UpdateDecision createUpdateJSONCommittee(int featureId, string missionNumber, string committeeDecision, string committeeMemberName, string committeeRemarks)
        {
            var upDecision = new UpdateDecision();
            var u = new UpdateDecisionAttributes();
            u.objectid = featureId;
            u.missionNumber = missionNumber;
            u.committeeMemberName = committeeMemberName;
            u.committeeDecision = committeeDecision;
            u.committeeRemarks = committeeRemarks;
            upDecision.attributes = u;
            return upDecision;
        }

        public async Task<object> GetRelatedFeatures(int featureId, int tableIndex, string targetField)
        {
            var token = await GetToken(AppSettings.Configuration.GetSection("ArcGISURL").Value);
            HttpClient client = new HttpClient();
            string queryURL = AppSettings.Configuration.GetSection("featureService").Value + "0/queryRelatedRecords?objectIds="+ featureId + 
                "&relationshipId="+ tableIndex+ 
                "&outFields="+ targetField+ "&f=json&token=" + token;
            var response = await client.GetAsync(queryURL);
            var jsonString = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<object>(jsonString);
        }

        public async Task<object> GetFeatureAttributes(int featureId)
        {
            HttpClient client = new HttpClient();
            
            var token = await GetToken(AppSettings.Configuration.GetSection("ArcGISURL").Value);
            var response = await client.GetAsync(AppSettings.Configuration.GetSection("featureService").Value + "0/query?objectids=" + featureId + "&f=json&&outFields=*&token=" + token);
            var jsonString = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<object>(jsonString);


        }
        public FormUrlEncodedContent createUpdateCommitteeJSON(string token, UpdateDecision updateContent)
        {
            return new FormUrlEncodedContent(new[]
             {
                new KeyValuePair<string, string>("f", "json"),
                new KeyValuePair<string, string>("updates", JsonConvert.SerializeObject(updateContent)),
                new KeyValuePair<string, string>("token", token)
            });
        }

        public async Task<string> GetToken(string arcGISUrl)
        {

            string tokenString = "";
            string jsonString = "";
            HttpClient client = new HttpClient();

            var tokenForm = new Dictionary<string, string>
            {
                {"f", "json" },
                {"client_id", AppSettings.Configuration.GetSection("client_id").Value },
                {"client_secret", AppSettings.Configuration.GetSection("client_secret").Value },
                {"grant_type", AppSettings.Configuration.GetSection("grant_type").Value }
            };

            var tokenContent = new FormUrlEncodedContent(tokenForm);
            var tokenResponse = await client.PostAsync(arcGISUrl, tokenContent);
            dynamic tokenObj = null;
            if (tokenResponse != null)
            {
                jsonString = await tokenResponse.Content.ReadAsStringAsync();
                tokenObj = JsonConvert.DeserializeObject<object>(jsonString);
                if (tokenObj != null)
                {
                    tokenString = tokenObj.access_token;
                }
            }
            if (tokenString == "" || tokenString == null)
            {
                var smtp = new SmtpEmailClass();
                await smtp.SendEmail(AppSettings.Configuration.GetSection("CommitteeEmail").Value, AppSettings.Configuration.GetSection("DebugEmail").Value, "AGOL Token Issue, UAS", jsonString);
            }
            
            return tokenString;

        }

        public async Task<bool> hasMissionNumber(int featureId, string arcGISUrl, string featureService)
        {
            var token = await GetToken(arcGISUrl);
            HttpClient client = new HttpClient();
            var url = featureService + "0/query?where=%28missionNumber+is+not+null%29+and+%28missionNumber+not+like+%27%27%29&objectIds=" + featureId + "&returnCountOnly=true&f=json&token=" + token;

            var response = await client.GetAsync(url);
            var jsonString = await response.Content.ReadAsStringAsync();
            dynamic counts = JsonConvert.DeserializeObject<object>(jsonString);
            return counts.count > 0;
        }

        public async Task<DateTime?> getCreationDate(string featureService, int featureId, string token)
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(featureService + "0/query?objectids=" + featureId + "&f=json&&outFields=CreationDate&token=" + token);
            var jsonString = await response.Content.ReadAsStringAsync();
            dynamic f = JsonConvert.DeserializeObject<object>(jsonString);
            if (f.features.Count > 0)
            {
                var dateUtils = new DateUtils();
                var date = dateUtils.GetDateFromUnix(f.features[0].attributes.CreationDate);
                return date;
            } else
            {
                return null;
            }
            
        }

    }
}

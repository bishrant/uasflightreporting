using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Survey123EmailNotification.Helpers
{
    public class OfflineFeatureAttachments
    {
        public async Task<string> GetAttachment(int featureId, string keywords)
        {
            var agol = new AGOLFeature();
            var arcgisURL = AppSettings.Configuration.GetSection("ArcGISURL").Value;
            var featureURL = AppSettings.Configuration.GetSection("featureService").Value;
            var token = await agol.GetToken(arcgisURL);
            HttpClient client = new HttpClient();

            // need to query which attachment it has

            var response1 = await client.GetAsync(featureURL+ "0/queryAttachments?objectIds=" + featureId + "&f=json&keywords="+keywords+"&outFields=*&token=" + token);
            var jsonString = await response1.Content.ReadAsStringAsync();

            dynamic jsonDes = JsonConvert.DeserializeObject<object>(jsonString);

            var attachmentId = Convert.ToInt32(jsonDes.attachmentGroups[0].attachmentInfos[0].id);

            
            string furl = featureURL +"0/" + featureId + "/attachments/" + attachmentId + "?";

            furl += "token=" + Convert.ToString(token);
            return furl;
        }

        public async Task<string> GetLandOwnerDocumentAsync(dynamic content) {
            var agol = new AGOLFeature();
            var arcgisURL = AppSettings.Configuration.GetSection("ArcGISURL").Value;
            var token = await agol.GetToken(arcgisURL);
            HttpClient client = new HttpClient();
            dynamic response = null;
            foreach (dynamic r in content.response) {
                if (Convert.ToInt32(r.id) == 0) {
                    response = r;
                    break;
                }
            }
            if (response == null) { return null; }
            var ff = response.addResults == null ? response.updateResults : response.addResults;
            var featureId = Convert.ToString(ff[0].objectId);
            // need to query which attachment it has

            var response1 = await client.GetAsync(AppSettings.Configuration.GetSection("featureService").Value + "0/queryAttachments?objectIds=" + featureId + "&f=json&keywords=landOwnerPermission&outFields=*&token=" + token);
            var jsonString = await response1.Content.ReadAsStringAsync();

            var jsonDes = JsonConvert.DeserializeObject<object>(jsonString);

            var attachmentId = Convert.ToInt32(jsonDes.attachmentGroups[0].attachmentInfos[0].id);

            string furl = Convert.ToString(content.surveyInfo.serviceUrl) + "/";
            furl += "0/" + featureId + "/attachments/" + attachmentId + "?";

            furl += "token=" + Convert.ToString(token);
            return furl;
        }
    }
}

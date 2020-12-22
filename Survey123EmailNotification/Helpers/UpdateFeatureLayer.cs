using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Survey123EmailNotification.Helpers;

namespace Survey123EmailNotification.Helpers
{
    public class UpdateFeatureLayer
    {
        public async Task<bool> updatePDFLinkFeatureLayer(string fieldName, string filename, dynamic content)
        {
            var agol = new AGOLFeature();
            string token = await agol.GetToken(AppSettings.Configuration.GetSection("ArcGISURL").Value);
            bool updateSuccess = false;
            HttpClient client = new HttpClient();
            var s = new StringUtils();
            dynamic updates = null;
            if (fieldName == "pdfApplicationLink")
            {
                updates = s.updatePDFLink(Convert.ToInt32(content.feature.result["objectId"]), filename, Convert.ToString(content.userInfo.email));
            }
            else
            {
                updates = s.updatePostFlight(Convert.ToInt32(content.feature.result["objectId"]), filename);
            }

            var updateJson = s.createUpdateAttributeJSON(token, updates);
            string serverUrls = Convert.ToString(Convert.ToString(content.surveyInfo.serviceUrl) + "/0/applyEdits");

            var response = await client.PostAsync(serverUrls, updateJson);
            var jsonString = await response.Content.ReadAsStringAsync();
            var jgj = JsonConvert.DeserializeObject<object>(jsonString);

            return updateSuccess;
        }
    }
}

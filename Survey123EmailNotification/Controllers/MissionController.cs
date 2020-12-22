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
    [Route("api/arcgis/hasMissionNumber")]
    [ApiController]
    public class MissionController : ControllerBase
    {
        string arcGISUrl = AppSettings.Configuration.GetSection("ArcGISURL").Value;
        string featureService = AppSettings.Configuration.GetSection("featureService").Value;
        IdObfuscator obfuscator = new IdObfuscator();

        [HttpGet("{encyptedFeatureId}")]
        public async Task<bool> checkForMissionNumber(string encyptedFeatureId)
        {
            int featureId = obfuscator.decryptId(encyptedFeatureId);
            var agol = new AGOLFeature();
            Log.Information(" obfuscated mission number " + Convert.ToString(encyptedFeatureId));
            Log.Information(" parsed mission number " + Convert.ToString(featureId));
            return await agol.hasMissionNumber(featureId, arcGISUrl, featureService);
        }
    }

    [Route("api/arcgis/getMissionNumber")]
    [ApiController]
    public class MissionNumberController : ControllerBase
    {
        string arcGISUrl = AppSettings.Configuration.GetSection("ArcGISURL").Value;
        string featureService = AppSettings.Configuration.GetSection("featureService").Value;
        IdObfuscator obfuscator = new IdObfuscator();
       
        [HttpGet("{encyptedFeatureId}")]
               public async Task<Dictionary<string, string>> getNextMissionNumber(string encyptedFeatureId)
               {
                   int featureId = obfuscator.decryptId(encyptedFeatureId);
                   string missionNumber = "";
                   HttpClient client = new HttpClient();
                   var agol = new AGOLFeature();
                   var token = await agol.GetToken(arcGISUrl);
                   var hasMissionNo = await agol.hasMissionNumber(featureId, arcGISUrl, featureService);
                   var creationDate = await agol.getCreationDate(featureService, featureId, token);
                   if (!hasMissionNo && creationDate != null) {
                       string signatureWhere = "creationDate between date '" + creationDate?.ToString("MM/dd/yyyy 00:00:00") + "' and date '" +
                           creationDate?.AddDays(1).ToString("MM/dd/yyyy 00:00:00") + "'";
                       string queryURL = featureService + "0/query?where=" + signatureWhere
                           + "&returnGeometry=false&outFields=objectid,creationDate,pilotName,missionDate,committeeDecision,missionNumber&sqlFormat=standard&orderBy=CreationDate asc&f=json&token=" + token;

                    Log.Information(" mission number query url " + Convert.ToString(queryURL));
                var response = await client.GetAsync(queryURL);
                       var jsonString = await response.Content.ReadAsStringAsync();
                       dynamic json = JsonConvert.DeserializeObject<object>(jsonString);
                       dynamic features = json.features;

                       int counter = 1;
                       if (features.Count > 1) {
                           foreach (dynamic feature in features) {
                               var ff = feature.attributes;
                               var fg = ff.missioNumber;
                                   if (feature.attributes.missionNumber != null && ff.missionNumber != "") {
                                       var mm = feature.attributes.committeeDecision;
                                       counter = counter + 1;
                                        var x = counter.ToString("00");
                                       if (feature.attributes.objectid == featureId) {
                                           break;
                                       }
                               }
                            }
                    missionNumber = creationDate?.ToString("yyyyMMdd") + (counter).ToString("00");
                } else if (features.Count == 1) {
                    missionNumber = creationDate?.ToString("yyyyMMdd") + "01";
                }
                
                else {
                    missionNumber = null;
                }
            } else {
                missionNumber = null;
            }
            Log.Information(" final calculated mission number " + Convert.ToString(missionNumber));
            return new Dictionary<string, string>
            {
                {"missionNumber", missionNumber }
            };
        }
    }

}
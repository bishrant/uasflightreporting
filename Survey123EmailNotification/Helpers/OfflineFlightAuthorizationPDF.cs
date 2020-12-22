using DocumentFormat.OpenXml.Packaging;
using Microsoft.Office.Interop.Word;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Survey123EmailNotification.Helpers
{
    public class OfflineFlightAuthorizationPDF
    {

        public async Task<string> OfflineCreateFlightPDF(int featureId)
        {
            var agolFeature = new AGOLFeature();
            dynamic attributes = await agolFeature.GetFeatureAttributes(featureId);
            string directory = Directory.GetCurrentDirectory();
            var offlineFeatureAttachments = new OfflineFeatureAttachments();
            var signatureUrl = await offlineFeatureAttachments.GetAttachment(featureId, "signature");

            var setting = new OpenSettings();
            setting.AutoSave = false;

            var dateUtils = new DateUtils();
            var stringUtils = new StringUtils();
            var missionDate = dateUtils.GetDateFromUnix(attributes.missionDate);
            var signatureDate = dateUtils.GetDateFromUnix(attributes.signatureDate);

            var dispatchNotifiedDate = dateUtils.GetDateFromUnix(attributes.dateDispatchNotified);
            string tempFileName = directory + "\\wwwroot\\templates\\UAS Flight Authorization Application_" + Convert.ToString(attributes["pilotName"]) + "_" + missionDate.Date.ToString("MM-dd-yyyy") + "_" + DateTime.Now.ToString("HHmmss") + ".doc";
            string templateFileName = directory + "\\wwwroot\\templates\\uas.docx";
            try  {
                File.Copy(templateFileName, tempFileName);
            } catch (Exception e) { Console.WriteLine(e); }

            FlightPurposes flightPurposes = new FlightPurposes();

            string flightPurpose = flightPurposes.getFlightPurpose(attributes.features[0]);
            string missionLocation = stringUtils.GetMissionLocation(attributes.features[0].geometry);
             using (WordprocessingDocument doc = WordprocessingDocument.Open(tempFileName, true)) {
                 string docText = null;
                 using (StreamReader sr = new StreamReader(doc.MainDocumentPart.GetStream())) {
                     docText = sr.ReadToEnd();
                 }
                dynamic att = attributes.features[0].attributes;
                string listOfAdditionalRPIC = await GetAdditionalRPICsAsync(att.globalid);
             };
            return tempFileName;
        }

        public async Task<dynamic> GetAdditionalRPICsAsync(string featureGlobalID) {
            /*parentglobalid like '5555035b-b2f9-405b-b9f5-fae84738bbdf'*/
            var agolFeature = new AGOLFeature();
            HttpClient client = new HttpClient();
            var featureService = AppSettings.Configuration.GetSection("featureService").Value;
            var token = agolFeature.GetToken(AppSettings.Configuration.GetSection("ArcGISURL").Value);
            var response = await client.GetAsync(featureService+ "1/query?where=parentglobalid like '" + featureGlobalID + "'&f=json&&outFields=*&token=" + token);
            dynamic jsonString = await response.Content.ReadAsStringAsync();
            if (jsonString.features.length < 0)
                return "";
            else {
                var names = new List<String>();
                foreach( dynamic f in jsonString.features) {
                    names.Add(f.attributes.additionalRPICNames);
                }
                return String.Join(", ", names.ToArray());
            }
        }
    }
}

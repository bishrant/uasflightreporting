using DocumentFormat.OpenXml.Packaging;
using Microsoft.Office.Interop.Word;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Survey123EmailNotification.Models;

namespace Survey123EmailNotification.Helpers
{
    public class PostFlightReport
    {
        public string config(string name)  {
            var t = AppSettings.Configuration.GetSection(name).Value;
            return AppSettings.Configuration.GetSection(name).Value;
        }

        public async Task<SaveFileResult> CreatePostFlightReport(dynamic content, string serverAddress)
        {
            var returnResponse = new SaveFileResult();
            string directory = Directory.GetCurrentDirectory();
            var setting = new OpenSettings();
            setting.AutoSave = false;
            var dateUtils = new DateUtils();
            var stringUtils = new StringUtils();

            var attributes = content.feature.attributes;
            var missionDate = dateUtils.GetDateFromUnix(attributes.missionDate);
            var signatureDate = dateUtils.GetDateFromUnix(attributes.signatureDate);
            var dispatchNotifiedDate = dateUtils.GetDateFromUnix(attributes.dateDispatchNotified);

            string fName = "UAS Post Flight_" + stringUtils.cleanTxt(attributes["pilotName"]) + "_" + 
                missionDate.Date.ToString("MM - dd - yyyy") + "_" + DateTime.Now.ToString("HHmmss") + ".docx";
            string tempFileName = directory + "\\wwwroot\\templates\\" + stringUtils.RemoveFilenameInvalidChars(fName);
            string templateFileName = directory + "\\wwwroot\\templates\\uas_postflight.docx";
            try {
                System.IO.File.Copy(templateFileName, tempFileName);
            }catch (Exception e) {
                Console.WriteLine(e);
            }
            using (WordprocessingDocument doc = WordprocessingDocument.Open(tempFileName, true))
            {
                string docText = null;
                var agol = new AGOLFeature();
                var oo = attributes.objectid == null;
                var objectid = attributes.objectid == null ? content.feature.result.objectId : attributes.objectid;
                var att = await agol.GetFeatureAttributes(Convert.ToInt32(objectid));
                if (att.features.Count > 0)
                {
                    var feat = att.features[0].attributes;
                    using (StreamReader sr = new StreamReader(doc.MainDocumentPart.GetStream()))
                    {
                        docText = sr.ReadToEnd();
                    }
                    int batteryTableRelationshipID = Convert.ToInt32(AppSettings.Configuration.GetSection("batteryTableRelationshipID").Value);
                    var flightTimePerBatteryJSON = await agol.GetRelatedFeatures(Convert.ToInt32(objectid), batteryTableRelationshipID, "flightTimeForBattery,batteryIdentifier");
                    string flightTimerPerBattery = "";
                    string numberOfBattery = "";
                    if (flightTimePerBatteryJSON.relatedRecordGroups.Count > 0)
                    {
                        List<string> b = new List<string>();
                        var batteries = flightTimePerBatteryJSON.relatedRecordGroups[0].relatedRecords;
                        foreach (dynamic battery in batteries)  {
                            var a = battery.attributes;
                            string __id = a.batteryIdentifier == null ? "" : "#"+a.batteryIdentifier + ": ";
                            string _t = __id + Convert.ToString(a.flightTimeForbattery) + " min";
                            b.Add(_t);
                        }
                        flightTimerPerBattery = String.Join(", ", b.ToArray());
                        numberOfBattery = Convert.ToString(batteries.Count);
                    }
                    var d = new DateUtils();
                    DateTime postFlightDate = d.GetDateFromUnix(feat.postFlightDate);
                    IDictionary<string, string> map = new Dictionary<string, string>()  {
                   {"zzMissionNumber", Convert.ToString(feat.missionNumber)},
                   {"zzMissionDescriptionPostFlight", Convert.ToString(feat.missionDescriptionPostFlight)},
                   {"zzMissionIssues", Convert.ToString(feat.missionIssues)},
                   {"zzMaintenanceIssues", Convert.ToString(feat.maintenanceIssues)},
                   {"zzTotalFlightTime", Convert.ToString(feat.totalFlightTime) },
                   {"zzNumberOfBattery", Convert.ToString(numberOfBattery) },
                   {"zzFlightTimePerBattery", Convert.ToString(flightTimerPerBattery) },
                   {"zzPilotName", Convert.ToString(feat.pilotName) },
                   {"zzPostFlightDate", postFlightDate.Date.ToString("d") }
                };

                    var regex = new Regex(String.Join("|", map.Keys.Select(k => Regex.Escape(k))));
                    docText = regex.Replace(docText, m => stringUtils.escape(map[m.Value]));

                    using (StreamWriter sw = new StreamWriter(doc.MainDocumentPart.GetStream(FileMode.Create))) {
                        sw.Write(docText);
                    }
                    doc.Save();
                }

            };

            string targetPdfName = stringUtils.RemoveFilenameInvalidChars("UAS Post Flight_" + stringUtils.cleanTxt(content.feature.attributes["pilotName"]) + 
                "_" + missionDate.Date.ToString("MM-dd-yyyy") + "_" + DateTime.Now.ToString("HHmmss") + ".pdf");
            string targetPdfPath = directory + "\\wwwroot\\UASPostFlight\\" + targetPdfName;

            Application app = new Application();
            Document worddoc = app.Documents.Open(tempFileName);
            worddoc.ExportAsFixedFormat(targetPdfPath, WdExportFormat.wdExportFormatPDF);
            worddoc.Close();
            app.Quit();
            File.Delete(tempFileName);
            returnResponse.FileName = targetPdfName;

            var UpdateFeatureLayer = new UpdateFeatureLayer();
            var updateFeatureStatus = await UpdateFeatureLayer.updatePDFLinkFeatureLayer("postFlightPDF", targetPdfName, content);

            var missionNumber = Convert.ToString(content.feature.attributes["missionNumber"]);
            var missionNo = Convert.ToString(content.feature.attributes["missionNumber"]);
            if (Convert.ToString(content.feature.attributes["flightCompleted"]) == "yes")  {
                returnResponse.Status = await this.SendEmailPostFlight(content, serverAddress + "UASPostFlight/" + HttpUtility.UrlPathEncode(targetPdfName), serverAddress);
            } else {
                File.AppendAllText(directory + "\\wwwroot\\errors\\emailerror.txt", targetPdfName + "no email" + Environment.NewLine);
                returnResponse.Status = true;
            }
            return returnResponse;
        }

        public async Task<bool> SendEmailPostFlight(dynamic content, string postFlightPDF, string serverAddress)
        {
            var stringUtils = new StringUtils();
            var dateUtils = new DateUtils();
            var smtpEmail = new SmtpEmailClass();
            var attributes = content.feature.attributes;
            var missionNumber = stringUtils.convetToString(attributes.missionNumber);

            // create a updated pdf of the flight authorization application
            var FlightAuthorizationPDF = new FlightAuthorizationPDF();
            var appPdfName = await FlightAuthorizationPDF.CreateFlightAuthorizationApplication(content);
            string applicationPdfName = serverAddress + "UASApplications/" + appPdfName;

            var UpdateFeatureLayer = new UpdateFeatureLayer();
            var updateFeatureStatus = await UpdateFeatureLayer.updatePDFLinkFeatureLayer("pdfApplicationLink", applicationPdfName, content);

            var msgBody = "";
            msgBody += attributes.pilotName + " has submitted Post UAS Flight form for mission number: " + missionNumber + "<br>";
            msgBody += "Completed PDF form can be accessed at <a href=\"" + postFlightPDF + "\">" + postFlightPDF + "</a><br>";
            msgBody += "Post flight form completed on: " + dateUtils.GetDateFromUnix(attributes.postFlightDate).Date.ToString("d") + ".<br>";
            msgBody += "Updated flight authorization application document can be found at <a href=\"" + applicationPdfName + "\">" + applicationPdfName + "</a>.";

            var sendEmailResult = await smtpEmail.SendEmail(Convert.ToString(content.userInfo.email), config("CommitteeEmail"), "FW: UAS Post Flight " + attributes.pilotName, msgBody);
            bool sendEmailResult2 = await smtpEmail.SendEmail(config("CommitteeEmail"), Convert.ToString(content.userInfo.email), "FW: UAS Post Flight " + attributes.pilotName, msgBody);

            return (sendEmailResult && sendEmailResult2 && updateFeatureStatus);
        }
    }
}

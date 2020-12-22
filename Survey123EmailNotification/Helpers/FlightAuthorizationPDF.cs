using DocumentFormat.OpenXml.Packaging;
using Microsoft.Office.Interop.Word;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Survey123EmailNotification.Helpers
{
    public class FlightAuthorizationPDF
    {
        public async Task<string> CreateFlightAuthorizationApplication(dynamic content)
        {
            string directory = Directory.GetCurrentDirectory();
            var featureAttachments = new FeatureAttachments();
            var signatureUrl = await featureAttachments.GetSignatureAsync(content);
            var setting = new OpenSettings();
            setting.AutoSave = false;

            var attributes = content.feature.attributes;
            var dateUtils = new DateUtils();
            var stringUtils = new StringUtils();
            var missionDate = dateUtils.GetDateFromUnix(attributes.missionDate);
            var signatureDate = dateUtils.GetDateFromUnix(attributes.signatureDate);

            var dispatchNotifiedDate = dateUtils.GetDateFromUnix(attributes.dateDispatchNotified);
            string fileName = stringUtils.RemoveFilenameInvalidChars("UAS Flight Authorization Application_" + 
                stringUtils.cleanTxt(content.feature.attributes["pilotName"]) + "_" + 
                missionDate.Date.ToString("MM - dd - yyyy") + "_" + DateTime.Now.ToString("HHmmss") + ".doc");
            string tempFileName = directory + "\\wwwroot\\templates\\" + fileName;
            string templateFileName = directory + "\\wwwroot\\templates\\uas.docx";
            try  {
                File.Copy(templateFileName, tempFileName);
            } catch (Exception e) { Console.WriteLine(e); }

            FlightPurposes flightPurposes = new FlightPurposes();

            string flightPurpose = flightPurposes.getFlightPurpose(content.feature);
            string missionLocation = stringUtils.GetMissionLocation(content.feature.geometry);
            ImagePart imagePartLandOwner;
            using (WordprocessingDocument doc = WordprocessingDocument.Open(tempFileName, true)) {
                string docText = null;
                dynamic additionalRPIC = null;
                using (StreamReader sr = new StreamReader(doc.MainDocumentPart.GetStream())) {
                    docText = sr.ReadToEnd();
                }

                foreach (dynamic edits in content.applyEdits) {
                    if (Convert.ToInt32(edits.id) == 1) {
                        additionalRPIC = edits.adds;
                        break;
                    }
                }

                List<string> additionalRPICList = new List<string>();
                string listOfAdditionalRPIC = "";
                if (additionalRPIC != null) {
                    foreach (dynamic aRPIC in additionalRPIC)
                    {
                        additionalRPICList.Add(Convert.ToString(aRPIC.attributes.additionalRPICNames));
                    }
                }
                if (additionalRPICList.Count > 0) {
                    listOfAdditionalRPIC = String.Join(", ", additionalRPICList.ToArray());
                }

                TextInfo myTI = new CultureInfo("en-US", false).TextInfo;
                string landOwnerPermStr = Convert.ToString(content.feature.attributes["landOwnerPermissionRequired"]);
                string landOwnerName = Convert.ToString(content.feature.attributes["landOwnerName"]);
                string landOwnerPerm = landOwnerPermStr == "yes" ? "Yes" : (landOwnerPermStr == "no" ? "No" : "Already on file (" + landOwnerName+")");

                IDictionary<string, string> map = new Dictionary<string, string>()  {
                   {"zztimeofMission", Convert.ToString(content.feature.attributes["missionTime"])},
                   {"zzMissionNo", Convert.ToString(content.feature.attributes["missionNumber"])},
                   {"zzmissionDate", missionDate.Date.ToString("d")},
                   {"zzdispatchNotifiedDate",  dispatchNotifiedDate.ToString("MM/dd/yyyy HH:mm") },
                   {"zzpilotName",  Convert.ToString(content.feature.attributes["pilotName"])},
                   {"zzcallSign", Convert.ToString(content.feature.attributes["uavCallSign"]) },
                   {"zzaddRPIC", listOfAdditionalRPIC },
                   {"zzvisualObserver", Convert.ToString(content.feature.attributes["visualObserver"]) },
                   {"zzflightPurpose", flightPurpose },
                   {"zzmissionLocation", missionLocation },
                   {"zzlocationDescription", Convert.ToString(content.feature.attributes["locationDesciption"]) },
                   {"zzNoFlyZone", myTI.ToTitleCase(Convert.ToString(content.feature.attributes["outsideNoFlyZone"])) },
                   {"zzwindSpeed", Convert.ToString(content.feature.attributes["windSpeed"]) },
                   {"zzwindGust", Convert.ToString(content.feature.attributes["windGust"]) },
                   {"zzmissionHazardDetails", Convert.ToString(content.feature.attributes["missionHazardDetails"]) },
                    {"zzLandOwnerPermission", landOwnerPerm },
                   {"zzsignatureDate", signatureDate.Date.ToString("d")},
                };

                var regex = new Regex(String.Join("|", map.Keys.Select(k => Regex.Escape(k))));
                docText = regex.Replace(docText, m => stringUtils.escape(map[m.Value]));

                using (StreamWriter sw = new StreamWriter(doc.MainDocumentPart.GetStream(FileMode.Create))) {
                    sw.Write(docText);
                }
                ImagePart imagePart = (ImagePart)doc.MainDocumentPart.GetPartById("rId7");

                var webClient = new WebClient();
                byte[] imageBytes = webClient.DownloadData(signatureUrl);
                BinaryWriter writer = new BinaryWriter(imagePart.GetStream());
                writer.Write(imageBytes);
                writer.Close();
                imagePartLandOwner = (ImagePart)doc.MainDocumentPart.GetPartById("rId9");

                if (content.feature.attributes["landOwnerPermissionRequired"] == "yes") {
                    string landOwnerDocumentUrl = await featureAttachments.GetLandOwnerDocumentAsync(content);
                    byte[] imageBytes2 = webClient.DownloadData(landOwnerDocumentUrl);
                    BinaryWriter writer2 = new BinaryWriter(imagePartLandOwner.GetStream());
                    writer2.Write(imageBytes2);
                    writer2.Close();
                    doc.Save();
                } else {
                    doc.Save();
                }
            };
            if (content.feature.attributes["landOwnerPermissionRequired"] != "yes") {
                ReportUtils h = new ReportUtils();
                var l = new List<int>();
                l.Add(2);
                h.RemoveSections(tempFileName, l);
            }

            string targetPdfName = stringUtils.RemoveFilenameInvalidChars("UAS Flight Authorization Application_" + 
                stringUtils.cleanTxt(content.feature.attributes["pilotName"]) +
                "_" + missionDate.Date.ToString("MM-dd-yyyy") + "_" + DateTime.Now.ToString("HHmmss") + ".pdf");
            string targetPdfPath = directory + "\\wwwroot\\UASApplications\\" + targetPdfName;
            Application app = new Application();
            Document worddoc = app.Documents.Open(tempFileName);
            worddoc.ExportAsFixedFormat(targetPdfPath, WdExportFormat.wdExportFormatPDF);
            worddoc.Close();
            app.Quit();
            File.Delete(tempFileName);
            return targetPdfName;
        }
    }
}

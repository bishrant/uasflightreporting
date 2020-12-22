using System;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Survey123EmailNotification.Helpers;
using System.Threading.Tasks;
using Survey123EmailNotification.Models;
using System.Web;
using Serilog;

namespace Survey123EmailNotification.Controllers
{
    [Route("api/report")]
    [ApiController]
    public class ReportController : Controller
    {
        IdObfuscator obfuscator = new IdObfuscator();
        public string config(string name)  {
            return AppSettings.Configuration.GetSection(name).Value;
        }

        [EnableCors("AllowOrigin")]
        [HttpPost]
        public async Task<SaveFileResult> ReplaceOpenXML([FromBody] dynamic content) {
            Log.Information("Request content: " + Convert.ToString(content));

            var serverAddr = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}/";
            if (Convert.ToString(content.feature.attributes["flightCompleted"]) == "yes") {
                var PostFlightReport = new PostFlightReport();
                return await PostFlightReport.CreatePostFlightReport(content, serverAddr);
            }
            else  {
                var FlightAuthorizationPDF = new FlightAuthorizationPDF();
                var targetPdfName = await FlightAuthorizationPDF.CreateFlightAuthorizationApplication(content);

                Log.Information("target PDF name " + Convert.ToString(targetPdfName));
                var returnResponse = new SaveFileResult();
                returnResponse.FileName = targetPdfName;

                var UpdateFeatureLayer = new UpdateFeatureLayer();
                var updateFeatureStatus = await UpdateFeatureLayer.updatePDFLinkFeatureLayer("pdfApplicationLink", targetPdfName, content);
                Log.Information("UpdateFeatureLayer status failed or pass " + Convert.ToString(updateFeatureStatus));

                var missionNumber = Convert.ToString(content.feature.attributes["missionNumber"]);
                if (missionNumber == "" || missionNumber == null) {
                    returnResponse.Status = await this.SendEmailAboutRequest(content, serverAddr + "UASApplications/" + HttpUtility.UrlPathEncode(targetPdfName));
                } else {
                    returnResponse.Status = await this.SendEmailWithoutApprovalLink(content, serverAddr + "UASApplications/" + Uri.EscapeUriString(targetPdfName));
                }
                return returnResponse;
            }
        }


        public async Task<bool> SendEmailWithoutApprovalLink(dynamic content, string applicationPDFLink) {
            var stringUtils = new StringUtils();
            var smtpEmail = new SmtpEmailClass();
            var attributes = content.feature.attributes;
            var missionNumber = stringUtils.convetToString(attributes.missionNumber);

            var msgBody = "FW:<br><br>";
            msgBody += "UAS Committee,<br>Texas A & M Forest Service<br><br>";
            msgBody += "Completed UAS flight authorization request application for " + Convert.ToString(attributes.pilotName);
            msgBody += ", mission number: "+ missionNumber + " was updated and can be accessed at  <a href=\"" + applicationPDFLink + "\">" + applicationPDFLink + "</a>. <br>";
            msgBody += "If there are any concerns/comments about information in the attached PDF document, please email <a href='mailto:uastech@tfs.tamu.edu'>uastech@tfs.tamu.edu</a>.";

            msgBody += "<br><br>Thank you,<br>" + Convert.ToString(attributes.pilotName);
            bool sendEmailResult = await smtpEmail.SendEmail(Convert.ToString(content.userInfo.email), config("CommitteeEmail"), "FW: UAS Flight Authorization", msgBody);

            Log.Information("SendEmailWithoutApprovalLink msg body " + Convert.ToString(msgBody) + "  result of that task: " + Convert.ToString(sendEmailResult));

            var msgBodyForSender = Convert.ToString(attributes.pilotName) + ":<br><br>";
            msgBodyForSender += "Your flight authorization document has been submitted to uas@tfs.tamu.edu. <br>";
            msgBodyForSender += "A copy of UAS flight authorization application can be accessed at  <a href=\"" + applicationPDFLink + "\">" + applicationPDFLink + "</a>. <br>";

            msgBodyForSender += "Please email <a href='mailto:uas@tfs.tamu.edu'>uas@tfs.tamu.edu</a> if you have any questions/concerns about UAS flight authorization.";
            msgBodyForSender += "<br><br>Thank you,<br>UAS Committee<br>Texas A&M Forest Service";
            var sendEmailResult2 = await smtpEmail.SendEmail(config("CommitteeEmail"), Convert.ToString(content.userInfo.email), "FW: Request for UAS Flight Authorization", msgBodyForSender);
            Log.Information("SendEmailWithoutApprovalLink msg body " + Convert.ToString(msgBodyForSender) +  "  result of that task: " + Convert.ToString(sendEmailResult2) );

            return (sendEmailResult && sendEmailResult2);
        }

        public async Task<bool> SendEmailAboutRequest(dynamic content, string applicationPDFLink) {
            var stringUtils = new StringUtils();
            var smtpEmail = new SmtpEmailClass();
            var attributes = content.feature.attributes;

            FlightPurposes flightPurposes = new FlightPurposes();
            string flightPurpose = flightPurposes.getFlightPurpose(content.feature);
            var dateUtils = new DateUtils();
            var missionDate = dateUtils.GetDateFromUnix(attributes.missionDate);
            var lonDMS = stringUtils.DecimalDegToDMS(Convert.ToDecimal(content.feature.geometry.x));
            var latDMS = stringUtils.DecimalDegToDMS(Convert.ToDecimal(content.feature.geometry.y));

            var msgBody = "";
            msgBody += "UAS Committee,<br>Texas A & M Forest Service<br><br>";
            msgBody += "I would like to request an authorization to perform a UAS flight on " + missionDate.Date.ToString("d") + " " + Convert.ToString(attributes.missionTime);
            msgBody += " at Lon: " + lonDMS + ", Lat: " + latDMS + " using " + Convert.ToString(content.feature.attributes.uavCallSign) + ". <br>";
            msgBody += Convert.ToString(attributes.pilotName) + " will be the designated remote pilot in command and " +
                Convert.ToString(attributes.visualObserver) + " will be the visual observer for this mission.<br>";

            msgBody += "The purpose of this mission is " + flightPurpose + ". The wind speed is expected to be around " +
                Convert.ToString(attributes.windSpeed) + " mph with gusts of " + 
                Convert.ToString(attributes.windGust) + " mph.<br><br>";
            msgBody += "A copy of UAS flight authorization application can be accessed at<a href=\"" + 
                applicationPDFLink + "\">" + applicationPDFLink+"</a>. <br>";
            int objectId = Convert.ToInt32(content.feature.result.objectId.Value);
            var link = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}/?featureId=" + obfuscator.encryptId(objectId);
            msgBody += "<br>Please go to <a href='"+ link +"'>"+link+"</a> to approve/deny this flight authorization request.<br><br>";
            msgBody += "<br>Thank you,<br>" + Convert.ToString(attributes.pilotName);

            bool sendEmailResult = await smtpEmail.SendEmail(Convert.ToString(content.userInfo.email), config("CommitteeEmail"), "Request for UAS Flight Authorization", msgBody);

            Log.Information("SendEmailAboutRequest msg body " + Convert.ToString(msgBody) + "  result of that task: " + Convert.ToString(sendEmailResult));

            var msgBodyForSender = Convert.ToString(attributes.pilotName)+":<br><br>";
            msgBodyForSender += "Your request for flight authorization has been submitted to uas@tfs.tamu.edu. <br>";
            msgBodyForSender += "A copy of UAS flight authorization application can be accessed at <a href=\"" + applicationPDFLink + "\">" + applicationPDFLink + "</a>. <br>";
            msgBodyForSender += "Please email <a href='mailto:uas@tfs.tamu.edu'>uas@tfs.tamu.edu</a> if you have any questions/concerns about UAS flight authorization.";
            msgBodyForSender += "<br><br>Thank you,<br>UAS Committee<br>Texas A&M Forest Service";
            var sendEmailResult2 = await smtpEmail.SendEmail(config("CommitteeEmail"), Convert.ToString(content.userInfo.email), "FW: Request for UAS Flight Authorization", msgBodyForSender);

            Log.Information("SendEmailAboutRequest msg body sendEmailResult2 " + Convert.ToString(msgBodyForSender) + "  result of that task: " + Convert.ToString(sendEmailResult2));
            return (sendEmailResult && sendEmailResult2);
        }
    }
}
using System;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Survey123EmailNotification.Helpers;
using System.Threading.Tasks;
using Survey123EmailNotification;
using Survey123EmailNotification.Models;

namespace Survey123EmailNotification.Controllers
{
    [Route("api/offlinereport")]
    [ApiController]
    public class OfflineReportController : Controller
    {
        IdObfuscator obfuscator = new IdObfuscator();


        public string config(string name)  {
            return AppSettings.Configuration.GetSection(name).Value;
        }

        [EnableCors("AllowOrigin")]
        [HttpGet("{featureId}")]
        public async Task<SaveFileResult> ReplaceOpenXML(int featureId) {
            var serverAddr = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}/";
                var OfflineFlightAuthorizationPDF = new OfflineFlightAuthorizationPDF();
                var targetPdfName = await OfflineFlightAuthorizationPDF.OfflineCreateFlightPDF(featureId);
                var returnResponse = new SaveFileResult();
                string directory = System.IO.Directory.GetCurrentDirectory();

/*                returnResponse.FileName = targetPdfName;
                var UpdateFeatureLayer = new UpdateFeatureLayer();
                var updateFeatureStatus = await UpdateFeatureLayer.updatePDFLinkFeatureLayer("pdfApplicationLink", targetPdfName, content);
                var missionNumber = Convert.ToString(content.feature.attributes["missionNumber"]);
                var missionNo = Convert.ToString(content.feature.attributes["missionNumber"]);*/
                return returnResponse;
            
        }

    }
}
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Office.Interop.Word;
using Serilog;
using Survey123EmailNotification.Helpers;

namespace Survey123EmailNotification.Controllers
{
    [Route("api/test")]
    [ApiController]
    public class TestController : ControllerBase {
        IdObfuscator obfuscator = new IdObfuscator();

        [HttpGet("{featureId}")]
        public string Test(int featureId) {
            var ob = obfuscator.encryptId(featureId);
            var f = obfuscator.decryptId(ob);
            return ob + " -- " + f;
        }

        [HttpPost]
        public async Task<String> Post([FromBody] dynamic content)
        {
            var smtp = new SmtpEmailClass();
            string x = Convert.ToString(content);
            //var obj = JsonConvert.DeserializeObject<dynamic>(content);
            var success = await smtp.SendEmailWithDecision("bishrant.adhikari@tfs.tamu.edu", "bishrnt@gmail.com", "TEST", "THIS IS A TEST");
            Log.Information(x);
            return "post pass";
        }
    }
}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.IO;

namespace Survey123EmailNotification.Helpers
{
    public class StringUtils
    {
        public string convetToString(object o) {
            string val = "";
            if (Convert.ToString(o) != "") {
                val = Convert.ToString(o);
            }
            return val;
        }

        public string escape(dynamic text) {
            var t = Convert.ToString(text);
            if (t == "" || t == null) {
                return t;
            } else {
                t = Regex.Replace(t, "&", "&amp;");
                t = Regex.Replace(t, "'", "&apos;");
                t = Regex.Replace(t, "\"", "&quot;");
                t = Regex.Replace(t, ">", "&gt;");
                t = Regex.Replace(t, "<", "&lt;");
                return t;
            }
        }

        public string cleanTxt(dynamic text) {
            return Regex.Replace(Convert.ToString(text), @"(\s+|@|&|'|\(|\)|<|>|#)", "");
        }

        public string RemoveFilenameInvalidChars(string filename) {
            return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
        }

        public string GetAttribute(dynamic attributes, string value)
        {
            return Convert.ToString(attributes[value]);
        }

        public string GetMissionLocation(dynamic geometry)
        {
            var lonDMS = DecimalDegToDMS(Convert.ToDecimal(geometry.x));
            var latDMS = DecimalDegToDMS(Convert.ToDecimal(geometry.y));
            return "Lon: " + lonDMS + ", Lat: " + latDMS;
        }

        public string DecimalDegToDMS(Decimal d)
        {
            string dms;
            if (d < 0)
            {
                d = d * -1;
                var dd = Decimal.Truncate(d);
                var mm = Decimal.Truncate((d - dd) * 60);
                var ss = Math.Round((d - dd - mm / 60) * 3600, 2);
                dms = "- " + dd + "° " + mm + "' " + ss + "''";

            } else
            {
                var dd = Decimal.Truncate(d);
                var mm = Decimal.Truncate((d - dd) * 60);
                var ss = Math.Round((d - dd - mm / 60) * 3600, 2);
                dms = dd + "° " + mm + "' " + ss + "''";

            }
            return dms;
        }

        public FormUrlEncodedContent createUpdateAttributeJSON(string token, dynamic updateContent)
        {
            return new FormUrlEncodedContent(new[]
             {
                new KeyValuePair<string, string>("f", "json"),
                new KeyValuePair<string, string>("updates", JsonConvert.SerializeObject(updateContent)),
                new KeyValuePair<string, string>("token", token)
            });
        }

        public class UpdateApplicationPDFLink
        {
            public UpdateApplicationPDF attributes { get; set; }
        }
        public class UpdateApplicationPDF
        {
            public int objectid { get; set; }
            public string pdfApplicationLink { get; set; }
            public string email { get; set; }
        }

        public UpdateApplicationPDFLink updatePDFLink(int objectid, string pdfApplicationLink, string email)
        {
            var updates = new UpdateApplicationPDFLink();

            var attributes = new UpdateApplicationPDF();
            attributes.objectid = objectid;
            attributes.pdfApplicationLink = pdfApplicationLink;
            attributes.email = email;
            updates.attributes = attributes;
            return updates;
        }

        public class UpdatePostFlightPDFLink {
            public UpdatePostFlightPDF attributes { get; set; }
        }

        public class UpdatePostFlightPDF {
            public int objectid { get; set; }
            public string postFlightPDF { get; set; }
        }

        public UpdatePostFlightPDFLink updatePostFlight(int objectid, string postFlightPDF) {
            var updates = new UpdatePostFlightPDFLink(){
                attributes = new UpdatePostFlightPDF() {
                    objectid = objectid,
                    postFlightPDF = postFlightPDF
                }
            };
            return updates;
        }
    }
}

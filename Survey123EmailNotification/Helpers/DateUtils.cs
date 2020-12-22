using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Survey123EmailNotification.Helpers
{
    public class DateUtils
    {
        public DateTime GetDateFromUnix(dynamic v)
        {
            double unixTimeStamp = Convert.ToDouble(v);
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
}

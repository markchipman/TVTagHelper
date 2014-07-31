using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTagHelper
{
    public static class Extensions
    {
        public static string ConciseFormat(this TimeSpan ts)
        {
            var sb = new StringBuilder();

            if((int)ts.TotalHours > 0)
            {
                sb.Append((int)ts.TotalHours);
                sb.Append(":");
            }

            sb.Append(ts.Minutes.ToString("d2"));
            sb.Append(":");
            sb.Append(ts.Seconds.ToString("d2"));

            return sb.ToString();
        }
    }
}

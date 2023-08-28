using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lyricism
{
    public class LoggingHttpClient : HttpClient
    {
        public List<string> DebugLog { get; set; }
        public LoggingHttpClient(ref List<string> debugLog) : base()
        {
            DebugLog = debugLog;
        }
    }
}

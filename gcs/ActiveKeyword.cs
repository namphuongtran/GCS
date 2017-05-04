using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gcs
{
    public class ActiveKeyword
    {
        public string Keyword { get; set; }
        public string Url { get; set; }
        public string TotalResult { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
    }

    public class GoogleApi
    {
        public string ApiKey { get; set; }
        public string SearchEngineId { get; set; }
    }
}

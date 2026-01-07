using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SearchProxy.Common
{
    public class SitecoreSearchClientException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string ResponseBody { get; }

        public SitecoreSearchClientException(string message, HttpStatusCode statusCode, string responseBody)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }

        public override string ToString() => $"{base.ToString()}\nStatus: {(int)StatusCode} {StatusCode}\nBody: {ResponseBody}";
    }
}

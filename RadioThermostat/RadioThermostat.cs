using EasyHttp.Http;

namespace RadioThermostat
{
    public class RadioThermostat
    {
        public RadioThermostat(string host)
        {
            Host = host;
        }

        public string Host { get; private set; }

        public string GetAPIVersion()
        {
            return Get(Host, string.Empty);
        }

        private static string Get(string hostIP, string requestJson)
        {
            var http = new HttpClient();
            http.Request.Accept = HttpContentTypes.ApplicationJson;

            var url = string.Format("http://{0}/tstat", hostIP);
            try
            {
                HttpResponse resp = http.Post(url, requestJson, HttpContentTypes.ApplicationJson);
                return resp.RawText;
            }
            catch (System.Net.WebException e)
            {
                return string.Empty;
            }        
        }

    }
}

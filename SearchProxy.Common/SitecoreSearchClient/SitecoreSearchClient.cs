using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace SearchProxy.Common
{
    public interface ISitecoreSearchClient
    {
        Task<JObject> SearchAsync(SitecoreSearchClientRequest request);
    }

    public class SitecoreSearchClient : ISitecoreSearchClient
    {
        private readonly SitecoreSearchClientSettings _settings;
        private readonly IFlurlClient _client;

        public SitecoreSearchClient(IOptions<SitecoreSearchClientSettings> settings)
        {

            _settings = settings.Value;
            _client = new FlurlClient(new Url($"{_settings.BaseApiUrl}/discover/v2/{_settings.DomainId}"));
            _client.Settings.Timeout = _settings.Timeout;
        }

        /// <summary>
        /// POST /discover/v2/{domainId}
        /// </summary>
        public async Task<JObject> SearchAsync(SitecoreSearchClientRequest request)
        {
            var req = _client.Request().WithHeader("X-API-Key", _settings.ApiKey);

            // simple retry on >=500 and timeouts
            int attempts = 0;
            while (true)
            {
                attempts++;
                try
                {
                    var resp = await req.AllowAnyHttpStatus().PostJsonAsync(request);

                    if (resp.ResponseMessage.IsSuccessStatusCode)
                        return await resp.GetJsonAsync<JObject>();

                    var body = await resp.ResponseMessage.Content.ReadAsStringAsync();
                    throw new SitecoreSearchClientException("Search request failed",
                        resp.ResponseMessage.StatusCode, body);
                }
                catch (FlurlHttpException ex) when (attempts <= _settings.RetryCount && IsTransient(ex))
                {
                    await Task.Delay(_settings.RetryDelay);
                    continue;
                }
            }
        }

        private static bool IsTransient(FlurlHttpException ex)
        {
            if (ex.Call?.Response == null) return true; // network
            var code = (int)ex.Call.Response.StatusCode;
            return code >= 500 || code == 429;
        }
    }
}

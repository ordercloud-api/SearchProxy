using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrderCloud.Catalyst;
using SearchProxy.Common;
using System.Text;

namespace SearchProxy.AzureFunction
{
    public class SearchFunction
    {
        private readonly ITokenValidator _auth;
        private readonly IOrderCloudSearchService _searchService;

        public SearchFunction(ITokenValidator auth, IOrderCloudSearchService searchService)
        {
            _auth = auth;
            _searchService = searchService;
        }

        [Function("search")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            var requiredRoles = new List<string>();
            string? userToken = req.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            DecodedUserInfoToken context;
            try
            {
                context = await _auth.ValidateUserInfoTokenAsync(userToken, requiredRoles);
            }
            catch (Exception)
            {
                var message = requiredRoles.Count > 0 ? $"Userinfo token is missing, expired, or missing required roles: {string.Join(",", requiredRoles)}" : "Userinfo token is missing or expired";
                return new UnauthorizedObjectResult(new { message });
            }

            var searchRequest = await ReadJsonBodyAsync<OrderCloudSearchRequest>(req);

            JObject result = await _searchService.SearchAsync(searchRequest, context);
            return new ContentResult
            {
                StatusCode = StatusCodes.Status200OK,
                ContentType = "application/json",
                Content = result.ToString(Formatting.None)
            };
        }


        private static async Task<T> ReadJsonBodyAsync<T>(HttpRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));

            // Allow multiple reads (e.g., logging, downstream services)
            req.EnableBuffering();

            // Read body as UTF-8 without BOM; leave stream open so we can rewind
            using var reader = new StreamReader(
                req.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true
            );

            var json = await reader.ReadToEndAsync();
            // Reset position for any subsequent reads in your pipeline
            req.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Request body is empty.", nameof(req));

            try
            {
                var result = JsonConvert.DeserializeObject<T>(json);
                if (result == null)
                    throw new JsonException("Deserialized body resulted in null.");
                return result;
            }
            catch (JsonException ex)
            {
                // Include the first 256 chars to help diagnose malformed JSON without logging entire payload
                var preview = json.Length > 256 ? json[..256] + "…" : json;
                throw new ArgumentException($"Invalid JSON in request body. Preview: {preview}", nameof(req), ex);
            }
        }
    }
}

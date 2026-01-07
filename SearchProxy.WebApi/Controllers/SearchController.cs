using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using OrderCloud.Catalyst;
using SearchProxy.Common;

namespace SearchProxy.WebApi.Controllers
{
    public class SearchController : CatalystController
    {
        private readonly IOrderCloudSearchService _searchService;
        public SearchController(IOrderCloudSearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpPost("api/search")]
        [OrderCloudUserInfoAuth]
        public async Task<JObject> Search([FromBody] OrderCloudSearchRequest requestBody)
        {
            return await _searchService.SearchAsync(requestBody, UserInfoContext);
        }
    }
}

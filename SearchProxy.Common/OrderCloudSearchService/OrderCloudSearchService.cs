using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OrderCloud.Catalyst;

namespace SearchProxy.Common
{
    public interface IOrderCloudSearchService
    {
        Task<JObject> SearchAsync(OrderCloudSearchRequest originalRequest, DecodedUserInfoToken context);
    }

    public class OrderCloudSearchService : IOrderCloudSearchService
    {
        private readonly OrderCloudSearchSettings _settings;
        private readonly ISitecoreSearchClient _client;
        private readonly ISearchRequestMapper _requestMapper;
        private readonly ISearchResponseMapper _responseMapper;

        public OrderCloudSearchService(
            IOptions<OrderCloudSearchSettings> settings,
            ISitecoreSearchClient client, 
            ISearchRequestMapper requestMapper, 
            ISearchResponseMapper responseMapper)
        {
            _settings = settings.Value;
            _client = client;
            _requestMapper = requestMapper;
            _responseMapper = responseMapper;
        }

        public async Task<JObject> SearchAsync(OrderCloudSearchRequest originalRequest, DecodedUserInfoToken context)
        {
            var orderCloudExtensions = originalRequest.OrderCloud;
            if(!string.IsNullOrEmpty(_settings.OrderCloudMarketplaceId) && context.MarketplaceID != _settings.OrderCloudMarketplaceId)
            {
                throw new UnauthorizedAccessException("User's marketplaceId does not match the configured marketplace");
            }
            var mappedRequest = _requestMapper.Map(originalRequest, context, orderCloudExtensions);
            var response = await _client.SearchAsync(mappedRequest);
            return _responseMapper.Map(response, context, orderCloudExtensions);
        }
    }
}

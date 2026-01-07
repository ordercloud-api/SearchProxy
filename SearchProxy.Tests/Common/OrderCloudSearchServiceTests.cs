using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using NSubstitute;
using OrderCloud.Catalyst;
using SearchProxy.Common;
using Xunit;

namespace SearchProxy.Common.Tests
{
    public class OrderCloudSearchServiceTests
    {
        private readonly IOptions<OrderCloudSearchSettings> _settings = Substitute.For<IOptions<OrderCloudSearchSettings>>();
        private readonly ISitecoreSearchClient _client = Substitute.For<ISitecoreSearchClient>();
        private readonly ISearchRequestMapper _requestMapper = Substitute.For<ISearchRequestMapper>();
        private readonly ISearchResponseMapper _responseMapper = Substitute.For<ISearchResponseMapper>();


        [Fact]
        public async Task SucceedsIfMarketplaceMatchesSettings()
        {
            // Arrange
            _settings.Value.Returns(new OrderCloudSearchSettings { OrderCloudMarketplaceId = "test-marketplace" });
            var sut = new OrderCloudSearchService(_settings, _client, _requestMapper, _responseMapper);
            var originalRequest = new OrderCloudSearchRequest
            {
                OrderCloud = new OrderCloudExtensions(),
                Widget = new WidgetsContainer
                {
                    Items = new[]
                    {
                        new WidgetItem { RfkId = "rfk1", Entity = "product" }
                    }
                }
            };
            var context = MockUserInfoToken.BuildContext(marketplaceID: "test-marketplace");
            var mappedRequest = new SitecoreSearchClientRequest
            {
                Widget = new WidgetsContainer
                {
                    Items = new[]
                    {
                        new WidgetItem { RfkId = "rfk1", Entity = "product" }
                    }
                }
            };
            var sitecoreResponse = new JObject();
            var expectedResponse = new JObject();

            _requestMapper.Map(originalRequest, context, originalRequest.OrderCloud).Returns(mappedRequest);
            _client.SearchAsync(mappedRequest).Returns(sitecoreResponse);
            _responseMapper.Map(sitecoreResponse, context, originalRequest.OrderCloud).Returns(expectedResponse);

            // Act
            var result = await sut.SearchAsync(originalRequest, context);

            // Assert
            Assert.Same(expectedResponse, result);
            _requestMapper.Received(1).Map(originalRequest, context, originalRequest.OrderCloud);
            await _client.Received(1).SearchAsync(mappedRequest);
            _responseMapper.Received(1).Map(sitecoreResponse, context, originalRequest.OrderCloud);
        }

        [Fact]
        public async Task SucceedsIfMarketplaceSettingIsEmpty()
        {
            // Arrange
            _settings.Value.Returns(new OrderCloudSearchSettings()); // OrderCloudMarketplaceId not set
            var sut = new OrderCloudSearchService(_settings, _client, _requestMapper, _responseMapper);
            var originalRequest = new OrderCloudSearchRequest
            {
                OrderCloud = new OrderCloudExtensions(),
                Widget = new WidgetsContainer
                {
                    Items = new[]
                    {
                        new WidgetItem { RfkId = "rfk1", Entity = "product" }
                    }
                }
            };
            var context = MockUserInfoToken.BuildContext(marketplaceID: "a-different-marketplace");
            var mappedRequest = new SitecoreSearchClientRequest
            {
                Widget = new WidgetsContainer
                {
                    Items = new[]
                    {
                        new WidgetItem { RfkId = "rfk1", Entity = "product" }
                    }
                }
            };
            var sitecoreResponse = new JObject();
            var expectedResponse = new JObject();

            _requestMapper.Map(originalRequest, context, originalRequest.OrderCloud).Returns(mappedRequest);
            _client.SearchAsync(mappedRequest).Returns(sitecoreResponse);
            _responseMapper.Map(sitecoreResponse, context, originalRequest.OrderCloud).Returns(expectedResponse);

            // Act
            var result = await sut.SearchAsync(originalRequest, context);

            // Assert
            Assert.Same(expectedResponse, result);
            _requestMapper.Received(1).Map(originalRequest, context, originalRequest.OrderCloud);
            await _client.Received(1).SearchAsync(mappedRequest);
            _responseMapper.Received(1).Map(sitecoreResponse, context, originalRequest.OrderCloud);
        }


        [Fact]
        public async Task ThrowsIfMarketplaceDoesntMatchSettings()
        {
            // Arrange
            _settings.Value.Returns(new OrderCloudSearchSettings { OrderCloudMarketplaceId = "test-marketplace" });
            var sut = new OrderCloudSearchService(_settings, _client, _requestMapper, _responseMapper);
            var originalRequest = new OrderCloudSearchRequest
            {
                OrderCloud = new OrderCloudExtensions(),
                Widget = new WidgetsContainer
                {
                    Items = new[]
                    {
                new WidgetItem { RfkId = "rfk1", Entity = "product" }
            }
                }
            };

            // context with a different marketplace than settings
            var context = MockUserInfoToken.BuildContext(marketplaceID: "a-different-marketplace");

            // Act
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.SearchAsync(originalRequest, context)
            );

            // Assert: exception message is correct
            Assert.Equal("User's marketplaceId does not match the configured marketplace", ex.Message);

            // Assert: short-circuit occurred; no calls downstream
            _requestMapper.DidNotReceive()
                .Map(Arg.Any<OrderCloudSearchRequest>(), Arg.Any<DecodedUserInfoToken>(), Arg.Any<OrderCloudExtensions>());
            await _client.DidNotReceive().SearchAsync(Arg.Any<SitecoreSearchClientRequest>());
            _responseMapper.DidNotReceive()
                .Map(Arg.Any<JObject>(), Arg.Any<DecodedUserInfoToken>(), Arg.Any<OrderCloudExtensions>());
        }
    }
}

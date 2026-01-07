using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SearchProxy.Common
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds and configures all the necessary SearchProxy services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">An action to configure the unified SearchSync options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSearchProxy(this IServiceCollection services, Action<SearchProxySettings> configureOptions)
        {
            services.Configure(configureOptions);

            services.AddOptions<SitecoreSearchClientSettings>()
                .Configure<IOptions<SearchProxySettings>>((serviceSettings, appSettings) =>
                {
                    var settings = appSettings.Value;
                    serviceSettings.BaseApiUrl = settings.SearchBaseApiUrl;
                    serviceSettings.DomainId = settings.SearchDomainId;
                    serviceSettings.ApiKey = settings.SearchApiKey;
                    serviceSettings.Timeout = settings.SearchTimeout;
                    serviceSettings.RetryCount = settings.SearchRetryCount;
                    serviceSettings.RetryDelay = settings.SearchRetryDelay;
                });

            services.AddOptions<OrderCloudSearchSettings>()
                .Configure<IOptions<SearchProxySettings>>((serviceOptions, appSettings) =>
                {
                    var settings = appSettings.Value;
                    serviceOptions.OrderCloudMarketplaceId = settings.OrderCloudMarketplaceId;
                });

            // Register core services
            services.AddSingleton<ISitecoreSearchClient, SitecoreSearchClient>();
            services.AddSingleton<IOrderCloudSearchService, OrderCloudSearchService>();
            services.AddSingleton<ISearchRequestMapper, SearchRequestMapper>();
            services.AddSingleton<ISearchResponseMapper, SearchResponseMapper>();

            return services;
        }
    }
}

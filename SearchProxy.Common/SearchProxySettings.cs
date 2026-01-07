using System.ComponentModel.DataAnnotations;

namespace SearchProxy.Common
{
    public class SearchProxySettings
    {
        /// <summary>Root host for Search API. Examples:
        /// - discover.sitecorecloud.io
        /// - nickname.rfk.customerdomain.com (subdomain)
        /// </summary>
        [Required]
        public string SearchBaseApiUrl { get; init; } = "https://discover.sitecorecloud.io";

        /// <summary> Numeric domain identifier </summary>
        [Required]
        public required string SearchDomainId { get; init; }

        /// <summary>API key for making authenticated API calls</summary>
        [Required]
        public required string SearchApiKey { get; init; }

        /// <summary>Default request timeout.</summary>
        public TimeSpan SearchTimeout { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>Max retries on transient failures.</summary>
        public int SearchRetryCount { get; init; } = 2;

        /// <summary>Backoff between retries.</summary>
        public TimeSpan SearchRetryDelay { get; init; } = TimeSpan.FromMilliseconds(350);

        /// <summary>Optional. If provided, all requests must originate from this marketplace; otherwise, an UnauthorizedAccessException is thrown. </summary>
        public required string OrderCloudMarketplaceId { get; init; }
    }
}

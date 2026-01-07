using System.ComponentModel.DataAnnotations;

namespace SearchProxy.Common
{
    public class SitecoreSearchClientSettings
    {
        /// <summary>Root host for Search API. Examples:
        /// - discover.sitecorecloud.io
        /// - nickname.rfk.customerdomain.com (subdomain) </summary>
        [Required]
        public string BaseApiUrl { get; set; } = "https://discover.sitecorecloud.io";


        /// <summary> Numeric domain identifier </summary>
        [Required]
        public required string DomainId { get; set; }

        /// <summary>API key for making authenticated API calls </summary>
        [Required]
        public required string ApiKey { get; set; }

        /// <summary>Default request timeout.</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Max retries on transient failures.</summary>
        public int RetryCount { get; set; } = 2;

        /// <summary>Backoff between retries.</summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(350);
    }
}

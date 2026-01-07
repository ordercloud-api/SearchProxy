using Newtonsoft.Json;

namespace SearchProxy.Common
{
    public class OrderCloudSearchRequest : SitecoreSearchClientRequest
    {
        [JsonProperty("ordercloud")]
        public OrderCloudExtensions? OrderCloud { get; set; }
    }

    public class OrderCloudExtensions
    {
        /// <summary>
        /// If populated will map the response from search and include only inventory locations specified
        /// this helps reduce payload size to client
        /// </summary>
        [JsonProperty("requiredinventorylocations")]
        public IList<string>? RequiredInventoryLocations { get; set; } = new List<string>();

        /// <summary>
        /// Allows you to list products from a specific supplier
        /// </summary>
        [JsonProperty("sellerid")]
        public string? SellerId { get; set; } = "";
    }
}

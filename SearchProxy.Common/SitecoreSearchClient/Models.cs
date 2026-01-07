using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace SearchProxy.Common
{
    public class SitecoreSearchClientRequest
    {
        [Required]
        [JsonProperty("widget")]
        public required WidgetsContainer Widget { get; set; }

        [JsonProperty("context")]
        public dynamic? Context { get; set; }
    }

    public class WidgetsContainer
    {
        [Required]
        [JsonProperty("items")]
        public required WidgetItem[] Items { get; set; }
    }

    public class WidgetItem
    {
        [Required]
        [JsonProperty("rfk_id")]
        public required string RfkId { get; set; }

        [Required]
        [JsonProperty("entity")]
        public required string Entity { get; set; }

        [JsonProperty("search")]
        public WidgetSearch? Search { get; set; }

        [JsonProperty("rfk_flags")]
        public dynamic? RfkFlags { get; set; }

        [JsonProperty("sources")]
        public dynamic? Sources { get; set; }

        [JsonProperty("appearance")]
        public dynamic? Appearance { get; set; }

        [JsonProperty("recommendations")]
        public WidgetRecommendations? Recommendations { get; set; }
    }

    public class WidgetSearch
    {
        [JsonProperty("content")]
        public dynamic? Content { get; set; }

        [JsonProperty("filter")]
        public WidgetSearchFilter? Filter { get; set; }

        [JsonProperty("query")]
        public dynamic? Query { get; set; }

        [JsonProperty("facet")]
        public dynamic? Facet { get; set; }

        [JsonProperty("limit")]
        public int? Limit { get; set; }

        [JsonProperty("offset")]
        public int? Offset { get; set; }

        [JsonProperty("sort")]
        public dynamic? Sort { get; set; }

        [JsonProperty("group_by")]
        public dynamic? GroupBy { get; set; }

        [JsonProperty("personalization")]
        public dynamic? Personalization { get; set; }

        [JsonProperty("ranking")]
        public dynamic? Ranking { get; set; }

        [JsonProperty("related_questions")]
        public dynamic? RelatedQuestions { get; set; }

        [JsonProperty("response_context")]
        public dynamic? ResponseContext { get; set; }

        [JsonProperty("rule")]
        public dynamic? Rule { get; set; }

        [JsonProperty("suggestion")]
        public dynamic? Suggestion { get; set; }

        [JsonProperty("swatch")]
        public dynamic? Swatch { get; set; }
    }

    public class WidgetSearchFilter
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("value")]
        public object? Value { get; set; }

        [JsonProperty("values")]
        public dynamic? Values { get; set; }

        [JsonProperty("type")]
        public string? TypeValue { get; set; }

        [JsonProperty("distance")]
        public string? Distance { get; set; }

        [JsonProperty("lat")]
        public double? Lat { get; set; }

        [JsonProperty("lon")]
        public double? Lon { get; set; }

        [JsonProperty("coordinates")]
        public WidgetSearchFilterCoordinates? Coordinates { get; set; }

        [JsonProperty("filters")]
        public WidgetSearchFilter[]? Filters { get; set; }
    }

    public class WidgetSearchFilterCoordinates
    {
        [JsonProperty("lat")]
        public double? Lat { get; set; }

        [JsonProperty("lon")]
        public double? Lon { get; set; }
    }

    public class SearchQuery
    {
        [JsonProperty("keyphrase")]
        public string? Keyphrase { get; set; }
    }

    public class WidgetRecommendations
    {
        [JsonProperty("content")]
        public dynamic? Content { get; set; }

        [JsonProperty("filter")]
        public WidgetSearchFilter? Filter { get; set; }

        [JsonProperty("limit")]
        public int? Limit { get; set; }

        [JsonProperty("offset")]
        public int? Offset { get; set; }

        [JsonProperty("paginate_recommendations")]
        public bool? PaginateRecommendations { get; set; }

        [JsonProperty("recipe")]
        public dynamic? Recipe { get; set; }

        [JsonProperty("rule")]
        public dynamic? Rule { get; set; }

        [JsonProperty("swatch")]
        public dynamic? Swatch { get; set; }
    }
}

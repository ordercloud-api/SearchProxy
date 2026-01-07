using Newtonsoft.Json;
using OrderCloud.Catalyst;

namespace SearchProxy.Common
{
    public interface ISearchRequestMapper
    {
        SitecoreSearchClientRequest Map(
            OrderCloudSearchRequest originalRequest,
            DecodedUserInfoToken userContext,
            OrderCloudExtensions? orderCloudExtensions
        );
    }

    public class SearchRequestMapper : ISearchRequestMapper
    {
        public SitecoreSearchClientRequest Map(
            OrderCloudSearchRequest originalRequest,
            DecodedUserInfoToken userContext,
            OrderCloudExtensions? orderCloudExtensions
        )
        {
            if (originalRequest is null) throw new ArgumentNullException(nameof(originalRequest));
            if (userContext is null) throw new ArgumentNullException(nameof(userContext));

            var sellerId = orderCloudExtensions?.SellerId;

            var newRequest = DeepClone(originalRequest)
                ?? throw new InvalidOperationException("Failed to clone the incoming request.");

            // Nothing to do if the request has no widget section
            if (newRequest.Widget?.Items is null || newRequest.Widget.Items.Count() == 0)
                return newRequest;

            for (var i = 0; i < newRequest.Widget.Items.Count(); i++)
            {
                var item = newRequest.Widget.Items[i];
                if (!IsProductEntity(item)) continue;

                // Ensure Search exists if both Search and Recommendations were missing
                if (item.Search is null && item.Recommendations is null)
                {
                    item.Search = new WidgetSearch();
                }

                if (item.Search is not null)
                {
                    item.Search.Filter = AlterFilters(item.Search.Filter, userContext, sellerId);
                }

                if (item.Recommendations is not null)
                {
                    item.Recommendations.Filter = AlterFilters(item.Recommendations.Filter, userContext, sellerId);
                }
            }

            return newRequest;
        }

        private static bool IsProductEntity(WidgetItem item) =>
            item?.Entity is not null &&
            item.Entity.Equals("product", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Compose visibility constraints with an existing filter (if any).
        /// If there is no existing filter, the result is the visibility filter alone.
        /// If the existing filter is "simple" (Name present), AND it with visibility.
        /// If the existing filter is "complex" (no Name, has Filters), AND visibility with it.
        /// </summary>
        protected WidgetSearchFilter AlterFilters(
            WidgetSearchFilter? existing,
            DecodedUserInfoToken userContext,
            string? sellerId
        )
        {
            var visibility = GetVisibilityFilter(userContext, sellerId);

            if (existing is null)
            {
                // No filters provided — just apply visibility
                return visibility;
            }

            // Simple filter (e.g., Name + eq/lt/etc.)
            if (!string.IsNullOrWhiteSpace(existing.Name))
            {
                return And(visibility, existing);
            }

            // Complex filter (existing is itself a composed filter)
            // AND visibility with the existing composition
            return And(visibility, existing);
        }

        /// <summary>
        /// Builds the "visibility" constraints:
        /// - Active == true
        /// - Marketplace == userContext.MarketplaceID
        /// - Suppliers == sellerId (when present)
        /// - (Buyers == userContext.CompanyID OR UserGroups == each group in userContext.Groups)
        /// </summary>
        protected WidgetSearchFilter GetVisibilityFilter(
            DecodedUserInfoToken userContext,
            string? sellerId
        )
        {
            var active = Eq(Constants.KnownSearchAttributes.Active, true);

            var marketplaceField = Eq(
                Constants.KnownSearchAttributes.Marketplace,
                userContext.MarketplaceID
            );

            var buyer = Eq(
                Constants.KnownSearchAttributes.Buyers,
                userContext.CompanyID
            );

            WidgetSearchFilter? supplier = null;
            if (!string.IsNullOrWhiteSpace(sellerId))
            {
                supplier = Eq(Constants.KnownSearchAttributes.Suppliers, sellerId);
            }

            // buyer OR user groups
            var partyFilters = new List<WidgetSearchFilter> { buyer };

            if (userContext.Groups is { Count: > 0 })
            {
                foreach (var groupId in userContext.Groups)
                {
                    partyFilters.Add(Eq(Constants.KnownSearchAttributes.UserGroups, groupId));
                }
            }

            var partyOr = Or(partyFilters);

            // Final: AND( active, marketplace, [supplier?], OR(buyer, userGroups...) )
            var andOperands = new List<WidgetSearchFilter> { active, marketplaceField, partyOr };
            if (supplier is not null) andOperands.Insert(2, supplier); // keep stable order

            return And(andOperands);
        }

        // ---------- Small helpers for building filter trees ----------

        protected static WidgetSearchFilter Eq(string name, object? value) => new()
        {
            Name = name,
            TypeValue = "eq",
            Value = value
        };

        protected static WidgetSearchFilter And(params WidgetSearchFilter[] filters) => new()
        {
            TypeValue = "and",
            Filters = filters
        };

        protected static WidgetSearchFilter And(IEnumerable<WidgetSearchFilter> filters) => new()
        {
            TypeValue = "and",
            Filters = filters.ToArray()
        };

        protected static WidgetSearchFilter Or(IEnumerable<WidgetSearchFilter> filters) => new()
        {
            TypeValue = "or",
            Filters = filters.ToArray()
        };

        private static SitecoreSearchClientRequest? DeepClone(OrderCloudSearchRequest original)
        {
            var json = JsonConvert.SerializeObject(original);
            return JsonConvert.DeserializeObject<SitecoreSearchClientRequest>(json);
        }
    }
}

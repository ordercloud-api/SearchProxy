
using Newtonsoft.Json.Linq;
using OrderCloud.Catalyst;
using OrderCloud.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SearchProxy.Common
{
    public interface ISearchResponseMapper
    {
        JObject Map(JObject originalResponse, DecodedUserInfoToken context, OrderCloudExtensions? orderCloudExtensions = null);
    }

    public class SearchResponseMapper : ISearchResponseMapper
    {
        public JObject Map(JObject originalResponse, DecodedUserInfoToken userContext, OrderCloudExtensions? orderCloudExtensions = null)
        {
            if (originalResponse is null) throw new ArgumentNullException(nameof(originalResponse));
            if (userContext is null) throw new ArgumentNullException(nameof(userContext));

            var sellerId = orderCloudExtensions?.SellerId;

            var widgets = originalResponse["widgets"] as JArray;
            if (widgets is null || widgets.Count == 0) return originalResponse;

            foreach (var widget in widgets)
            {
                var entity = widget["entity"]?.Value<string>();
                var content = widget["content"] as JArray;
                if (!string.Equals(entity, "product", StringComparison.OrdinalIgnoreCase) || content is null)
                    continue;

                foreach (var item in content)
                {
                    var priceSchedule = FindPriceSchedule(userContext, item, sellerId);
                    if (priceSchedule is not null)
                    {
                        item["priceschedule"] = priceSchedule;
                        item["priceschedule"]!["isonsale"] = CalculateIsOnSale(priceSchedule);
                    }

                    if (orderCloudExtensions is not null)
                    {
                        ProcessOrderCloudExtensions(item, orderCloudExtensions);
                    }

                    // Prune fields not intended for client exposure
                    RemoveKey(item, "buyers");
                    RemoveKey(item, "suppliers");
                    RemoveKey(item, "defaultpriceschedule");
                    RemoveKey(item, "sellerdefaultpriceschedules");
                    RemoveKey(item, "partypriceschedules");
                }
            }

            return originalResponse;
        }

        /// <summary>Remove a field by key if present.</summary>
        public static void RemoveKey(JToken item, string key)
        {
            var token = item[key];
            token?.Parent?.Remove();
        }

        /// <summary>
        /// Resolve the correct price schedule respecting the user context (currency, party memberships)
        /// and optionally restricting by sellerId.
        /// </summary>
        public JToken? FindPriceSchedule(DecodedUserInfoToken userContext, JToken item, string? sellerId = null)
        {
            var ownerId = GetString(item["ownerid"]);
            var defaultPriceScheduleContainer = item["defaultpriceschedule"];
            var defaultPriceSchedule = defaultPriceScheduleContainer?["priceschedule"];

            // Currency gate for default PS (don't use default if currency mismatches)
            if (defaultPriceSchedule is not null)
            {
                var defaultCurrency = GetString(defaultPriceSchedule["currency"]);
                if (!CurrencyMatches(defaultCurrency, userContext.Currency))
                {
                    defaultPriceScheduleContainer?.Parent?.Remove();
                    defaultPriceSchedule = null;
                }
            }

            var sellerDefaultPriceSchedules = item["sellerdefaultpriceschedules"] as JArray;
            var partyPriceSchedules = item["partypriceschedules"] as JArray;

            // Party-based schedules that match the user's membership and currency
            var matchingPartySchedules =
                partyPriceSchedules?.Where(pps =>
                    pps["party"] is not null &&
                    pps["partytype"] is not null &&
                    IsInPartyList(userContext, pps["party"]!.Value<string>()!, pps["partytype"]!.Value<long>()) &&
                    IsPriceScheduleCurrencyMatch(pps["priceschedule"], userContext.Currency))
                .ToList() ?? new List<JToken>();

            // If sellerId is provided, prefer seller-scoped schedules; otherwise, use owner-scoped schedules
            if (!string.IsNullOrEmpty(sellerId))
            {
                var defaultSellerSchedule =
                    sellerDefaultPriceSchedules?
                        .Where(dps =>
                            string.Equals(GetString(dps["seller"]), sellerId, StringComparison.OrdinalIgnoreCase) &&
                            IsPriceScheduleCurrencyMatch(dps["priceschedule"], userContext.Currency))
                        .Select(dps => dps["priceschedule"])
                        .FirstOrDefault();

                var sellerPartySchedule =
                    matchingPartySchedules
                        .Where(pps =>
                            string.Equals(GetString(pps["seller"]), sellerId, StringComparison.OrdinalIgnoreCase) &&
                            IsPriceScheduleCurrencyMatch(pps["priceschedule"], userContext.Currency))
                        .Select(pps => pps["priceschedule"])
                        .FirstOrDefault();

                return sellerPartySchedule ?? defaultSellerSchedule ?? defaultPriceSchedule;
            }
            else
            {
                // No explicit seller: prefer schedules owned by product owner, then default
                var ownerPartySchedule =
                    matchingPartySchedules
                        .Where(pps =>
                            string.Equals(GetString(pps["seller"]), ownerId, StringComparison.OrdinalIgnoreCase) &&
                            IsPriceScheduleCurrencyMatch(pps["priceschedule"], userContext.Currency))
                        .Select(pps => pps["priceschedule"])
                        .FirstOrDefault();

                var resolved = ownerPartySchedule ?? defaultPriceSchedule;
                if (resolved is not null) return resolved;
            }

            // Fallback: any matching party schedule by currency, else default
            var anyPartyByCurrency =
                partyPriceSchedules?
                    .Where(pps => IsPriceScheduleCurrencyMatch(pps["priceschedule"], userContext.Currency))
                    .Select(pps => pps["priceschedule"])
                    .FirstOrDefault();

            return anyPartyByCurrency ?? defaultPriceSchedule;
        }

        /// <summary>Check if the user is in the referenced party list.</summary>
        protected bool IsInPartyList(DecodedUserInfoToken userContext, string partyId, long partyType)
        {
            var enumPartyType = (PartyType)partyType - 1;
            return enumPartyType switch
            {
                PartyType.Company => string.Equals(userContext.CompanyID, partyId, StringComparison.OrdinalIgnoreCase),
                PartyType.Group => userContext.Groups?.Contains(partyId, StringComparer.OrdinalIgnoreCase) == true,
                _ => throw new ArgumentOutOfRangeException(nameof(partyType), $"Unsupported party type: {partyType}")
            };
        }

        /// <summary>True if any salePrice exists and now is within optional start/end window.</summary>
        protected bool CalculateIsOnSale(JToken? priceSchedule)
        {
            if (priceSchedule is null) return false;

            var priceBreaks = priceSchedule["pricebreaks"] as JArray;
            var hasSalePrice = priceBreaks?.Any(pb => pb["salePrice"] is not null && pb["salePrice"]!.Type != JTokenType.Null) == true;
            if (!hasSalePrice) return false;

            var nowUtc = DateTimeOffset.UtcNow;
            var hasStart = TryGetDate(priceSchedule["salestart"], out var saleStart);
            var hasEnd = TryGetDate(priceSchedule["saleend"], out var saleEnd);

            // If no bounds, sale is active
            if (!hasStart && !hasEnd) return true;

            var startsOk = !hasStart || saleStart <= nowUtc;
            var endsOk = !hasEnd || saleEnd > nowUtc;
            return startsOk && endsOk;
        }

        /// <summary>Case-insensitive currency match with trimming and null handling.</summary>
        public bool CurrencyMatches(string? currency1, string? currency2)
        {
            var a = NormalizeStringForComparison(currency1);
            var b = NormalizeStringForComparison(currency2);
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        public static string? NormalizeStringForComparison(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        protected void ProcessOrderCloudExtensions(JToken item, OrderCloudExtensions orderCloudExtensions)
        {
            var required = orderCloudExtensions.RequiredInventoryLocations;
            var inventoryRecords = item["inventoryrecords"] as JArray;

            if (required is null || required.Count == 0 || inventoryRecords is null || inventoryRecords.Count == 0)
                return;

            // Keep only required inventory locations:
            // remove if addressId is null/empty OR addressId is NOT in required.
            var toRemove = inventoryRecords
                .Where(inv =>
                {
                    var addressId = GetString(inv["addressid"]);
                    return string.IsNullOrEmpty(addressId) || !required.Contains(addressId);
                })
                .ToList();

            foreach (var r in toRemove) r.Remove();
        }

        // ----------------------------
        // Helper methods
        // ----------------------------

        private static string? GetString(JToken? token) => token?.Type == JTokenType.Null ? null : token?.Value<string>();

        private static bool TryGetDate(JToken? token, out DateTimeOffset value)
        {
            value = default;
            if (token is null || token.Type == JTokenType.Null) return false;

            // Support JValue DateTimeOffset or parse string
            if (token.Type == JTokenType.Date)
            {
                value = token.Value<DateTimeOffset>();
                return true;
            }
            var s = token.Value<string>();
            return DateTimeOffset.TryParse(s, out value);
        }

        private bool IsPriceScheduleCurrencyMatch(JToken? priceScheduleToken, string? expectedCurrency)
        {
            if (priceScheduleToken is null || priceScheduleToken.Type == JTokenType.Null) return false;
            var currency = GetString(priceScheduleToken["currency"]);
            return CurrencyMatches(currency, expectedCurrency);
        }
    }
}
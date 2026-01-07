
using Newtonsoft.Json.Linq;
using OrderCloud.Catalyst;
using System;
using Xunit;

namespace SearchProxy.Common.Tests
{
    public class SearchResponseMapper_CurrencyTests
    {
        private readonly SearchResponseMapper _sut = new SearchResponseMapper();

        // ------------------------------------------------------------
        // 1) Default + party exist, but none match user's currency
        // ------------------------------------------------------------
        [Fact]
        public void NoSchedulesMatchUserCurrency_NoPriceScheduleSelected_DefaultPruned()
        {
            // User currency is USD; all schedules are EUR
            var context = Ctx(currency: "USD");

            var defaultPs_Eur = PriceSchedule("EUR");
            var partyPs_Eur = PriceSchedule("EUR");

            var product = BuildProduct(
                defaultPriceSchedule: defaultPs_Eur,
                partyPriceSchedules: new JArray(
                    PartySchedule(partyId: "SELLER_A", partyType: 3, sellerId: "SELLER_A", priceschedule: partyPs_Eur)
                )
            );

            var response = BuildResponseWithProduct(product);

            // Act
            var result = _sut.Map(response, context);

            // Assert (end-to-end)
            var item = result["widgets"]![0]!["content"]![0]!;
            // Since no match, FindPriceSchedule returns null and we do NOT set item["priceschedule"]
            Assert.Null(item["priceschedule"]);

            // Default PS is pruned when its currency mismatches
            Assert.Null(item["defaultpriceschedule"]);

            // Fields not intended for client exposure are pruned
            Assert.Null(item["buyers"]);
            Assert.Null(item["suppliers"]);
            Assert.Null(item["sellerdefaultpriceschedules"]);
            Assert.Null(item["partypriceschedules"]);
        }

        // ------------------------------------------------------------
        // 2) Only some schedules match (second party schedule matches)
        // ------------------------------------------------------------
        [Fact]
        public void PartySchedulesOnlySecondMatchesCurrency_SelectsSecondParty()
        {
            var context = Ctx(currency: "USD");

            var defaultPs_Eur = PriceSchedule("EUR");
            var partyPs1_Eur = PriceSchedule("EUR");
            var partyPs2_Usd = PriceSchedule("USD");

            var product = BuildProduct(
                defaultPriceSchedule: defaultPs_Eur,
                partyPriceSchedules: new JArray(
                    PartySchedule(partyId: "SELLER_A", partyType: 3, sellerId: "SELLER_A", priceschedule: partyPs1_Eur),
                    PartySchedule(partyId: "SELLER_A", partyType: 3, sellerId: "SELLER_A", priceschedule: partyPs2_Usd)
                )
            );

            var response = BuildResponseWithProduct(product);

            // Act
            var result = _sut.Map(response, context);

            // Assert: the mapper should select the USD party PS
            var item = result["widgets"]![0]!["content"]![0]!;
            var selected = item["priceschedule"];
            Assert.NotNull(selected);
            Assert.Equal("USD", selected!["currency"]!.Value<string>());

            // Pruning should occur
            Assert.Null(item["defaultpriceschedule"]);
            Assert.Null(item["sellerdefaultpriceschedules"]);
            Assert.Null(item["partypriceschedules"]);
        }

        // ------------------------------------------------------------
        // 3) No currency defined (user or schedule)
        //    - null == null => match (Normalize -> null, Equals(null,null) => true)
        // ------------------------------------------------------------
        [Fact]
        public void UserCurrencyNull_AndScheduleCurrencyNull_DefaultMatches()
        {
            var context = Ctx(currency: null);

            var defaultPs_Null = PriceSchedule(null);
            var product = BuildProduct(defaultPriceSchedule: defaultPs_Null);
            var response = BuildResponseWithProduct(product);

            // Act
            var result = _sut.Map(response, context);

            // Assert: default should be selected since null == null matches
            var item = result["widgets"]![0]!["content"]![0]!;
            var selected = item["priceschedule"];
            Assert.NotNull(selected);
            Assert.True(selected!["currency"] == null || selected!["currency"]!.Type == JTokenType.Null);

            // Default container is removed by pruning, but the selected PS is copied into item["priceschedule"]
            Assert.Null(item["defaultpriceschedule"]);
        }

        [Fact]
        public void UserCurrencyNull_ButScheduleHasCurrency_DefaultDoesNotMatch()
        {
            var context = Ctx(currency: null);

            var defaultPs_Usd = PriceSchedule("USD");
            var product = BuildProduct(defaultPriceSchedule: defaultPs_Usd);
            var response = BuildResponseWithProduct(product);

            // Act
            var result = _sut.Map(response, context);

            // Assert: no match -> no selected priceschedule, default removed
            var item = result["widgets"]![0]!["content"]![0]!;
            Assert.Null(item["priceschedule"]);
            Assert.Null(item["defaultpriceschedule"]);
        }

        [Fact]
        public void UserCurrencyDefined_ButScheduleCurrencyNull_DefaultDoesNotMatch()
        {
            var context = Ctx(currency: "USD");

            var defaultPs_Null = PriceSchedule(null);
            var product = BuildProduct(defaultPriceSchedule: defaultPs_Null);
            var response = BuildResponseWithProduct(product);

            // Act
            var result = _sut.Map(response, context);

            // Assert: no match -> no selected priceschedule, default removed
            var item = result["widgets"]![0]!["content"]![0]!;
            Assert.Null(item["priceschedule"]);
            Assert.Null(item["defaultpriceschedule"]);
        }

        [Fact]
        public void WithSellerId_PrefersSellerDefaultMatchingCurrency()
        {
            var context = Ctx(currency: "USD");

            var sellerB = "SELLER_B";

            var sellerDefaultPs_UsdContainer = new JObject
            {
                ["seller"] = sellerB,
                ["priceschedule"] = PriceSchedule("USD")
            };

            var product = BuildProduct(
                defaultPriceSchedule: PriceSchedule("EUR"),
                sellerDefaultPriceSchedules: new JArray(sellerDefaultPs_UsdContainer),
                ownerId: "SELLER_A"
            );

            var response = BuildResponseWithProduct(product);

            // Provide orderCloudExtensions with SellerId to drive seller-scoped branch
            var ocExt = new OrderCloudExtensions { SellerId = sellerB };

            // Act
            var result = _sut.Map(response, context, ocExt);

            // Assert: seller default USD PS selected
            var item = result["widgets"]![0]!["content"]![0]!;
            var selected = item["priceschedule"];
            Assert.NotNull(selected);
            Assert.Equal("USD", selected!["currency"]!.Value<string>());

            // Pruning performed
            Assert.Null(item["defaultpriceschedule"]);
            Assert.Null(item["sellerdefaultpriceschedules"]);
            Assert.Null(item["partypriceschedules"]);
        }

        // ------------------------------------------------------------
        // Helpers (minimal, focused on public Map end-to-end)
        // ------------------------------------------------------------

        private static DecodedUserInfoToken Ctx(string? currency = null, string? companyId = "SELLER_A", string[]? groups = null)
            => MockUserInfoToken.BuildContext(currency, companyId, groups);

        private static JObject PriceSchedule(string? currency, params (string field, JToken value)[] extra)
        {
            var ps = new JObject
            {
                ["currency"] = currency is null ? JValue.CreateNull() : new JValue(currency),
                ["pricebreaks"] = new JArray { new JObject { ["price"] = 10 } }
            };
            foreach (var (field, value) in extra) ps[field] = value;
            return ps;
        }

        private static JObject PartySchedule(
            string partyId,
            long partyType,
            string? sellerId,
            JObject priceschedule)
        {
            var o = new JObject
            {
                ["party"] = partyId,
                ["partytype"] = partyType,
                ["priceschedule"] = priceschedule
            };
            if (sellerId is not null) o["seller"] = sellerId;
            return o;
        }

        private static JObject BuildResponseWithProduct(JObject product)
        {
            return new JObject
            {
                ["widgets"] = new JArray
                {
                    new JObject
                    {
                        ["entity"] = "product",
                        ["content"] = new JArray { product }
                    }
                }
            };
        }

        private static JObject BuildProduct(
            JObject? defaultPriceSchedule = null,
            JArray? partyPriceSchedules = null,
            JArray? sellerDefaultPriceSchedules = null,
            string ownerId = "SELLER_A")
        {
            var item = new JObject
            {
                ["ownerid"] = ownerId,
            };

            if (defaultPriceSchedule is not null)
            {
                item["defaultpriceschedule"] = new JObject
                {
                    ["priceschedule"] = defaultPriceSchedule
                };
            }

            if (partyPriceSchedules is not null)
            {
                item["partypriceschedules"] = partyPriceSchedules;
            }

            if (sellerDefaultPriceSchedules is not null)
            {
                item["sellerdefaultpriceschedules"] = sellerDefaultPriceSchedules;
            }

            // include fields that are pruned to verify pruning remains stable
            item["buyers"] = new JArray();
            item["suppliers"] = new JArray();

            return item;
        }
    }
}
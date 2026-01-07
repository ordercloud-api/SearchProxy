
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OrderCloud.Catalyst;
using SearchProxy.Common;
using Xunit;

namespace SearchProxy.Common.Tests
{
    public class SearchResponseMapper_OrderCloudExtensionsTests
    {
        private readonly SearchResponseMapper _sut = new SearchResponseMapper();

        // -----------------------
        // Tests: RequiredInventoryLocations filtering
        // -----------------------

        [Fact]
        public void Filters_Inventory_To_RequiredLocations_Only()
        {
            var ext = OcExt(requiredLocations: new[] { "LOC_A", "LOC_C" }); // LOC_C absent, LOC_A present
            var product = Product(
                ownerId: "SELLER_X",
                inventoryRecords: Inventory("LOC_A", "LOC_B", null, "")
            );
            var response = ResponseWithProduct(product);

            var result = _sut.Map(response, Ctx(), ext);

            var item = SelectedItemFromMap(result);
            var inv = (JArray)item["inventoryrecords"]!;
            Assert.Single(inv);
            Assert.Equal("LOC_A", (string)inv[0]!["addressid"]!);

            // unrelated server-only fields should still be pruned by Map
            Assert.Null(item["buyers"]);
            Assert.Null(item["suppliers"]);
        }

        [Fact]
        public void Filters_Inventory_To_RequiredLocations_Only_Multiple()
        {
            var ext = OcExt(requiredLocations: new[] { "LOC_A", "LOC_B" });
            var product = Product(
                ownerId: "SELLER_X",
                inventoryRecords: Inventory("LOC_A", "LOC_B", null, "")
            );
            var response = ResponseWithProduct(product);

            var result = _sut.Map(response, Ctx(), ext);

            var item = SelectedItemFromMap(result);
            var inv = (JArray)item["inventoryrecords"]!;
            Assert.Equal(2, inv.Count);
            Assert.Equal("LOC_A", (string)inv[0]!["addressid"]!);
            Assert.Equal("LOC_B", (string)inv[1]!["addressid"]!);

            // unrelated server-only fields should still be pruned by Map
            Assert.Null(item["buyers"]);
            Assert.Null(item["suppliers"]);
        }

        [Fact]
        public void DoesNotFilter_When_RequiredLocations_Empty()
        {
            var ext = OcExt(requiredLocations: Array.Empty<string>());
            var product = Product(
                ownerId: "SELLER_X",
                inventoryRecords: Inventory("LOC_A", "LOC_B", null, "")
            );
            var response = ResponseWithProduct(product);

            var result = _sut.Map(response, Ctx(), ext);

            var item = SelectedItemFromMap(result);
            var inv = (JArray)item["inventoryrecords"]!;
            Assert.Equal(4, inv.Count);
        }

        [Fact]
        public void DoesNotFilter_When_RequiredLocations_Null()
        {
            var ext = OcExt(requiredLocations: null);
            var product = Product(
                ownerId: "SELLER_X",
                inventoryRecords: Inventory("LOC_A", "LOC_B")
            );
            var response = ResponseWithProduct(product);

            var result = _sut.Map(response, Ctx(), ext);

            var item = SelectedItemFromMap(result);
            var inv = (JArray)item["inventoryrecords"]!;
            Assert.Equal(2, inv.Count);
        }

        [Fact]
        public void DoesNothing_When_InventoryRecords_Missing_Or_Empty()
        {
            // Missing inventoryrecords
            var ext = OcExt(requiredLocations: new[] { "LOC_A" });
            var productMissing = Product(ownerId: "SELLER_X");
            var responseMissing = ResponseWithProduct(productMissing);

            var resultMissing = _sut.Map(responseMissing, Ctx(), ext);
            var itemMissing = SelectedItemFromMap(resultMissing);
            Assert.Null(itemMissing["inventoryrecords"]);

            // Empty inventoryrecords
            var productEmpty = Product(ownerId: "SELLER_X", inventoryRecords: new JArray());
            var responseEmpty = ResponseWithProduct(productEmpty);

            var resultEmpty = _sut.Map(responseEmpty, Ctx(), ext);
            var itemEmpty = SelectedItemFromMap(resultEmpty);
            var inv = (JArray)itemEmpty["inventoryrecords"]!;
            Assert.Empty(inv);
        }

        // -----------------------
        // Tests: SellerId-driven price schedule selection via OrderCloudExtensions
        // -----------------------

        [Fact]
        public void WithSellerId_Prefers_SellerParty_Then_DefaultSeller_Then_Default()
        {
            var ext = OcExt(sellerId: "SELLER_X");

            var usdPartySeller = PriceSchedule("USD", PB_WithSale(8)); // sale present; no bounds
            var usdDefaultSeller = PriceSchedule("USD");
            var usdDefault = PriceSchedule("USD", PB_WithSale(8));

            var product = Product(
                ownerId: "SELLER_X",
                defaultPriceSchedule: usdDefault,
                sellerDefaultPriceSchedules: new JArray
                {
                    SellerDefaultPS("SELLER_X", usdDefaultSeller)
                },
                partyPriceSchedules: new JArray
                {
                    // partytype=3 => Company; user CompanyID=BUYER_A; seller must match SELLER_X
                    PartyPS("SELLER_X", "BUYER_A", partyType: 3, priceSchedule: usdPartySeller)
                }
            );
            var response = ResponseWithProduct(product);

            var result = _sut.Map(response, Ctx(currency: "USD", companyId: "BUYER_A"), ext);

            var item = SelectedItemFromMap(result);
            var selected = (JObject)item["priceschedule"]!;

            Assert.True(JToken.DeepEquals(usdPartySeller["pricebreaks"], selected["pricebreaks"]));
            Assert.Equal("USD", (string)selected["currency"]!);
            Assert.True((bool)selected["isonsale"]!);

            // containers pruned
            Assert.Null(item["defaultpriceschedule"]);
            Assert.Null(item["buyers"]);
            Assert.Null(item["suppliers"]);
        }

        [Fact]
        public void WithSellerId_Uses_DefaultSeller_When_NoPartyMatch()
        {
            var ext = OcExt(sellerId: "SELLER_X");

            var usdDefaultSeller = PriceSchedule("USD", PB_WithSale(8));
            var usdDefault = PriceSchedule("USD");

            var product = Product(
                ownerId: "SELLER_X",
                defaultPriceSchedule: usdDefault,
                sellerDefaultPriceSchedules: new JArray
                {
                    SellerDefaultPS("SELLER_X", usdDefaultSeller)
                },
                partyPriceSchedules: new JArray
                {
                    // Party schedule for different seller should not qualify
                    PartyPS("SELLER_Y", "BUYER_A", partyType: 3, priceSchedule: PriceSchedule("USD"))
                }
            );
            var response = ResponseWithProduct(product);

            var result = _sut.Map(response, Ctx(currency: "USD", companyId: "BUYER_A"), ext);

            var item = SelectedItemFromMap(result);
            var selected = (JObject)item["priceschedule"]!;

            Assert.True(JToken.DeepEquals(usdDefaultSeller["pricebreaks"], selected["pricebreaks"]));
            Assert.Equal("USD", (string)selected["currency"]!);
            Assert.True((bool)selected["isonsale"]!);

            Assert.Null(item["defaultpriceschedule"]);
        }

        [Fact]
        public void WithSellerId_Removes_DefaultPS_On_CurrencyMismatch_And_FallsBack_To_SellerDefault()
        {
            var ext = OcExt(sellerId: "SELLER_X");

            var eurDefault = PriceSchedule("EUR"); // mismatched currency
            var usdSellerDefault = PriceSchedule("USD", PB_WithSale(8));

            var product = Product(
                ownerId: "SELLER_X",
                defaultPriceSchedule: eurDefault,
                sellerDefaultPriceSchedules: new JArray
                {
                    SellerDefaultPS("SELLER_X", usdSellerDefault)
                }
            );
            var response = ResponseWithProduct(product);

            var result = _sut.Map(response, Ctx(currency: "USD", companyId: "BUYER_A"), ext);

            var item = SelectedItemFromMap(result);

            // defaultpriceschedule container pruned because currency mismatch
            Assert.Null(item["defaultpriceschedule"]);

            var selected = (JObject)item["priceschedule"]!;
            Assert.True(JToken.DeepEquals(usdSellerDefault["pricebreaks"], selected["pricebreaks"]));
            Assert.Equal("USD", (string)selected["currency"]!);
            Assert.True((bool)selected["isonsale"]!);
        }

        [Fact]
        public void WithoutSellerId_Prefers_OwnerParty_Schedule_Otherwise_Default()
        {
            // No SellerId provided in extensions; selection should prefer owner-scoped party schedule
            var ownerId = "SELLER_X";
            var usdDefault = PriceSchedule("USD");
            var usdOwnerParty = PriceSchedule("USD", PB_WithSale(8));

            var product = Product(
                ownerId: ownerId,
                defaultPriceSchedule: usdDefault,
                partyPriceSchedules: new JArray
                {
                    PartyPS(ownerId, "BUYER_A", partyType: 3, priceSchedule: usdOwnerParty)
                }
            );
            var response = ResponseWithProduct(product);

            var result = _sut.Map(response, Ctx(currency: "USD", companyId: "BUYER_A"), orderCloudExtensions: null);

            var item = SelectedItemFromMap(result);
            var selected = (JObject)item["priceschedule"]!;

            Assert.True(JToken.DeepEquals(usdOwnerParty["pricebreaks"]!, selected["pricebreaks"]!));
            Assert.Equal("USD", (string)selected["currency"]!);
            Assert.True((bool)selected["isonsale"]!);

            Assert.Null(item["defaultpriceschedule"]);
        }

        // -----------------------
        // Helpers (structured similarly to your SalePrice tests)
        // -----------------------

        private static DecodedUserInfoToken Ctx(string? currency = "USD", string? companyId = "BUYER_A", string[]? groups = null)
            => MockUserInfoToken.BuildContext(currency, companyId, groups);

        private static OrderCloudExtensions OcExt(string? sellerId = null, IEnumerable<string>? requiredLocations = null)
            => new OrderCloudExtensions
            {
                SellerId = sellerId,
                RequiredInventoryLocations = requiredLocations?.ToList() ?? new List<string>()
            };

        private static JObject Product(
            string ownerId = "SELLER_A",
            JArray? inventoryRecords = null,
            JObject? defaultPriceSchedule = null,
            JArray? sellerDefaultPriceSchedules = null,
            JArray? partyPriceSchedules = null)
        {
            var product = new JObject
            {
                ["ownerid"] = ownerId,

                // include these to confirm pruning by Map still happens,
                // even though not the main focus here
                ["buyers"] = new JArray(),
                ["suppliers"] = new JArray()
            };

            if (inventoryRecords is not null)
                product["inventoryrecords"] = inventoryRecords;

            if (defaultPriceSchedule is not null)
                product["defaultpriceschedule"] = new JObject { ["priceschedule"] = defaultPriceSchedule };

            if (sellerDefaultPriceSchedules is not null)
                product["sellerdefaultpriceschedules"] = sellerDefaultPriceSchedules;

            if (partyPriceSchedules is not null)
                product["partypriceschedules"] = partyPriceSchedules;

            return product;
        }

        private static JObject ResponseWithProduct(JObject product)
            => new JObject
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

        private static JToken SelectedItemFromMap(JObject result)
            => result["widgets"]![0]!["content"]![0]!;

        private static JObject PriceSchedule(string? currency, JArray? priceBreaks = null, params (string field, JToken value)[] extra)
        {
            var ps = new JObject
            {
                ["currency"] = currency is null ? JValue.CreateNull() : new JValue(currency),
                ["pricebreaks"] = priceBreaks ?? new JArray { new JObject { ["price"] = 10 } }
            };
            foreach (var (field, value) in extra)
                ps[field] = value;
            return ps;
        }

        private static JArray PB_WithSale(decimal sale, decimal price = 10m)
            => new JArray { new JObject { ["price"] = price, ["salePrice"] = sale } };

        private static JArray Inventory(params string?[] addressIds)
            => new JArray(addressIds.Select(id =>
                new JObject { ["addressid"] = id is null ? JValue.CreateNull() : new JValue(id) }));

        private static JObject SellerDefaultPS(string seller, JObject priceSchedule)
            => new JObject
            {
                ["seller"] = seller,
                ["priceschedule"] = priceSchedule
            };

        /// <param name="partyType">Use 3 for Company, 2 for Group. Mapper supports Company/Group after internal offset.</param>
        private static JObject PartyPS(string seller, string partyId, long partyType, JObject priceSchedule)
            => new JObject
            {
                ["seller"] = seller,
                ["party"] = partyId,
                ["partytype"] = partyType,
                ["priceschedule"] = priceSchedule
            };
    }
}
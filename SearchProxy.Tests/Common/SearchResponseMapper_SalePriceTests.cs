
using System;
using Newtonsoft.Json.Linq;
using OrderCloud.Catalyst;
using SearchProxy.Common;
using Xunit;

namespace SearchProxy.Common.Tests
{

    public class SearchResponseMapper_SalePriceTests
    {
        private readonly SearchResponseMapper _sut = new SearchResponseMapper();

        [Fact]
        public void Sale_True_WhenSalePriceExists_And_NoDates()
        {
            var context = Ctx(currency: "USD");
            var ps = PriceSchedule("USD", PB_WithSale(8));
            var response = ResponseWithProduct(ProductWithDefaultPS(ps));

            var result = _sut.Map(response, context);

            var item = SelectedItemFromMap(result);
            Assert.NotNull(item["priceschedule"]);
            Assert.Equal(true, (bool?)item["priceschedule"]!["isonsale"]);

            // containers pruned
            Assert.Null(item["defaultpriceschedule"]);
            Assert.Null(item["buyers"]);
            Assert.Null(item["suppliers"]);
        }

        [Fact]
        public void Sale_False_WhenPriceScheduleIsNullOrMissing()
        {
            var context = Ctx("USD");

            // No defaultpriceschedule at all
            var product = new JObject { ["ownerid"] = "SELLER_A" };
            var response = ResponseWithProduct(product);

            var result = _sut.Map(response, context);

            var item = SelectedItemFromMap(result);
            Assert.Null(item["priceschedule"]);
            Assert.Null(item["defaultpriceschedule"]);
        }

        [Fact]
        public void Sale_False_WhenNoPriceBreaks()
        {
            var context = Ctx("USD");
            var ps = PriceSchedule("USD", priceBreaks: new JArray()); // no price breaks
            var response = ResponseWithProduct(ProductWithDefaultPS(ps));

            var result = _sut.Map(response, context);

            var item = SelectedItemFromMap(result);
            Assert.NotNull(item["priceschedule"]);
            Assert.Equal(false, (bool?)item["priceschedule"]!["isonsale"]);
        }

        [Fact]
        public void Sale_False_WhenAllSalePricesMissingOrNull()
        {
            var context = Ctx("USD");

            var noSale = new JObject { ["price"] = 10 };
            var nullSale = new JObject { ["price"] = 10, ["salePrice"] = JValue.CreateNull() };
            var ps = PriceSchedule("USD", new JArray { noSale, nullSale });

            var response = ResponseWithProduct(ProductWithDefaultPS(ps));
            var result = _sut.Map(response, context);

            var item = SelectedItemFromMap(result);
            Assert.NotNull(item["priceschedule"]);
            Assert.Equal(false, (bool?)item["priceschedule"]!["isonsale"]);
        }

        [Fact]
        public void Sale_True_WhenAnySalePriceExists()
        {
            var context = Ctx("USD");

            var mixedBreaks = new JArray
            {
                new JObject { ["price"] = 10 }, // no sale
                new JObject { ["price"] = 20, ["salePrice"] = 15 } // sale
            };
            var ps = PriceSchedule("USD", mixedBreaks);

            var response = ResponseWithProduct(ProductWithDefaultPS(ps));
            var result = _sut.Map(response, context);

            var item = SelectedItemFromMap(result);
            Assert.NotNull(item["priceschedule"]);
            Assert.Equal(true, (bool?)item["priceschedule"]!["isonsale"]);
        }

        [Fact]
        public void Sale_StartOnly_PastOrEqualNow_IsTrue_Future_IsFalse()
        {
            var now = DateTimeOffset.UtcNow;
            var context = Ctx("USD");

            // Past
            var pastPS = PriceSchedule("USD", PB_WithSale(8), ("salestart", new JValue(now.AddMinutes(-5))));
            var resultPast = _sut.Map(ResponseWithProduct(ProductWithDefaultPS(pastPS)), context);
            var itemPast = SelectedItemFromMap(resultPast);
            Assert.Equal(true, (bool?)itemPast["priceschedule"]!["isonsale"]);

            // Equal (inclusive)
            var equalPS = PriceSchedule("USD", PB_WithSale(8), ("salestart", new JValue(now)));
            var resultEqual = _sut.Map(ResponseWithProduct(ProductWithDefaultPS(equalPS)), context);
            var itemEqual = SelectedItemFromMap(resultEqual);
            Assert.Equal(true, (bool?)itemEqual["priceschedule"]!["isonsale"]);

            // Future
            var futurePS = PriceSchedule("USD", PB_WithSale(8), ("salestart", new JValue(now.AddMinutes(5))));
            var resultFuture = _sut.Map(ResponseWithProduct(ProductWithDefaultPS(futurePS)), context);
            var itemFuture = SelectedItemFromMap(resultFuture);
            Assert.Equal(false, (bool?)itemFuture["priceschedule"]!["isonsale"]);
        }

        [Fact]
        public void Sale_EndOnly_Future_IsTrue_PastOrEqualNow_IsFalse()
        {
            var now = DateTimeOffset.UtcNow;
            var context = Ctx("USD");

            // Future
            var futurePS = PriceSchedule("USD", PB_WithSale(8), ("saleend", new JValue(now.AddMinutes(5))));
            var resultFuture = _sut.Map(ResponseWithProduct(ProductWithDefaultPS(futurePS)), context);
            var itemFuture = SelectedItemFromMap(resultFuture);
            Assert.Equal(true, (bool?)itemFuture["priceschedule"]!["isonsale"]);

            // Past
            var pastPS = PriceSchedule("USD", PB_WithSale(8), ("saleend", new JValue(now.AddMinutes(-5))));
            var resultPast = _sut.Map(ResponseWithProduct(ProductWithDefaultPS(pastPS)), context);
            var itemPast = SelectedItemFromMap(resultPast);
            Assert.Equal(false, (bool?)itemPast["priceschedule"]!["isonsale"]);

            // Equal (exclusive)
            var equalPS = PriceSchedule("USD", PB_WithSale(8), ("saleend", new JValue(now)));
            var resultEqual = _sut.Map(ResponseWithProduct(ProductWithDefaultPS(equalPS)), context);
            var itemEqual = SelectedItemFromMap(resultEqual);
            Assert.Equal(false, (bool?)itemEqual["priceschedule"]!["isonsale"]);
        }

        [Fact]
        public void Sale_Window_Inside_IsTrue_BeforeOrAfter_IsFalse()
        {
            var now = DateTimeOffset.UtcNow;
            var context = Ctx("USD");

            // Inside window
            var insidePS = PriceSchedule("USD", PB_WithSale(8),
                ("salestart", new JValue(now.AddMinutes(-10))),
                ("saleend", new JValue(now.AddMinutes(10))));
            var resultInside = _sut.Map(ResponseWithProduct(ProductWithDefaultPS(insidePS)), context);
            var itemInside = SelectedItemFromMap(resultInside);
            Assert.Equal(true, (bool?)itemInside["priceschedule"]!["isonsale"]);

            // Before window
            var beforePS = PriceSchedule("USD", PB_WithSale(8),
                ("salestart", new JValue(now.AddMinutes(10))),
                ("saleend", new JValue(now.AddMinutes(20))));
            var resultBefore = _sut.Map(ResponseWithProduct(ProductWithDefaultPS(beforePS)), context);
            var itemBefore = SelectedItemFromMap(resultBefore);
            Assert.Equal(false, (bool?)itemBefore["priceschedule"]!["isonsale"]);

            // After window
            var afterPS = PriceSchedule("USD", PB_WithSale(8),
                ("salestart", new JValue(now.AddMinutes(-20))),
                ("saleend", new JValue(now.AddMinutes(-10))));
            var resultAfter = _sut.Map(ResponseWithProduct(ProductWithDefaultPS(afterPS)), context);
            var itemAfter = SelectedItemFromMap(resultAfter);
            Assert.Equal(false, (bool?)itemAfter["priceschedule"]!["isonsale"]);
        }

        [Fact]
        public void Sale_InvalidDateStrings_TreatedAsNoBounds()
        {
            var context = Ctx("USD");

            var ps = PriceSchedule("USD", PB_WithSale(8),
                ("salestart", new JValue("not-a-date")),
                ("saleend", new JValue("also-not-a-date")));

            var result = _sut.Map(ResponseWithProduct(ProductWithDefaultPS(ps)), context);
            var item = SelectedItemFromMap(result);

            // TryGetDate fails for both -> treated as no bounds -> sale active because salePrice exists
            Assert.Equal(true, (bool?)item["priceschedule"]!["isonsale"]);
        }

        [Fact]
        public void Sale_DateTokensAsJValueDate_AreParsed()
        {
            var now = DateTimeOffset.UtcNow;
            var context = Ctx("USD");

            var ps = PriceSchedule("USD", PB_WithSale(8),
                ("salestart", new JValue(now.AddMinutes(-5))), // JTokenType.Date
                ("saleend", new JValue(now.AddMinutes(5))));

            var result = _sut.Map(ResponseWithProduct(ProductWithDefaultPS(ps)), context);
            var item = SelectedItemFromMap(result);

            Assert.Equal(true, (bool?)item["priceschedule"]!["isonsale"]);
        }

        [Fact]
        public void Sale_CurrentLogic_TreatsStringZeroNegativeSalePrice_AsActive()
        {
            var context = Ctx("USD");

            // salePrice as string
            var psString = PriceSchedule("USD", new JArray { new JObject { ["price"] = 10, ["salePrice"] = "8" } });
            var resString = _sut.Map(ResponseWithProduct(ProductWithDefaultPS(psString)), context);
            var itemString = SelectedItemFromMap(resString);
            Assert.Equal(true, (bool?)itemString["priceschedule"]!["isonsale"]);

            // salePrice = 0
            var psZero = PriceSchedule("USD", new JArray { new JObject { ["price"] = 10, ["salePrice"] = 0 } });
            var resZero = _sut.Map(ResponseWithProduct(ProductWithDefaultPS(psZero)), context);
            var itemZero = SelectedItemFromMap(resZero);
            Assert.Equal(true, (bool?)itemZero["priceschedule"]!["isonsale"]);

            // salePrice = -1
            var psNeg = PriceSchedule("USD", new JArray { new JObject { ["price"] = 10, ["salePrice"] = -1 } });
            var resNeg = _sut.Map(ResponseWithProduct(ProductWithDefaultPS(psNeg)), context);
            var itemNeg = SelectedItemFromMap(resNeg);
            Assert.Equal(true, (bool?)itemNeg["priceschedule"]!["isonsale"]);
        }

        // -----------------------
        // Helpers
        // -----------------------

        private static DecodedUserInfoToken Ctx(string? currency = "USD", string? companyId = "SELLER_A")
        {
            return MockUserInfoToken.BuildContext(
                currency,
                companyId
            );
        }

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

        private static JObject ProductWithDefaultPS(JObject priceSchedule, string ownerId = "SELLER_A")
            => new JObject
            {
                ["ownerid"] = ownerId,
                ["defaultpriceschedule"] = new JObject { ["priceschedule"] = priceSchedule },

                // include these so we can confirm pruning still works, even though not the main focus here
                ["buyers"] = new JArray(),
                ["suppliers"] = new JArray()
            };

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

        // Utility to build a PS with at least one salePrice (unless explicitly crafted otherwise)
        private static JArray PB_WithSale(decimal sale, decimal price = 10m)
            => new JArray { new JObject { ["price"] = price, ["salePrice"] = sale } };

        private static JArray PB_NoSaleOnly(decimal price = 10m)
            => new JArray { new JObject { ["price"] = price } };
    }
}

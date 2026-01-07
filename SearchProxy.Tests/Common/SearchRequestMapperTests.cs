using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrderCloud.Catalyst;
using SearchProxy.Common;
using Xunit;

namespace SearchProxy.Common.Tests
{
    public class SearchRequestMapperTests
    {
        private readonly SearchRequestMapper _sut = new SearchRequestMapper();

        [Fact]
        public void AddsVisibilityFilters()
        {
            // Arrange
            var request = CreateBaseRequest();
            var context = MockUserInfoToken.BuildContext(marketplaceID: "my-marketplace", companyID: "my-company");

            // Act
            var result = _sut.Map(request, context, null);

            // Assert
            var item = result.Widget.Items[0];
            var filter = item.Search!.Filter; // Asserted not null by context

            Assert.Equal("and", filter!.TypeValue);
            Assert.NotNull(filter.Filters);

            // Expected structure: AND(Active, Marketplace, OR(Buyer, Groups...))
            var filters = filter.Filters;
            Assert.Contains(filters!, f => f.Name == "active" && (bool)f.Value! == true);
            Assert.Contains(filters!, f => f.Name == "marketplace" && (string)f.Value! == "my-marketplace");
            
            // Check OR block for Buyer
            var orBlock = filters!.FirstOrDefault(f => f.TypeValue == "or");
            Assert.NotNull(orBlock);
            Assert.NotNull(orBlock!.Filters);
            Assert.Contains(orBlock.Filters!, f => f.Name == "buyers" && (string)f.Value! == "my-company");
        }

        [Fact]
        public void WithUserGroups_AddsGroupFilters()
        {
            // Arrange
            var request = CreateBaseRequest();
            var context = MockUserInfoToken.BuildContext(marketplaceID: "mp", companyID: "co", groups: new List<string> { "g1", "g2" });

            // Act
            var result = _sut.Map(request, context, null);

            // Assert
            var rootFilters = result.Widget.Items[0].Search!.Filter!.Filters;
            var orBlock = rootFilters!.FirstOrDefault(f => f.TypeValue == "or");
            
            Assert.NotNull(orBlock);
            Assert.NotNull(orBlock!.Filters);
            Assert.Contains(orBlock.Filters!, f => f.Name == "usergroups" && (string)f.Value! == "g1");
            Assert.Contains(orBlock.Filters!, f => f.Name == "usergroups" && (string)f.Value! == "g2");
        }

        [Fact]
        public void WithSellerId_AddsSupplierFilter()
        {
            // Arrange
            var request = CreateBaseRequest();
            var context = MockUserInfoToken.BuildContext(marketplaceID: "mp", companyID: "co");
            var ext = new OrderCloudExtensions { SellerId = "my-seller" };

            // Act
            var result = _sut.Map(request, context, ext);

            // Assert
            var filters = result.Widget.Items[0].Search!.Filter!.Filters;
            Assert.NotNull(filters);
            Assert.Contains(filters!, f => f.Name == "suppliers" && (string)f.Value! == "my-seller");
        }

        [Fact]
        public void MergesExistingFilters()
        {
            // Arrange
            var request = CreateBaseRequest();
            request.Widget.Items[0].Search!.Filter = new WidgetSearchFilter 
            { 
                Name = "foo", 
                TypeValue = "eq", 
                Value = "bar" 
            };
            var context = MockUserInfoToken.BuildContext(marketplaceID: "mp", companyID: "co");

            // Act
            var result = _sut.Map(request, context, null);

            // Assert
            var root = result.Widget.Items[0].Search!.Filter;
            Assert.Equal("and", root!.TypeValue);
            Assert.NotNull(root.Filters);

            // Should contain visibility filters AND the existing filter
            
            Assert.Equal(2, root.Filters!.Length);
            
            // One part should be visibility (which is an AND block itself, or contains the visibility logic)
            // The implementation wraps visibility filters effectively.
            
            // Check existing filter presence
            var existingPart = root.Filters.FirstOrDefault(f => f.Name == "foo");
            Assert.NotNull(existingPart);
            Assert.Equal("bar", existingPart!.Value);
        }

        private OrderCloudSearchRequest CreateBaseRequest()
        {
            return new OrderCloudSearchRequest
            {
                Widget = new WidgetsContainer
                {
                    Items = new[]
                    {
                        new WidgetItem
                        {
                            Entity = "product",
                            RfkId = "rfk",
                            Search = new WidgetSearch()
                        }
                    }
                }
            };
        }
    }
}

# SearchProxy
SearchProxy works alongside the [SearchSync](https://github.com/ordercloud-api/SearchSync) project. After product and catalog data has been synchronized via SearchSync, SearchProxy acts as a gateway to Sitecore Search, applying OrderCloud-specific visibility rules and returning user-specific pricing.

This solution mirrors the functionality of our hosted OrderCloud integration but is provided as open source, giving you full flexibility to customize and extend as needed.

## Key Features

- Forwards search requests to Sitecore Search.
- Applies visibility filters based on OrderCloud roles and permissions.
- Returns pricing tailored to the authenticated user.

## Architecture Diagram

![Architecture Diagram](/architecture-diagram.jpg)

## Examples
We've included sample implementations to help you get started:

- [ASP.NET Core Web API](./SearchProxy.WebApi/README.md)
- [Azure Functions](./SearchProxy.AzureFunction/README.md)

Check out the README files in the example projects for instructions on running locally.
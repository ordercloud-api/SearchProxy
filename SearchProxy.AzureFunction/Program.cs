using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderCloud.Catalyst;
using OrderCloud.SDK;
using SearchProxy.Common;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

var ocConfig = new OrderCloudClientConfig();
builder.Configuration.GetSection("OrderCloudSettings").Bind(ocConfig);

builder.Services
    .AddSearchProxy(options =>
    {
        builder.Configuration.GetSection("SearchProxySettings").Bind(options);
    })
    .AddSingleton<ISimpleCache, LazyCacheService>()
    .AddSingleton<ITokenValidator, TokenValidator>()
    .AddSingleton<IOrderCloudClient>(_ => new OrderCloudClient(ocConfig));

builder.Build().Run();

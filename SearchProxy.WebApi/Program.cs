using OrderCloud.Catalyst;
using OrderCloud.SDK;
using SearchProxy.Common;

var builder = WebApplication.CreateBuilder(args);


// OrderCloud Settings
var ocConfig = new OrderCloudClientConfig();
builder.Configuration.GetSection("OrderCloudSettings").Bind(ocConfig);


// Configure services
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddPolicy("integrationcors", builder => { builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); }));
builder.Services.AddSingleton<IOrderCloudClient>(_ => new OrderCloudClient(ocConfig));
builder.Services.AddOrderCloudUserAuth(options =>
{
    options.AllowAnyClientID();
});
builder.Services.AddOrderCloudUserInfoAuth();
builder.Services.AddOrderCloudWebhookAuth(options =>
{
    options.HashKey = "mysecretkey";
});
builder.Services.AddSearchProxy(options =>
{
    builder.Configuration.GetSection("SearchProxySettings").Bind(options);
});


// Build App
var app = builder.Build();


// Register Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCatalystExceptionHandler(reThrowError: true);
app.UseHttpsRedirection();
app.UseCors("integrationcors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


// Run App
app.Run();
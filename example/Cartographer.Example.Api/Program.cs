using Cartographer.Core.DependencyInjection;
using Cartographer.Core.Configuration.Naming;
using Cartographer.Core.Configuration;
using Cartographer.Example.Api.Services;
using Cartographer.Example.Api.Models;
using Cartographer.Example.Api.Profiles;
using Scalar.AspNetCore;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ----------------------------
// OpenAPI / Scalar UI
// ----------------------------
builder.Services.AddOpenApi();

builder.Services.AddSingleton<ICustomerService, InMemoryCustomerService>();
builder.Services.AddSingleton<IOrderService, InMemoryOrderService>();

builder.Services.AddCartographer(cfg =>
{
    cfg.SourceNamingConvention = new SnakeCaseNamingConvention();
    cfg.DestinationNamingConvention = new PascalCaseNamingConvention();
    cfg.NullCollectionStrategy = NullCollectionStrategy.UseEmptyCollection;
    cfg.PreserveReferences = true;
    new ApiProfile().Apply(cfg);
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/v1.json");
    app.MapScalarApiReference(opt =>
    {
        opt.OpenApiRoutePattern = "/openapi/v1.json";
        opt.Title = "AI Analyzer Hub API";

        // Remove BaseServerUrl for now; uncomment after confirming HTTPS port.
        // opt.BaseServerUrl = "https://localhost:5064";
    });

}


app.MapControllers();

app.Run();

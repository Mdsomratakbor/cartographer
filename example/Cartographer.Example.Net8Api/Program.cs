using Cartographer.Core.Configuration;
using Cartographer.Core.Configuration.Naming;
using Cartographer.Core.DependencyInjection;
using Cartographer.Example.Net8Api.Profiles;
using Cartographer.Example.Net8Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ICustomerDirectory, InMemoryCustomerDirectory>();
builder.Services.AddSingleton<IOrderBoard, InMemoryOrderBoard>();

builder.Services.AddCartographer(cfg =>
{
    cfg.SourceNamingConvention = new SnakeCaseNamingConvention();
    cfg.DestinationNamingConvention = new PascalCaseNamingConvention();
    cfg.MaxDepth = 2;
    cfg.PreserveReferences = true;
    cfg.NullCollectionStrategy = NullCollectionStrategy.UseEmptyCollection;
    new ExampleProfile().Apply(cfg);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

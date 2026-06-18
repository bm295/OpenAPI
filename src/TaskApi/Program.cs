using System.Text.Json.Serialization;
using TaskApi.Application.Abstractions;
using TaskApi.Application.Services;
using TaskApi.Infrastructure;
using TaskApi.Presentation.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<IOpenBankingStore, InMemoryOpenBankingStore>();
builder.Services.AddSingleton<IOpenBankingService, OpenBankingService>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapOpenBankingEndpoints();

app.Run();

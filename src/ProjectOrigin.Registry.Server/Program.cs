using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.Registry.Server;
using ProjectOrigin.Registry.Server.Models;

var startup = new Startup();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<TransactionProcessorOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

Configurator.ConfigureImmutableLog(builder);

startup.ConfigureServices(builder.Services);

var app = builder.Build();
startup.Configure(app, builder.Environment);

app.Run();

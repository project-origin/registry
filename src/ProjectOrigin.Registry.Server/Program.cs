using Microsoft.AspNetCore.Builder;
using ProjectOrigin.Registry.Server;

var startup = new Startup();

var builder = WebApplication.CreateBuilder(args);

Configurator.ConfigureImmutableLog(builder);

startup.ConfigureServices(builder.Services);

var app = builder.Build();
startup.Configure(app, builder.Environment);

app.Run();

using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Electricity.Server;

var startup = new Startup();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<ServerOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<IssuerOptions>()
    .Bind(builder.Configuration)
    .Validate(option => option.IsValid, "Invalid issuer configuration, must contain atleast one valid issuer.")
    .ValidateOnStart();

Configurator.ConfigureImmutableLog(builder);

startup.ConfigureServices(builder.Services);

var app = builder.Build();
startup.Configure(app, builder.Environment);

app.Run();

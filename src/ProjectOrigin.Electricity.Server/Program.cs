using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Electricity.Server;
using ProjectOrigin.VerifiableEventStore.Services.Batcher;

var startup = new Startup();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<IssuerOptions>()
    .Bind(builder.Configuration.GetSection("Issuers"))
    .Validate(option => option.IsValid, "Invalid issuer configuration.")
    .ValidateOnStart();

builder.Services.AddOptions<BatcherOptions>()
    .Bind(builder.Configuration.GetSection("VerifiableEventStore"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<ConcordiumOptions>()
    .Bind(builder.Configuration.GetSection("Concordium"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

startup.ConfigureServices(builder.Services);

var app = builder.Build();
startup.Configure(app, builder.Environment);

app.Run();

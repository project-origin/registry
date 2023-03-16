using ProjectOrigin.Electricity.Server;

var startup = new Startup();

var builder = WebApplication.CreateBuilder(args);
startup.ConfigureServices(builder.Services);

builder.Services.AddOptions<ConcordiumOptions>()
    .Bind(builder.Configuration.GetSection("Concordium"))
    .ValidateDataAnnotations();

var app = builder.Build();
startup.Configure(app, builder.Environment);

app.Run();

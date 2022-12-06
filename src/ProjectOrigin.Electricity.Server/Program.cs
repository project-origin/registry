using ProjectOrigin.Electricity.Server;

//WebApplication.CreateBuilder(args).WebHost
//    .UseStartup<Startup>()
//    .Build()
//    .Run();

var builder = WebApplication.CreateBuilder(args);
var startup = new Startup();
startup.ConfigureServices(builder.Services);
//startup.ConfigureServices(builder.Services); // calling ConfigureServices method
var app = builder.Build();
startup.Configure(app, builder.Environment);
app.Run();

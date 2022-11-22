using ProjectOrigin.Electricity.Server;

WebApplication.CreateBuilder(args).WebHost
    .UseStartup<Startup>()
    .Build()
    .Run();

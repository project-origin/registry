using System.Collections.Generic;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Registry.Server.Interfaces;
using ProjectOrigin.Registry.Server.Services;
using ProjectOrigin.VerifiableEventStore.Services.BatchProcessor;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.Registry.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGrpc();
        services.AddHostedService<BatchProcessorBackgroundService>();
        services.AddTransient<ITransactionStatusService, TransactionStatusService>();
        services.AddSingleton<ITransactionDispatcher, TransactionDispatcher>();

        // Memory only section
        services.AddDistributedMemoryCache();
        services.AddSingleton<IEventStore, MemoryEventStore>();
        services.AddMassTransit(x =>
        {
            x.UsingInMemory();
            x.AddConsumer<TransactionProcessor>();
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<RegistryService>();
            endpoints.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
        });
    }
}

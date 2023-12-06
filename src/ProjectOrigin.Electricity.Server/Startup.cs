using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.Electricity.Server.Interfaces;
using ProjectOrigin.Electricity.Server.Options;
using ProjectOrigin.Electricity.Server.Services;
using ProjectOrigin.Electricity.Server.Verifiers;

namespace ProjectOrigin.Electricity.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGrpc();

        services.AddSingleton<IProtoDeserializer>(new ProtoDeserializer(Assembly.GetAssembly(typeof(V1.IssuedEvent))
            ?? throw new Exception("Could not find assembly")));

        services.AddTransient<IEventVerifier<V1.IssuedEvent>, IssuedEventVerifier>();
        services.AddTransient<IEventVerifier<V1.AllocatedEvent>, AllocatedEventVerifier>();
        services.AddTransient<IEventVerifier<V1.ClaimedEvent>, ClaimedEventVerifier>();
        services.AddTransient<IEventVerifier<V1.SlicedEvent>, SlicedEventVerifier>();
        services.AddTransient<IEventVerifier<V1.TransferredEvent>, TransferredEventVerifier>();

        services.AddTransient<IVerifierDispatcher, VerifierDispatcher>();
        services.AddTransient<IRemoteModelLoader, GrpcRemoteModelLoader>();
        services.AddTransient<IModelHydrater, ElectricityModelHydrater>();
        services.AddTransient<IGridAreaIssuerService, GridAreaIssuerOptionsService>();

        services.AddOptions<IssuerOptions>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.Bind(settings);
            })
            .Validate(option => option.Verify())
            .ValidateOnStart();

        services.AddOptions<RegistryOptions>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.Bind(settings);
            })
            .Validate(x => x.Verify())
            .ValidateOnStart();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<ElectricityVerifierService>();
            endpoints.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
        });
    }
}

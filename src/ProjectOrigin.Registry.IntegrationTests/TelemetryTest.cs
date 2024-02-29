using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ProjectOrigin.Registry.Server;
using ProjectOrigin.TestUtils;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;
using Xunit.Sdk;

namespace ProjectOrigin.Registry.IntegrationTests;

public class TelemetryTest :
    IClassFixture<PostgresDatabaseFixture>,
    IClassFixture<GrpcTestBase<Startup>>,
    IClassFixture<RabbitMqFixture>,
    IClassFixture<RedisFixture>,
    IDisposable
{
    private readonly GrpcTestFixture<Startup> _grpcTestFixture;
    private readonly WireMockServer _wireMockServer;

    public TelemetryTest(GrpcTestFixture<Startup> grpcTestFixture,
        PostgresDatabaseFixture dbFixture,
        RabbitMqFixture rabbitMqFixture,
        RedisFixture redisFixture
        )
    {

        _grpcTestFixture = grpcTestFixture;
        _wireMockServer = WireMockServer.Start();
        _wireMockServer.Given(Request.Create().UsingAnyMethod())
            .RespondWith(Response.Create().WithStatusCode(200));

        grpcTestFixture.ConfigureHostConfiguration(new Dictionary<string, string?>()
        {
            {"Otlp:Enabled", "false"},
            {"RegistryName", "Test"},
            {"Verifiers:project_origin.electricity.v1", "https://localhost:5001"},
            {"ImmutableLog:type", "log"},
            {"BlockFinalizer:Interval", "00:00:05"},
            {"Persistance:type", "postgresql"},
            {"Persistance:postgresql:ConnectionString", dbFixture.HostConnectionString},
            {"Cache:Type", "redis"},
            {"Cache:Redis:ConnectionString", redisFixture.HostConnectionString},
            {"RabbitMq:Hostname", rabbitMqFixture.Hostname},
            {"RabbitMq:AmqpPort", rabbitMqFixture.AmqpPort.ToString()},
            {"RabbitMq:HttpApiPort", rabbitMqFixture.HttpApiPort.ToString()},
            {"RabbitMq:Username", RabbitMqFixture.Username},
            {"RabbitMq:Password", RabbitMqFixture.Password},
            {"TransactionProcessor:PodName", "Registry_0"},
            //{"TransactionProcessor:ServerNumber", "0"},
            {"TransactionProcessor:Servers", "1"},
            {"TransactionProcessor:Threads", "5"},
            {"TransactionProcessor:Weight", "10"},
        });
        grpcTestFixture.testServicesConfigure += services =>
        {
            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(serviceName: "Wallet.Test"))
                .WithMetrics(metrics => metrics
                    .AddOtlpExporter(o => o.Endpoint = new Uri(_wireMockServer.Urls[0])))
                .WithTracing(provider =>
                    provider
                        .AddOtlpExporter(o =>
                        {
                            o.Endpoint = new Uri(_wireMockServer.Urls[0]);
                            o.Protocol = OtlpExportProtocol.HttpProtobuf;
                            o.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>()
                            {
                                MaxQueueSize = 2,
                                ScheduledDelayMilliseconds = 1000,
                                MaxExportBatchSize = 1
                            };
                            o.HttpClientFactory = () =>
                            {
                                HttpClient client = new HttpClient();
                                client.DefaultRequestHeaders.Add("X-TestHeader", "value");
                                return client;
                            };
                        }));
        };
    }
    [Fact]
    public async Task TelemetryData_ShouldBeSentToMockCollector()
    {

    }

    public void Dispose()
    {
        _grpcTestFixture.Dispose();
    }
}

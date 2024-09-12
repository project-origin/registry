using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using ProjectOrigin.Electricity.IntegrationTests;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Registry.IntegrationTests.Fixtures;
using ProjectOrigin.TestCommon.Fixtures;
using Xunit;

namespace ProjectOrigin.Registry.IntegrationTests;

public class TelemetryTest :
    IClassFixture<PostgresDatabaseFixture<Startup>>,
    IClassFixture<TestServerFixture<Startup>>,
    IClassFixture<ElectricityServiceFixture>,
    IClassFixture<RabbitMqFixture>,
    IClassFixture<RedisFixture>,
    IClassFixture<OpenTelemetryFixture>,
    IDisposable
{
    private readonly TestServerFixture<Startup> _grpcTestFixture;
    private readonly OpenTelemetryFixture _openTelemetryFixture;
    private readonly ElectricityServiceFixture _electricityServiceFixture;
    private V1.RegistryService.RegistryServiceClient Client => new(_grpcTestFixture.Channel);

    public TelemetryTest(TestServerFixture<Startup> grpcTestFixture,
        PostgresDatabaseFixture<Startup> dbFixture,
        RabbitMqFixture rabbitMqFixture,
        RedisFixture redisFixture,
        OpenTelemetryFixture openTelemetryFixture,
        ElectricityServiceFixture electricityServiceFixture
    )
    {
        _grpcTestFixture = grpcTestFixture;
        _openTelemetryFixture = openTelemetryFixture;
        _electricityServiceFixture = electricityServiceFixture;

        grpcTestFixture.ConfigureHostConfiguration(new Dictionary<string, string?>()
        {
            { "Otlp:Enabled", "true" },
            { "Otlp:Endpoint", openTelemetryFixture.OtelUrl},
            { "RegistryName", "Test" },
            { "Verifiers:project_origin.electricity.v1", _electricityServiceFixture.Url },
            { "ImmutableLog:type", "log" },
            { "BlockFinalizer:Interval", "00:00:05" },
            { "Persistence:type", "postgresql" },
            { "ConnectionStrings:Database", dbFixture.HostConnectionString },
            { "Cache:Type", "redis" },
            { "Cache:Redis:ConnectionString", redisFixture.HostConnectionString },
            { "RabbitMq:Hostname", rabbitMqFixture.Hostname },
            { "RabbitMq:AmqpPort", rabbitMqFixture.AmqpPort.ToString() },
            { "RabbitMq:HttpApiPort", rabbitMqFixture.HttpApiPort.ToString() },
            { "RabbitMq:Username", RabbitMqFixture.Username },
            { "RabbitMq:Password", RabbitMqFixture.Password },
            { "TransactionProcessor:ServerNumber", "0" },
            { "TransactionProcessor:Servers", "1" },
            { "TransactionProcessor:Threads", "5" },
            { "TransactionProcessor:Weight", "10" },
        });
    }

    [Fact]
    public async Task TelemetryData_ShouldBeSentToMockCollector()
    {
        var owner = Algorithms.Secp256k1.GenerateNewPrivateKey();

        var commitmentInfo = new SecretCommitmentInfo(250);
        var certId = Guid.NewGuid();

        IssuedEvent @event = Helper.CreateIssuedEvent("Test", _electricityServiceFixture.IssuerArea, owner.PublicKey, commitmentInfo, certId);

        var transaction = Helper.SignTransaction(@event.CertificateId, @event, _electricityServiceFixture.IssuerKey);

        await Client.SendTransactions(transaction);

        await Task.Delay(10000);
        var telemetryData = await _openTelemetryFixture.GetContainerLog();
        telemetryData.Should().Contain("Trace ID       : ");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _grpcTestFixture.Dispose();
    }
}

using Xunit.Abstractions;
using Xunit;
using System.Threading.Tasks;
using System;
using ProjectOrigin.PedersenCommitment;
using FluentAssertions;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.HierarchicalDeterministicKeys;
using System.Collections.Generic;
using ProjectOrigin.TestCommon.Fixtures;
using ProjectOrigin.Registry;
using ProjectOrigin.Registry.IntegrationTests.Fixtures;

namespace ProjectOrigin.Electricity.IntegrationTests;

public class FlowTestsComitted :
    IClassFixture<TestServerFixture<Startup>>,
    IClassFixture<ElectricityServiceFixture>,
    IClassFixture<PostgresDatabaseFixture<Startup>>,
    IClassFixture<RedisFixture>,
    IClassFixture<RabbitMqFixture>
{
    protected const string RegistryName = "SomeRegistry";
    protected readonly ElectricityServiceFixture _verifierFixture;
    private readonly PostgresDatabaseFixture<Startup> _postgresDatabaseFixture;

    private readonly Lazy<Registry.V1.RegistryService.RegistryServiceClient> _client;
    protected Registry.V1.RegistryService.RegistryServiceClient Client => _client.Value;

    public FlowTestsComitted(
        ElectricityServiceFixture verifierFixture,
        TestServerFixture<Startup> serverFixture,
        PostgresDatabaseFixture<Startup> postgresDatabaseFixture,
        RedisFixture redisFixture,
        RabbitMqFixture rabbitMqFixture,
        ITestOutputHelper outputHelper)
    {
        _verifierFixture = verifierFixture;
        _postgresDatabaseFixture = postgresDatabaseFixture;
        _client = new(() => new Registry.V1.RegistryService.RegistryServiceClient(serverFixture.Channel));
        serverFixture.ConfigureHostConfiguration(new Dictionary<string, string?>()
        {
            {"Otlp:Enabled", "false"},
            {"RegistryName", RegistryName},
            {"ReturnComittedForFinalized", "true"},
            {"Verifiers:project_origin.electricity.v1", _verifierFixture.Url},
            {"ImmutableLog:type", "log"},
            {"BlockFinalizer:Interval", "00:00:05"},
            {"Persistence:type", "postgresql"},
            {"ConnectionStrings:Database", _postgresDatabaseFixture.HostConnectionString},
            {"Cache:Type", "redis"},
            {"Cache:Redis:ConnectionString", redisFixture.HostConnectionString},
            {"RabbitMq:Hostname", rabbitMqFixture.Hostname},
            {"RabbitMq:AmqpPort", rabbitMqFixture.AmqpPort.ToString()},
            {"RabbitMq:HttpApiPort", rabbitMqFixture.HttpApiPort.ToString()},
            {"RabbitMq:Username", RabbitMqFixture.Username},
            {"RabbitMq:Password", RabbitMqFixture.Password},
            {"TransactionProcessor:ServerNumber", "0"},
            {"TransactionProcessor:Servers", "1"},
            {"TransactionProcessor:Threads", "5"},
            {"TransactionProcessor:Weight", "10"},
        });
    }

    [Fact]
    public async Task issue_comsumption_certificate_success()
    {
        var owner = Algorithms.Secp256k1.GenerateNewPrivateKey();

        var commitmentInfo = new SecretCommitmentInfo(250);
        var certId = Guid.NewGuid();

        IssuedEvent @event = Helper.CreateIssuedEvent(RegistryName, _verifierFixture.IssuerArea, owner.PublicKey, commitmentInfo, certId);

        var transaction = Helper.SignTransaction(@event.CertificateId, @event, _verifierFixture.IssuerKey);

        var status = await Client.GetStatus(transaction);
        status.Status.Should().Be(Registry.V1.TransactionState.Unknown);

        await Client.SendTransactions(transaction);
        status = await Client.GetStatus(transaction);
        status.Status.Should().Be(Registry.V1.TransactionState.Pending);

        status = await Helper.RepeatUntilOrTimeout(
            () => Client.GetStatus(transaction),
            result => result.Status == Registry.V1.TransactionState.Finalized,
            TimeSpan.FromSeconds(60));

        status.Message.Should().BeEmpty();

        var stream = await Client.GetStream(certId);
        stream.Transactions.Should().HaveCount(1);

        await Task.Delay(10000);

        var blocks = await Client.GetBlocksAsync(new Registry.V1.GetBlocksRequest
        {
            Skip = 0,
            Limit = 1,
            IncludeTransactions = false
        });

        blocks.Blocks.Should().HaveCount(1);
    }
}

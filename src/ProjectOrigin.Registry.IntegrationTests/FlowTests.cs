using Xunit.Abstractions;
using ProjectOrigin.TestUtils;
using ProjectOrigin.Registry.Server;
using Xunit;
using System.Threading.Tasks;
using System;
using ProjectOrigin.PedersenCommitment;
using FluentAssertions;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.HierarchicalDeterministicKeys;
using System.Collections.Generic;
using ProjectOrigin.Registry.IntegrationTests;

namespace ProjectOrigin.Electricity.IntegrationTests;

public class FlowTests : GrpcTestBase<Startup>, IClassFixture<ElectricityServiceFixture>, IClassFixture<PostgresDatabaseFixture>, IClassFixture<RedisFixture>
{
    protected ElectricityServiceFixture _verifierFixture;
    private PostgresDatabaseFixture _postgresDatabaseFixture;
    protected const string RegistryName = "SomeRegistry";

    protected Registry.V1.RegistryService.RegistryServiceClient Client => new(_grpcFixture.Channel);

    public FlowTests(
        ElectricityServiceFixture verifierFixture,
        GrpcTestFixture<Startup> grpcFixture,
        PostgresDatabaseFixture postgresDatabaseFixture,
        RedisFixture redisFixture,
        ITestOutputHelper outputHelper) : base(grpcFixture, outputHelper)
    {
        _verifierFixture = verifierFixture;
        _postgresDatabaseFixture = postgresDatabaseFixture;
        grpcFixture.ConfigureHostConfiguration(new Dictionary<string, string?>()
        {
            {"RegistryName", RegistryName},
            {$"Verifiers:project_origin.electricity.v1", _verifierFixture.Url},
            {$"ImmutableLog:type", "log"},
            {"BlockFinalizer:Interval", "00:00:05"},
            {"Persistance:type", "postgresql"},
            {"Persistance:postgresql:ConnectionString", _postgresDatabaseFixture.HostConnectionString},
            {"Cache:Type", "redis"},
            {"Cache:Redis:ConnectionString", redisFixture.HostConnectionString},
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
            result => result.Status == Registry.V1.TransactionState.Committed,
            TimeSpan.FromSeconds(60));

        status.Message.Should().BeEmpty();

        var stream = await Client.GetStream(certId);
        stream.Transactions.Should().HaveCount(1);
    }
}

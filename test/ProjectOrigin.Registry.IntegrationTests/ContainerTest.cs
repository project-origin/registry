using Xunit;
using System.Threading.Tasks;
using System;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using System.Text;
using Grpc.Net.Client;
using FluentAssertions;
using Xunit.Abstractions;
using ProjectOrigin.TestCommon.Fixtures;
using ProjectOrigin.Registry;
using ProjectOrigin.Registry.IntegrationTests.Fixtures;

namespace ProjectOrigin.Electricity.IntegrationTests;

public class ContainerTest : IAsyncLifetime,
    IClassFixture<ContainerImageFixture>,
    IClassFixture<ElectricityServiceFixture>,
    IClassFixture<PostgresDatabaseFixture<Startup>>,
    IClassFixture<RedisFixture>,
    IClassFixture<RabbitMqFixture>
{
    private const string ElectricityVerifierImage = "ghcr.io/project-origin/electricity-server:0.5.0";
    private const int ElectricityVerifierGrpcPort = 5000;
    private const int GrpcPort = 5000;
    private const string IssuerArea = "Narnia";
    private const string RegistryName = "TheRegistry";

    private readonly IContainer _verifierContainer;
    private readonly Lazy<IContainer> _registryContainer;
    private readonly IPrivateKey _issuerKey;
    private readonly PostgresDatabaseFixture<Startup> _postgresDatabaseFixture;
    private readonly ITestOutputHelper _outputHelper;

    public ContainerTest(
        ContainerImageFixture imageFixture,
        PostgresDatabaseFixture<Startup> postgresDatabaseFixture,
        RedisFixture redisFixture,
        RabbitMqFixture rabbitMqFixture,
        ITestOutputHelper outputHelper)
    {
        _issuerKey = Algorithms.Ed25519.GenerateNewPrivateKey();
        _postgresDatabaseFixture = postgresDatabaseFixture;
        _outputHelper = outputHelper;

        _verifierContainer = new ContainerBuilder()
                .WithImage(ElectricityVerifierImage)
                .WithPortBinding(ElectricityVerifierGrpcPort, true)
                .WithEnvironment($"Issuers__{IssuerArea}", Convert.ToBase64String(Encoding.UTF8.GetBytes(_issuerKey.PublicKey.ExportPkixText())))
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilPortIsAvailable(ElectricityVerifierGrpcPort)
                    )
                .Build();

        _registryContainer = new Lazy<IContainer>(() =>
        {
            var verifierUrl = $"http://{_verifierContainer.IpAddress}:{ElectricityVerifierGrpcPort}";
            return new ContainerBuilder()
                .WithImage(imageFixture.Image)
                .WithPortBinding(GrpcPort, true)
                .WithCommand("--serve")
                .WithEnvironment("RegistryName", RegistryName)
                .WithEnvironment("Verifiers__project_origin.electricity.v1", verifierUrl)
                .WithEnvironment("ImmutableLog__type", "log")
                .WithEnvironment("BlockFinalizer__Interval", "00:00:05")
                .WithEnvironment("ConnectionStrings__Database", _postgresDatabaseFixture.ContainerConnectionString)
                .WithEnvironment("Cache__Type", "redis")
                .WithEnvironment("Cache__Redis__ConnectionString", redisFixture.ContainerConnectionString)
                .WithEnvironment("RabbitMq__Hostname", rabbitMqFixture.ContainerIp)
                .WithEnvironment("RabbitMq__AmqpPort", RabbitMqFixture.ContainerAmqpPort.ToString())
                .WithEnvironment("RabbitMq__HttpApiPort", RabbitMqFixture.ContainerHttpPort.ToString())
                .WithEnvironment("RabbitMq__Username", RabbitMqFixture.Username)
                .WithEnvironment("RabbitMq__Password", RabbitMqFixture.Password)
                .WithEnvironment("TransactionProcessor__ServerNumber", "0")
                .WithEnvironment("TransactionProcessor__Servers", "1")
                .WithEnvironment("TransactionProcessor__Threads", "5")
                .WithEnvironment("TransactionProcessor__Weight", "10")
                .WithCreateParameterModifier(parameterModifier => parameterModifier.User = "1654")
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilPortIsAvailable(GrpcPort)
                    )
                .Build();
        });
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _verifierContainer.StartAsync();
            await _registryContainer.Value.StartAsync();
            await _postgresDatabaseFixture.ResetDatabase();
        }
        catch (Exception)
        {
            await WriteRegistryContainerLog();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (_registryContainer.IsValueCreated)
        {
            await WriteRegistryContainerLog();
            await _registryContainer.Value.StopAsync();
        }
        await _verifierContainer.StopAsync();
    }

    private async Task WriteRegistryContainerLog()
    {
        var log = await _registryContainer.Value.GetLogsAsync();
        _outputHelper.WriteLine($"-------Container stdout------\n{log.Stdout}\n-------Container stderr------\n{log.Stderr}\n\n----------");
    }

    private GrpcChannel CreateRegistryChannel()
    {
        return GrpcChannel.ForAddress($"http://{_registryContainer.Value.IpAddress}:{GrpcPort}");
    }


    [Fact]
    public async Task issue_comsumption_certificate_success()
    {
        var Client = new Registry.V1.RegistryService.RegistryServiceClient(CreateRegistryChannel());

        var owner = Algorithms.Secp256k1.GenerateNewPrivateKey();

        var commitmentInfo = new SecretCommitmentInfo(250);
        var certId = Guid.NewGuid();

        IssuedEvent @event = Helper.CreateIssuedEvent(RegistryName, IssuerArea, owner.PublicKey, commitmentInfo, certId);

        var transaction = Helper.SignTransaction(@event.CertificateId, @event, _issuerKey);

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

        var blocks = await Client.GetBlocksAsync(new Registry.V1.GetBlocksRequest
        {
            Skip = 0,
            Limit = 1,
            IncludeTransactions = true
        });

        blocks.Blocks.Should().HaveCount(1);
    }
}

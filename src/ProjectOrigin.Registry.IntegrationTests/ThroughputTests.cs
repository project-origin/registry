using Xunit;
using System.Threading.Tasks;
using System;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.HierarchicalDeterministicKeys;
using System.Diagnostics;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using System.Text;
using Grpc.Net.Client;
using DotNet.Testcontainers.Images;
using System.Collections.Concurrent;
using FluentAssertions;

namespace ProjectOrigin.Electricity.IntegrationTests;

public class ThroughputTests : IAsyncLifetime
{
    private const string ElectricityVerifierImage = "ghcr.io/project-origin/electricity-server:0.2.0-rc.17";
    private const string RegistryImage = "ghcr.io/project-origin/registry-server:0.2.0-rc.17";
    private const int GrpcPort = 80;
    private const string IssuerArea = "Narnia";
    private const string RegistryName = "TheRegistry";

    private readonly IFutureDockerImage _registryImage;
    private readonly IContainer _verifierContainer;
    private readonly Lazy<IContainer> _registryContainer;
    private readonly IPrivateKey _issuerKey;

    public ThroughputTests()
    {
        _issuerKey = Algorithms.Ed25519.GenerateNewPrivateKey();

        _verifierContainer = new ContainerBuilder()
                .WithImage(ElectricityVerifierImage)
                .WithPortBinding(GrpcPort, true)
                .WithEnvironment($"Issuers__{IssuerArea}", Convert.ToBase64String(Encoding.UTF8.GetBytes(_issuerKey.PublicKey.ExportPkixText())))
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilPortIsAvailable(GrpcPort)
                    )
                .Build();

        _registryImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), string.Empty)
            .WithDockerfile("ProjectOrigin.Registry.Server/Dockerfile")
            .Build();

        _registryContainer = new Lazy<IContainer>(() =>
        {
            var verifierUrl = $"http://{_verifierContainer.IpAddress}:{GrpcPort}";
            return new ContainerBuilder()
                .WithImage(_registryImage)
                .WithPortBinding(GrpcPort, true)
                .WithEnvironment("Verifiers__project_origin.electricity.v1", verifierUrl)
                .WithEnvironment("RegistryName", RegistryName)
                .WithEnvironment("IMMUTABLELOG__TYPE", "log")
                .WithEnvironment("VERIFIABLEEVENTSTORE__BATCHSIZEEXPONENT", "0")
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilPortIsAvailable(GrpcPort)
                    )
                .Build();
        });
    }

    private GrpcChannel CreateRegistryChannel()
    {
        return GrpcChannel.ForAddress($"http://{_registryContainer.Value.IpAddress}:{GrpcPort}");
    }

    [Fact]
    public async Task TestThroughput()
    {
        int concurrency = 10;
        int concurrentRequests = 500;
        var tasks = new Task[concurrency];
        var stopwatch = new Stopwatch();

        ConcurrentQueue<Registry.V1.Transaction> queued = new ConcurrentQueue<Registry.V1.Transaction>();
        ConcurrentBag<Registry.V1.Transaction> completed = new ConcurrentBag<Registry.V1.Transaction>();
        using var channel = CreateRegistryChannel();

        stopwatch.Start();
        for (int i = 0; i < concurrency; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                while (true)
                {
                    if (queued.Count < concurrentRequests)
                    {
                        queued.Enqueue(await SendRequest(channel).ConfigureAwait(false));
                    }
                    else
                    {
                        if (queued.TryDequeue(out var transaction))
                        {
                            if (await IsCommitted(channel, transaction))
                            {
                                Console.WriteLine($"completed request {completed.Count}");
                                completed.Add(transaction);
                            }
                            else
                            {
                                queued.Enqueue(transaction);
                                await Task.Delay(25).ConfigureAwait(false);
                            }
                        }
                    }
                }
            });
        }
        await Task.Delay(TimeSpan.FromMinutes(5)).ConfigureAwait(false);
        stopwatch.Stop();

        var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
        var requestsPerSecond = completed.Count / elapsedSeconds;

        Console.WriteLine($"Completed {completed.Count} requests in {elapsedSeconds} seconds ({requestsPerSecond} requests per second).");
        requestsPerSecond.Should().BeGreaterThan(8); // based on througput test on github ~10
    }

    private async Task<Registry.V1.Transaction> SendRequest(GrpcChannel channel)
    {
        var client = new Registry.V1.RegistryService.RegistryServiceClient(channel);
        var owner = Algorithms.Secp256k1.GenerateNewPrivateKey();
        var commitmentInfo = new SecretCommitmentInfo(250);
        IssuedEvent @event = Helper.CreateIssuedEvent(RegistryName, IssuerArea, owner.Derive(1).PublicKey, commitmentInfo, Guid.NewGuid());
        var transaction = Helper.SignTransaction(@event.CertificateId, @event, _issuerKey);
        await client.SendTransactions(transaction);
        return transaction;
    }

    private async Task<bool> IsCommitted(GrpcChannel channel, Registry.V1.Transaction transaction)
    {
        var client = new Registry.V1.RegistryService.RegistryServiceClient(channel);
        var result = await client.GetStatus(transaction);
        return result.Status == Registry.V1.TransactionState.Committed;
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_registryImage.CreateAsync(), _verifierContainer.StartAsync())
            .ConfigureAwait(false);

        await _registryContainer.Value.StartAsync()
            .ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_registryContainer.IsValueCreated)
        {
            await _registryContainer.Value.StopAsync();
        }
        await _verifierContainer.StopAsync();
        await _registryImage.DeleteAsync();
    }
}

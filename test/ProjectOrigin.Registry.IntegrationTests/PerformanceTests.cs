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
using System.Collections.Concurrent;
using FluentAssertions;
using System.Linq;
using System.Collections.Generic;
using Xunit.Abstractions;
using ProjectOrigin.TestCommon.Fixtures;
using ProjectOrigin.Registry;
using ProjectOrigin.Registry.IntegrationTests.Fixtures;

namespace ProjectOrigin.Electricity.IntegrationTests;

public class PerformanceTests : IAsyncLifetime,
    IClassFixture<ContainerImageFixture>,
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

    public PerformanceTests(
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
                .WithEnvironment("BlockFinalizer__Interval", "00:00:02")
                .WithEnvironment("ConnectionStrings__Database", _postgresDatabaseFixture.ContainerConnectionString)
                .WithEnvironment("Logging__LogLevel__Default", "Debug")
                .WithEnvironment("Logging__LogLevel__Grpc.AspNetCore", "Information")
                .WithEnvironment("Logging__LogLevel__Grpc.AspNetCore", "Information")
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
        Console.WriteLine($"-- Starting throughput test --");

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
                        queued.Enqueue(await SendRequest(channel));
                    }
                    else
                    {
                        if (queued.TryDequeue(out var transaction))
                        {
                            if (await IsFinalized(channel, transaction))
                            {
                                completed.Add(transaction);
                            }
                            else
                            {
                                queued.Enqueue(transaction);
                                await Task.Delay(25);
                            }
                        }
                    }
                }
            });
        }
        await Task.Delay(TimeSpan.FromMinutes(5));
        stopwatch.Stop();

        var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
        var requestsPerSecond = completed.Count / elapsedSeconds;

        Console.WriteLine($"Completed {completed.Count} transactions in {elapsedSeconds} seconds ({requestsPerSecond} requests per second).");
        Console.WriteLine($"-- Finished throughput test --");

        requestsPerSecond.Should().BeGreaterThan(150); // based on througput test on github ~170
    }

    [Fact]
    public async Task TestLatencySequential()
    {
        Console.WriteLine($"-- Starting sequential test --");

        int count = 10;

        using var channel = CreateRegistryChannel();

        List<long> measurements = new List<long>();
        var stopwatch = new Stopwatch();

        foreach (var i in Enumerable.Range(0, count))
        {
            stopwatch.Restart();
            var transaction = await SendRequest(channel);
            while (!await IsFinalized(channel, transaction))
            {
                await Task.Delay(25);
            }

            stopwatch.Stop();
            measurements.Add(stopwatch.ElapsedMilliseconds);
        }

        var ms95th = measurements.OrderBy(x => x).ElementAt((int)(count * 0.95));
        Console.WriteLine($"Unit: Millisecond delay from published to committed");
        Console.WriteLine("Max:  " + measurements.Max());
        Console.WriteLine("95th: " + ms95th);
        Console.WriteLine("Mean: " + Math.Round(measurements.Average()));
        Console.WriteLine("Min:  " + measurements.Min());
        Console.WriteLine($"-- Finished sequential test --");

        ms95th.Should().BeLessThan(4000);
    }

    [Fact]
    public async Task TestLatencyParallel()
    {
        Console.WriteLine($"-- Starting parallel test --");
        int count = 100;

        using var channel = CreateRegistryChannel();

        List<long> measurements = new List<long>();

        ConcurrentQueue<(Stopwatch stopwatch, Registry.V1.Transaction transaction)> queued = new();

        for (int i = 0; i < count; i++)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var transaction = await SendRequest(channel);

            queued.Enqueue((stopwatch, transaction));
        }

        while (queued.TryDequeue(out var item))
        {
            if (await IsFinalized(channel, item.transaction))
            {
                item.stopwatch.Stop();
                measurements.Add(item.stopwatch.ElapsedMilliseconds);
            }
            else
            {
                queued.Enqueue(item);
                await Task.Delay(25);
            }
        }

        var ms95th = measurements.OrderBy(x => x).ElementAt((int)(count * 0.95));
        Console.WriteLine($"Number of transactions: {count}");
        Console.WriteLine($"Unit: Milliseconds test start to transaction committed");
        Console.WriteLine("Max:  " + measurements.Max());
        Console.WriteLine("95th: " + ms95th);
        Console.WriteLine("Mean: " + measurements.Average());
        Console.WriteLine("Min:  " + measurements.Min());
        Console.WriteLine($"-- Finished parallel test --");

        ms95th.Should().BeLessThan(2000);
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

    private static async Task<bool> IsFinalized(GrpcChannel channel, Registry.V1.Transaction transaction)
    {
        var client = new Registry.V1.RegistryService.RegistryServiceClient(channel);
        var result = await client.GetStatus(transaction);
        return result.Status == Registry.V1.TransactionState.Finalized;
    }

}

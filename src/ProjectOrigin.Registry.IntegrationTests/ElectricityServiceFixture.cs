using System;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using Xunit;

public class ElectricityServiceFixture : IAsyncLifetime
{
    private const string ElectricityVerifierImage = "ghcr.io/project-origin/electricity-server:0.2.0-rc.13";
    private const int GrpcPort = 80;

    public string IssuerArea => "SomeArea";
    public IPrivateKey IssuerKey { get; init; }
    private IContainer _container;

    public string Url => $"http://{_container.Hostname}:{_container.GetMappedPublicPort(GrpcPort)}";

    public ElectricityServiceFixture()
    {
        IssuerKey = Algorithms.Ed25519.GenerateNewPrivateKey();

        _container = new ContainerBuilder()
                .WithImage(ElectricityVerifierImage)
                .WithPortBinding(GrpcPort, true)
                .WithEnvironment($"Issuers__{IssuerArea}", Convert.ToBase64String(Encoding.UTF8.GetBytes(IssuerKey.PublicKey.ExportPkixText())))
                .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync()
            .ConfigureAwait(false);

        await Task.Delay(5000);
    }

    public async Task DisposeAsync()
    {
        // var log = await _container.GetLogsAsync();
        // Console.WriteLine("CONTAINERLOG: " + log.Stdout);
        // Console.WriteLine("CONTAINERERR: " + log.Stderr);
        await _container.StopAsync();
    }
}

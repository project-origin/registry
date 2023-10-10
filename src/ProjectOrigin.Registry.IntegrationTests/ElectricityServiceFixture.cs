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
    private const string ElectricityVerifierImage = "ghcr.io/project-origin/electricity-server:0.3.0";
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
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilPortIsAvailable(GrpcPort)
                    )
                .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
    }
}

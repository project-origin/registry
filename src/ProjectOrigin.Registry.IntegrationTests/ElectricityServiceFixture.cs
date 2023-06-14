using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using Xunit;

public class ElectricityServiceFixture : IAsyncLifetime
{
    private const string ElectricityVerifierImage = "ghcr.io/project-origin/electricity-server:0.2.0-rc.3";
    private const int GrpcPort = 80;

    public string IssuerArea { get => "SomeArea"; }
    public IHDPrivateKey IssuerKey { get; init; }
    private IContainer _container;

    public string Url { get => $"http://{_container.Hostname}:{_container.GetMappedPublicPort(GrpcPort)}"; }

    public ElectricityServiceFixture()
    {
        var algorithm = new Secp256k1Algorithm();
        IssuerKey = algorithm.GenerateNewPrivateKey();

        _container = new ContainerBuilder()
                .WithImage(ElectricityVerifierImage)
                .WithPortBinding(GrpcPort, true)
                .WithEnvironment($"Issuers__{IssuerArea}", Convert.ToBase64String(IssuerKey.PublicKey.Export()))
                .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync()
            .ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
    }

}

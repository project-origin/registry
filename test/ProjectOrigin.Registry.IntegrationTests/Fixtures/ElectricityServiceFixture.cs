using System;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.TestCommon.Extensions;
using Xunit;

namespace ProjectOrigin.Registry.IntegrationTests.Fixtures;

public class ElectricityServiceFixture : IAsyncLifetime
{
    private const string ElectricityVerifierImage = "ghcr.io/project-origin/electricity-server:3.0.1-rc.1";
    private const int GrpcPort = 80;
    private const string Area = "SomeArea";

    public string IssuerArea => Area;
    public IPrivateKey IssuerKey { get; init; }
    private IContainer _container;

    public string Url => $"http://{_container.Hostname}:{_container.GetMappedPublicPort(GrpcPort)}";

    public ElectricityServiceFixture()
    {
        IssuerKey = Algorithms.Ed25519.GenerateNewPrivateKey();

        _container = new ContainerBuilder()
                .WithImage(ElectricityVerifierImage)
                .WithPortBinding(GrpcPort, true)
                .WithEnvironment($"Issuers__{Area}", Convert.ToBase64String(Encoding.UTF8.GetBytes(IssuerKey.PublicKey.ExportPkixText())))
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilGrpcResponds(GrpcPort)
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

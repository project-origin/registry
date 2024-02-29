using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace ProjectOrigin.TestUtils;

public class OpenTelemetryFixture : IAsyncLifetime
{
    private readonly Lazy<IContainer> OtelCollectorContainer = new(() =>
    {
        return new ContainerBuilder()
            .WithImage("otel/opentelemetry-collector-contrib:latest")
            .WithPortBinding(4317, true)
            .WithPortBinding(55681, true)
            .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")
            .Build();
    });

    public Task InitializeAsync() => OtelCollectorContainer.Value.StartAsync();

    public Task DisposeAsync() => OtelCollectorContainer.Value.StopAsync();
}

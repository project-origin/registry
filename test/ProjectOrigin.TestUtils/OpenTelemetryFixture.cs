using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace ProjectOrigin.TestUtils;

public class OpenTelemetryFixture : IAsyncLifetime
{
    private const int OtelPort = 4317;
    private readonly IContainer _container;

    public OpenTelemetryFixture()
    {
        _container = new ContainerBuilder()
            .WithImage("otel/opentelemetry-collector:0.95.0")
            .WithPortBinding(OtelPort, true)
            .Build();
    }

    public string OtelUrl
    {
        get
        {
            return $"http://localhost:{_container.GetMappedPublicPort(OtelPort)}";
        }
    }

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.StopAsync();

    public async Task<string> GetContainerLog()
    {
        var (stdout, strerr) = await _container.GetLogsAsync();
        return stdout + "\n" + strerr;
    }
}


using System;
using System.Threading.Tasks;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace ProjectOrigin.TestUtils;

public class RabbitMqFixture : IAsyncLifetime
{
    private string _username = "test";
    private string _password = "test";

    public string HostConnectionString => _container.GetConnectionString();
    public string ContainerConnectionString => new UriBuilder("amqp://", _container.IpAddress, 5672)
    {
        UserName = Uri.EscapeDataString(_username),
        Password = Uri.EscapeDataString(_password)
    }.ToString();

    private RabbitMqContainer _container;

    public RabbitMqFixture()
    {
        _container = new RabbitMqBuilder()
            .WithUsername(_username)
            .WithPassword(_password)
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

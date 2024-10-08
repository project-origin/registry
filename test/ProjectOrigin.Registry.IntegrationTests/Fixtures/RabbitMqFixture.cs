using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;
using ProjectOrigin.Registry.IntegrationTests.Extensions;
using Testcontainers.RabbitMq;
using Xunit;

namespace ProjectOrigin.Registry.IntegrationTests.Fixtures;

public class RabbitMqFixture : IAsyncLifetime
{
    public const int ContainerHttpPort = 15672;
    public const int ContainerAmqpPort = RabbitMqBuilder.RabbitMqPort;
    public const string Username = RabbitMqBuilder.DefaultUsername;
    public const string Password = RabbitMqBuilder.DefaultPassword;

    private readonly IFutureDockerImage _image;
    private readonly RabbitMqContainer _container;

    public string Hostname => _container.Hostname;
    public string ContainerIp => _container.IpAddress;
    public int AmqpPort => _container.GetMappedPublicPort(ContainerAmqpPort);
    public int HttpApiPort => _container.GetMappedPublicPort(ContainerHttpPort);

    public RabbitMqFixture()
    {
        _image = new ImageFromDockerfileBuilder()
            .WithDockerfileContent("""
            FROM rabbitmq:3.13

            RUN rabbitmq-plugins enable --offline rabbitmq_management

            EXPOSE 15672
            """)
            .Build();

        _container = new RabbitMqBuilder()
            .WithImage(_image)
            .WithPortBinding(ContainerHttpPort, true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _image.CreateAsync();
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
    }
}

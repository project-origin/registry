using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;
using Testcontainers.RabbitMq;
using Xunit;

namespace ProjectOrigin.TestUtils;

public class RabbitMqFixture : IAsyncLifetime
{
    private const int HttpPort = 15672;
    private readonly IFutureDockerImage _image;
    private readonly RabbitMqContainer _container;

    public string Hostname => _container.Hostname;
    public int AmqpPort => _container.GetMappedPublicPort(RabbitMqBuilder.RabbitMqPort);
    public int HttpApiPort => _container.GetMappedPublicPort(HttpPort);
    public string Username => RabbitMqBuilder.DefaultUsername;
    public string Password => RabbitMqBuilder.DefaultPassword;

    public RabbitMqFixture()
    {
        _image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetProjectDirectory(), string.Empty)
            .WithDockerfile("rabbitmq.dockerfile")
            .Build();

        _container = new RabbitMqBuilder()
            .WithImage(_image)
            .WithPortBinding(HttpPort, true)
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

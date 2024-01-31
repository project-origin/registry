using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.Server.Services;
using ProjectOrigin.TestUtils;
using RabbitMQ.Client;
using Xunit;

namespace ProjectOrigin.Registry.IntegrationTests;

public class RabbitMqHttpClientTests
{
    [Fact]
    public async Task GetQueuesAsync_ShouldReturnQueue()
    {
        var rabbitMq = new RabbitMqFixture();
        try
        {
            await rabbitMq.InitializeAsync();

            // Arrange
            var options = Options.Create(new Server.Models.RabbitMqOptions()
            {
                Username = rabbitMq.Username,
                Password = rabbitMq.Password,
                Hostname = rabbitMq.Hostname,
                HttpApiPort = rabbitMq.HttpApiPort,
                AmqpPort = rabbitMq.AmqpPort
            });
            var client = new RabbitMqHttpClient(options);

            var factory = new ConnectionFactory()
            {
                HostName = rabbitMq.Hostname,
                Port = rabbitMq.AmqpPort,
                UserName = rabbitMq.Username,
                Password = rabbitMq.Password,
            };

            using var con = factory.CreateConnection();
            using var channel = con.CreateChannel();

            channel.QueueDeclare("test", true, false, false, null);
            channel.BasicPublish("", "test", Encoding.UTF8.GetBytes("test1"));
            channel.BasicPublish("", "test", Encoding.UTF8.GetBytes("test2"));
            channel.BasicPublish("", "test", Encoding.UTF8.GetBytes("test3"));

            // Act
            var queues = await client.GetQueuesAsync();

            // Assert
            queues.Should().ContainSingle().Which.Messages.Should().Be(3);
        }
        finally
        {
            await rabbitMq.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetQueuesAsync_ShouldBeEmpty()
    {
        var rabbitMq = new RabbitMqFixture();
        try
        {
            await rabbitMq.InitializeAsync();

            // Arrange
            var options = Options.Create(new Server.Models.RabbitMqOptions()
            {
                Username = rabbitMq.Username,
                Password = rabbitMq.Password,
                Hostname = rabbitMq.Hostname,
                HttpApiPort = rabbitMq.HttpApiPort,
                AmqpPort = rabbitMq.AmqpPort
            });
            var client = new RabbitMqHttpClient(options);

            // Act
            var queues = await client.GetQueuesAsync();

            // Assert
            queues.Should().BeEmpty();
        }
        finally
        {
            await rabbitMq.DisposeAsync();
        }
    }
}

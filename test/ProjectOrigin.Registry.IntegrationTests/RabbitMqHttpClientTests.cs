using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using ProjectOrigin.Registry.IntegrationTests.Fixtures;
using ProjectOrigin.Registry.MessageBroker;
using ProjectOrigin.Registry.Options;
using RabbitMQ.Client;
using Xunit;
using MsOptions = Microsoft.Extensions.Options.Options;

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
            var options = MsOptions.Create(new RabbitMqOptions()
            {
                Username = RabbitMqFixture.Username,
                Password = RabbitMqFixture.Password,
                Hostname = rabbitMq.Hostname,
                HttpApiPort = rabbitMq.HttpApiPort,
                AmqpPort = rabbitMq.AmqpPort
            });
            var client = new RabbitMqHttpClient(new HttpClient(), options);

            var factory = new ConnectionFactory()
            {
                HostName = rabbitMq.Hostname,
                Port = rabbitMq.AmqpPort,
                UserName = RabbitMqFixture.Username,
                Password = RabbitMqFixture.Password,
            };

            using var con = await factory.CreateConnectionAsync();
            using var channel = await con.CreateChannelAsync();

            await channel.QueueDeclareAsync("test", true, false, false, null);
            await channel.BasicPublishAsync("", "test", Encoding.UTF8.GetBytes("test1"));
            await channel.BasicPublishAsync("", "test", Encoding.UTF8.GetBytes("test2"));
            await channel.BasicPublishAsync("", "test", Encoding.UTF8.GetBytes("test3"));
            await con.CloseAsync();
            await Task.Delay(1000);

            // Act
            var queues = await client.GetQueuesAsync();

            // Assert
            queues.Should().ContainSingle().Which.Messages.Should().Be(3, "Messages on the queue should be 3");
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
            var options = MsOptions.Create(new RabbitMqOptions()
            {
                Username = RabbitMqFixture.Username,
                Password = RabbitMqFixture.Password,
                Hostname = rabbitMq.Hostname,
                HttpApiPort = rabbitMq.HttpApiPort,
                AmqpPort = rabbitMq.AmqpPort
            });
            var client = new RabbitMqHttpClient(new HttpClient(), options);

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

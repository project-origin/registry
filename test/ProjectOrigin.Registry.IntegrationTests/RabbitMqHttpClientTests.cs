using System;
using System.Linq;
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

            // Assert
            await WaitForQueueToBePopulatedAsync(client, "test", expectedMessageCount: 3, timeout: TimeSpan.FromSeconds(10));
        }
        finally
        {
            await rabbitMq.DisposeAsync();
        }
    }

    private async Task WaitForQueueToBePopulatedAsync(RabbitMqHttpClient client, string queueName, int expectedMessageCount, TimeSpan timeout)
    {

        var began = DateTimeOffset.UtcNow;
        while (true)
        {
            var queues = await client.GetQueuesAsync();
            var queue = queues.Single(q => q.Name == queueName);

            if (queue != null && queue.Messages == expectedMessageCount)
                return;

            await Task.Delay(100);

            if (began + timeout < DateTimeOffset.UtcNow)
                throw new TimeoutException($"Queue '{queueName}' did not reach expected message count of {expectedMessageCount} within the timeout period.");
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

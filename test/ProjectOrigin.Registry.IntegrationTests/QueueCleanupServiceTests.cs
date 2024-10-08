using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectOrigin.Registry.Extensions;
using Xunit;
using RabbitMQ.Client;
using Google.Protobuf;
using System.Linq;
using ProjectOrigin.Registry.Options;
using System.Net.Http;
using ProjectOrigin.Registry.MessageBroker;
using MsOptions = Microsoft.Extensions.Options.Options;
using ProjectOrigin.Registry.IntegrationTests.Fixtures;

namespace ProjectOrigin.Registry.Tests;

public class QueueCleanupServiceTests
{
    [Theory]
    [InlineData(5, 3, 3, 3)]
    [InlineData(3, 5, 3, 3)]
    [InlineData(5, 5, 3, 3)]
    public async Task ShouldCleanupQueue(int queuesBefore, int threadsBefore, int queuesAfter, int threadsAfter)
    {
        var rabbitMq = new RabbitMqFixture();
        try
        {
            await rabbitMq.InitializeAsync();

            // Arrange
            var numberOfMessages = 10000;
            var rabbitMqOptions = MsOptions.Create(new RabbitMqOptions
            {
                Username = RabbitMqFixture.Username,
                Password = RabbitMqFixture.Password,
                Hostname = rabbitMq.Hostname,
                HttpApiPort = rabbitMq.HttpApiPort,
                AmqpPort = rabbitMq.AmqpPort
            });

            await using var channelPool = new RabbitMqChannelPool(rabbitMqOptions);
            var httpClient = new RabbitMqHttpClient(new HttpClient(), rabbitMqOptions);

            List<V1.Transaction> transactions = new();
            for (var i = 0; i < numberOfMessages; i++)
            {
                transactions.Add(new Fixture().Create<V1.Transaction>());
            }

            var queueResolver1 = new ConsistentHashRingQueueResolver(MsOptions.Create(new TransactionProcessorOptions
            {
                Servers = queuesBefore,
                ServerNumber = 0,
                Threads = threadsBefore,
                Weight = 10,
            }));

            using (var channel = await channelPool.GetChannelAsync())
            {
                foreach (var transaction in transactions)
                {
                    var queue = queueResolver1.GetQueueName(transaction);

                    await channel.Channel.QueueDeclareAsync(queue, true, false, false, null);
                    await channel.Channel.BasicPublishAsync("", queue, transaction.ToByteArray());
                }
            }

            var queueResolver2 = new ConsistentHashRingQueueResolver(MsOptions.Create(new TransactionProcessorOptions
            {
                Servers = queuesAfter,
                ServerNumber = 0,
                Threads = threadsAfter,
                Weight = 10,
            }));

            var uot = new QueueCleanupService(
                Mock.Of<ILogger<QueueCleanupService>>(),
                httpClient,
                channelPool,
                queueResolver2
            );

            var resultBefore = await httpClient.GetQueuesAsync();
            resultBefore.Sum(x => x.Messages).Should().Be(numberOfMessages);
            resultBefore.Should().HaveCount(queuesBefore * threadsBefore);

            // Act
            await uot.StartAsync(CancellationToken.None);
            await Task.Delay(10000);
            await uot.StopAsync(CancellationToken.None);

            // Assert
            var resultAfter = await httpClient.GetQueuesAsync();
            resultAfter.Sum(x => x.Messages).Should().Be(numberOfMessages);
            resultAfter.Should().HaveCount(queuesAfter * threadsAfter);
        }
        finally
        {
            await rabbitMq.DisposeAsync();
        }
    }
}



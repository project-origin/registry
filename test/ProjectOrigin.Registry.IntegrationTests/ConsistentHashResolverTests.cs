using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.IntegrationTests.Extensions;
using ProjectOrigin.Registry.Server.Options;
using ProjectOrigin.Registry.Server.Services;
using Xunit;

namespace ProjectOrigin.Registry.IntegrationTests;

public class ConsistentHashResolverTests
{
    [Fact]
    public void GetQueueInRing_VerifyDistribution()
    {
        Random r = new Random(42); // Seed the random number generator to get consistent results
        var options = new TransactionProcessorOptions
        {
            ServerNumber = 0,
            Servers = 3,
            Threads = 5,
            Weight = 20
        };
        var number = 100000;
        var expected = number / (options.Servers * options.Threads);
        var resolver = new ConsistentHashRingQueueResolver(Options.Create(options));

        ConcurrentDictionary<string, int> queueCounts = new ConcurrentDictionary<string, int>();
        for (int i = 0; i < number; i++)
        {
            var s = r.GenerateString();
            var queue = resolver.GetQueueName(s);
            queueCounts.AddOrUpdate(queue, 1, (key, value) => value + 1);
        }

        double deviation = NormalizedStandardDeviation(queueCounts.Select(x => x.Value));

        deviation.Should().BeLessThan(0.2);
    }

    [Fact]
    public void GetQueue_ReturnsConsistentQueue()
    {
        var options = new TransactionProcessorOptions
        {
            ServerNumber = 0,
            Servers = 2,
            Threads = 2,
            Weight = 2
        };
        var resolver = new ConsistentHashRingQueueResolver(Options.Create(options));

        var queue = resolver.GetQueueName("test");

        Assert.Equal("registry_0.verifier_1", queue);
    }

    [Fact]
    public void RemovingServer_VerifyOnlyPartialReallocation()
    {
        var totalNumber = 100000;

        var resolverOld = new ConsistentHashRingQueueResolver(Options.Create(new TransactionProcessorOptions
        {
            ServerNumber = 0,
            Servers = 10,
            Threads = 2,
            Weight = 20
        }));

        var resolverNew = new ConsistentHashRingQueueResolver(Options.Create(new TransactionProcessorOptions
        {
            ServerNumber = 0,
            Servers = 9,
            Threads = 2,
            Weight = 20
        }));

        var sameQueue = 0;

        for (int i = 0; i < totalNumber; i++)
        {
            var queueOld = resolverOld.GetQueueName(i.ToString());
            var queueNew = resolverNew.GetQueueName(i.ToString());

            if (queueOld == queueNew)
            {
                sameQueue++;
            }
        }

        Assert.True(sameQueue > totalNumber * 0.8);
    }

    /// <summary>
    /// Calculates the standard deviation of a list of numbers as a percentage of the mean
    /// </summary>
    private static double NormalizedStandardDeviation(IEnumerable<int> numbers)
    {
        double mean = numbers.Average();
        double sumOfSquaresOfDifferences = numbers.Select(value => (value - mean) * (value - mean)).Sum();
        double standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / numbers.Count());
        return standardDeviation / mean;
    }
}

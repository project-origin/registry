using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Murmur;
using ProjectOrigin.Registry.Server.Interfaces;
using ProjectOrigin.Registry.Server.Options;

namespace ProjectOrigin.Registry.Server.Services;

public partial class ConsistentHashRingQueueResolver : IQueueResolver
{
    private readonly SortedDictionary<uint, string> _ring = new SortedDictionary<uint, string>();
    private readonly TransactionProcessorOptions _options;

    public ConsistentHashRingQueueResolver(IOptions<TransactionProcessorOptions> options)
    {
        _options = options.Value;
        PopulateDictionary(options.Value);
    }

    public string GetQueueName(string streamId) => GetQueueInRing(Encoding.UTF8.GetBytes(streamId));

    public string GetQueueName(int server, int verifier) => $"registry_{server}.verifier_{verifier}";

    public string GetQueueInRing(byte[] bytes)
    {
        var position = GetConsistentRingPosition(bytes);
        var firstNode = _ring.FirstOrDefault(node => node.Key >= position);
        if (firstNode.Value is null)
        {
            // Wrap around to the first node in the ring
            firstNode = _ring.First();
        }
        return firstNode.Value;
    }

    private void PopulateDictionary(TransactionProcessorOptions options)
    {
        for (int server = 0; server < options.Servers; server++)
        {
            for (int verifier = 0; verifier < options.Threads; verifier++)
            {
                for (int weight = 0; weight < options.Weight; weight++)
                {
                    var queue = GetQueueName(server, verifier);
                    var position = GetConsistentRingPosition(Encoding.UTF8.GetBytes(queue + $".weight_{weight}"));
                    _ring[position] = queue;
                }
            }
        }
    }

    [GeneratedRegex(@"registry_(.+)\.verifier_(.+)", RegexOptions.Compiled, 10)]
    private static partial Regex MultipleWhitespacesGeneratedRegex();
    public IEnumerable<string> GetInactiveQueues(IEnumerable<string> queues)
    {
        return queues.Where(queueName =>
        {
            var match = MultipleWhitespacesGeneratedRegex().Match(queueName);
            if (match.Success)
            {
                var server = int.Parse(match.Groups[1].Value);
                var verifier = int.Parse(match.Groups[2].Value);

                if (server >= _options.Servers ||
                    verifier >= _options.Threads)
                {
                    return true;
                }
            }
            return false;
        });
    }

    private static uint GetConsistentRingPosition(byte[] bytes)
    {
        using (var algorithm = MurmurHash.Create32())
        {
            var hashedBytes = algorithm.ComputeHash(bytes);
            return BitConverter.ToUInt32(hashedBytes);
        }
    }
}

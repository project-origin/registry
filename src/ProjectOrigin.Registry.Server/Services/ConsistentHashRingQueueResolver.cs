using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.Server.Interfaces;
using ProjectOrigin.Registry.Server.Models;

public class ConsistentHashRingQueueResolver : IQueueResolver
{
    private readonly SortedDictionary<uint, string> _ring = new SortedDictionary<uint, string>();

    public ConsistentHashRingQueueResolver(IOptions<ProcessOptions> options)
    {
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

    private void PopulateDictionary(ProcessOptions options)
    {
        for (int server = 0; server < options.Servers; server++)
        {
            for (int verifier = 0; verifier < options.VerifyThreads; verifier++)
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

    private uint GetConsistentRingPosition(byte[] bytes)
    {
        using (var algorithm = MD5.Create())
        {
            var hashedBytes = algorithm.ComputeHash(bytes);
            return BitConverter.ToUInt32(hashedBytes);
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace ProjectOrigin.VerifiableEventStore.Extensions;

public static class IEnumerableMerkleExtension
{
    public static byte[] CalculateMerkleRoot<T>(this IList<T> events, Func<T, byte[]> selector)
    {
        if (!events.Any())
        {
            throw new ArgumentException("Can not CalculateMerkleRoot on an empty collection.", nameof(events));
        }

        var balancedCount = GetBalancedNodeCount(events);

        return RecursiveShaNodes(events.Select(selector), balancedCount);
    }

    public static IEnumerable<byte[]> GetRequiredHashes<T>(this IList<T> events, Func<T, byte[]> selector, int leafIndex)
    {
        if (leafIndex > events.Count - 1)
        {
            throw new ArgumentException("leafIndex can not be greater than the number of events.", nameof(leafIndex));
        }

        var balancedCount = GetBalancedNodeCount(events);

        return RecursiveGetRequiredHashes(events.Select(selector), leafIndex, balancedCount);
    }

    private static IEnumerable<byte[]> RecursiveGetRequiredHashes(IEnumerable<byte[]> events, int leafIndex, int balancedCount)
    {
        if (balancedCount == 2)
        {
            if (leafIndex == 0 && events.Count() == 2)
                yield return SHA256.HashData(events.Last());
            else if (leafIndex == 1)
                yield return SHA256.HashData(events.First());
        }
        else
        {
            var halfSize = balancedCount / 2;
            if (leafIndex >= halfSize)
            {
                yield return RecursiveShaNodes(events.Take(halfSize), halfSize);
                foreach (var x in RecursiveGetRequiredHashes(events.Skip(halfSize), leafIndex - halfSize, halfSize))
                {
                    yield return x;
                }
            }
            else
            {
                foreach (var x in RecursiveGetRequiredHashes(events.Take(halfSize), leafIndex, halfSize))
                {
                    yield return x;
                }
                yield return RecursiveShaNodes(events.Skip(halfSize), halfSize);
            }
        }
    }

    private static byte[] RecursiveShaNodes(IEnumerable<byte[]> nodes, int balancedCount)
    {
        if (nodes.Count() == 1)
        {
            var data = SHA256.HashData(nodes.Single());
            for (int i = (int)Math.Log(balancedCount, 2); i > 0; i--)
            {
                var doubleData = data.Concat(data);
                data = SHA256.HashData(doubleData.ToArray());
            }
            return data;
        }
        else
        {
            var halfSize = balancedCount / 2;

            var left = RecursiveShaNodes(nodes.Take(halfSize), halfSize);
            var right = RecursiveShaNodes(nodes.Skip(halfSize).DefaultIfEmpty(nodes.Last()), halfSize);

            return SHA256.HashData(left.Concat(right).ToArray());
        }
    }

    private static int GetBalancedNodeCount<T>(IEnumerable<T> enumarble)
    {
        return (int)Math.Pow(2, Math.Ceiling(Math.Log(enumarble.Count(), 2)));
    }
}

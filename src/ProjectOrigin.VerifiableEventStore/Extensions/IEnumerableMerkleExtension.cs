using System.Security.Cryptography;

namespace ProjectOrigin.VerifiableEventStore.Extensions;

public static class IEnumerableMerkleExtension
{
    public static byte[] CalculateMerkleRoot<T>(this IEnumerable<T> events, Func<T, byte[]> selector)
    {
        if (!events.Any())
        {
            throw new ArgumentException("Can not CalculateMerkleRoot on an empty collection.", nameof(events));
        }
        if (!IsPowerOfTwo(events.Count()))
        {
            throw new NotSupportedException("CalculateMerkleRoot currently only supported on exponents of 2");
        }

        return RecursiveShaNodes(events.Select(selector));
    }

    public static IEnumerable<byte[]> GetRequiredHashes<T>(this IEnumerable<T> events, Func<T, byte[]> selector, int leafIndex)
    {
        return RecursiveGetRequiredHashes(events.Select(selector), leafIndex);
    }

    private static IEnumerable<byte[]> RecursiveGetRequiredHashes(IEnumerable<byte[]> events, int leafIndex)
    {
        if (events.Count() == 2)
        {
            yield return SHA256.HashData(events.Skip(1 - leafIndex).First());
        }
        else
        {
            var i = events.Count() / 2;
            if (leafIndex >= i)
            {
                yield return RecursiveShaNodes(events.Take(i));
                foreach (var x in RecursiveGetRequiredHashes(events.Skip(i), leafIndex - i))
                {
                    yield return x;
                }
            }
            else
            {
                foreach (var x in RecursiveGetRequiredHashes(events.Take(i), leafIndex))
                {
                    yield return x;
                }
                yield return RecursiveShaNodes(events.Skip(i));
            }
        }
    }

    private static byte[] RecursiveShaNodes(IEnumerable<byte[]> nodes)
    {
        if (nodes.Count() == 1)
        {
            return SHA256.HashData(nodes.Single());
        }

        var newList = new List<byte[]>();

        for (var i = 0; i < nodes.Count(); i = i + 2)
        {
            var left = SHA256.HashData(nodes.Skip(i).First());
            var right = SHA256.HashData(nodes.Skip(i + 1).First());

            newList.Add(left.Concat(right).ToArray());
        }

        return RecursiveShaNodes(newList);
    }

    private static bool IsPowerOfTwo(int x)
    {
        return (x & (x - 1)) == 0;
    }
}

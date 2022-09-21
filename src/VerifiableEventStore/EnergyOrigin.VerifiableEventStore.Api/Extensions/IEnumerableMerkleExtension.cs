using System.Security.Cryptography;

namespace EnergyOrigin.VerifiableEventStore.Api.Extensions;

public static class IEnumerableMerkleExtension
{
    public static byte[] CalculateMerkleRoot<T>(this IEnumerable<T> events, Func<T, byte[]> selector)
    {
        if (!IsPowerOfTwo(events.Count()))
        {
            throw new NotSupportedException("CalculateMerkleRoot currently only supported on exponents of 2");
        }

        return RecursiveShaNodes(events.Select(selector));
    }

    private static byte[] RecursiveShaNodes(IEnumerable<byte[]> nodes)
    {
        if (nodes.Count() == 1)
        {
            return SHA256.HashData(nodes.Single());
        }

        List<byte[]> newList = new List<byte[]>();

        for (int i = 0; i < nodes.Count(); i = i + 2)
        {
            var left = SHA256.HashData(nodes.Skip(i).First());
            var right = SHA256.HashData(nodes.Skip(i + 1).First());

            var combined = new byte[left.Length + right.Length];

            left.CopyTo(combined, 0);
            right.CopyTo(combined, left.Length);

            newList.Add(combined);
        }

        return RecursiveShaNodes(newList);
    }

    private static bool IsPowerOfTwo(int x)
    {
        return (x & (x - 1)) == 0;
    }
}

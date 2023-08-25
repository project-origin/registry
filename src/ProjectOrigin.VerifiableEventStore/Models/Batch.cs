using System.Linq;
using System.Security.Cryptography;
using Google.Protobuf;

namespace ProjectOrigin.VerifiableEventStore.Models;

public sealed record BatchHash(byte[] Data)
{
    public static BatchHash FromHeader(ImmutableLog.V1.BlockHeader batchHeader) => new(SHA256.HashData(batchHeader.ToByteArray()));

    public bool Equals(BatchHash? right)
    {
        if (right is null)
            return false;

        if (Data == null || right.Data == null)
        {
            return Data == right.Data;
        }
        return Data.SequenceEqual(right.Data);
    }
    public override int GetHashCode()
    {
        return Data.Sum(b => b);
    }
}

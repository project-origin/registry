using System.Linq;
using System.Security.Cryptography;
using Google.Protobuf;

namespace ProjectOrigin.Registry.Repository.Models;

public sealed record BlockHash(byte[] Data)
{
    public static BlockHash FromHeader(Registry.V1.BlockHeader blockHeader) => new(SHA256.HashData(blockHeader.ToByteArray()));

    public bool Equals(BlockHash? right)
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

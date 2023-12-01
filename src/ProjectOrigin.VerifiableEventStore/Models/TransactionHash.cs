using System;
using System.Linq;
using StackExchange.Redis;

namespace ProjectOrigin.VerifiableEventStore.Models;

public sealed record TransactionHash(byte[] Data)
{
    private readonly Lazy<string> _base64String = new(() => Convert.ToBase64String(Data));

    public bool Equals(TransactionHash? right)
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

    public override string ToString() => _base64String.Value;

    public static implicit operator RedisKey(TransactionHash transactionHash)
    {
        return transactionHash.ToString();
    }
}

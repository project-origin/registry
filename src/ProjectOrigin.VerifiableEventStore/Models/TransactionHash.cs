using System.Linq;

namespace ProjectOrigin.VerifiableEventStore.Models;

public sealed record TransactionHash(byte[] Data)
{
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
}

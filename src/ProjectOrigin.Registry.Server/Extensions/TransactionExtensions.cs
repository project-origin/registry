using System.Security.Cryptography;
using Google.Protobuf;
using SimpleBase;

namespace ProjectOrigin.Registry.Server.Extensions;

public static class TransactionExtensions
{
    public static V1.TransactionId GetTransactionId(this V1.Transaction transaction)
    {
        return new V1.TransactionId
        {
            Value = Base58.Bitcoin.Encode(SHA256.HashData(transaction.ToByteArray()))
        };
    }
}

using System.Security.Cryptography;
using Google.Protobuf;
using SimpleBase;

namespace ProjectOrigin.Registry.Server.Extensions;

public static class TransactionExtensions
{
    public static string GetTransactionId(this V1.Transaction transaction)
    {
        return Base58.Bitcoin.Encode(SHA256.HashData(transaction.ToByteArray()));
    }
}

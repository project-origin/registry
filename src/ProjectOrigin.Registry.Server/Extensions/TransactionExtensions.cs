using System.Security.Cryptography;
using Google.Protobuf;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.Registry.Server.Extensions;

public static class TransactionExtensions
{
    public static TransactionHash GetTransactionHash(this V1.Transaction transaction)
    {
        return new TransactionHash(SHA256.HashData(transaction.ToByteArray()));
    }
}

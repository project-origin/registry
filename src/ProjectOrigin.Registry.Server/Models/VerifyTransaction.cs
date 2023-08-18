using Google.Protobuf;

namespace ProjectOrigin.Registry.Server;

public record VerifyTransaction(byte[] Transaction)
{
    public V1.Transaction ToTransaction() => V1.Transaction.Parser.ParseFrom(Transaction);

    public static VerifyTransaction Create(V1.Transaction transaction)
    {
        return new VerifyTransaction(transaction.ToByteArray());
    }
}

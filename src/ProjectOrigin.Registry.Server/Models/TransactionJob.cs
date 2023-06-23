using Google.Protobuf;

namespace ProjectOrigin.Registry.Server;

public record TransactionJob(byte[] Transaction)
{
    public Registry.V1.Transaction ToTransaction() => Registry.V1.Transaction.Parser.ParseFrom(Transaction);

    public static TransactionJob Create(Registry.V1.Transaction transaction)
    {
        return new TransactionJob(transaction.ToByteArray());
    }
}

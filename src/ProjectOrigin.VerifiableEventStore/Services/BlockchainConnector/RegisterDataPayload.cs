using System.Buffers.Binary;
using ConcordiumNetSdk.Types;

namespace ConcordiumNetSdk.Transactions;

/// <summary>
/// Represents a contents of the register data transaction.
/// </summary>
public class RegisterDataPayload : IAccountTransactionPayload
{
    private RegisterDataPayload(byte[] data)
    {
        Data = data;
    }

    /// <summary>
    /// The data to register.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Creates an instance of register data payload.
    /// </summary>
    /// <param name="data">the to register on the ledger.</param>
    public static RegisterDataPayload Create(byte[] data)
    {
        if (data.Length > short.MaxValue) throw new InvalidDataException($"Data in RegisterDataPayload is to long, max length is {short.MaxValue}");

        return new RegisterDataPayload(data);
    }

    public byte[] SerializeToBytes()
    {
        var length = Data.Length;

        var bytes = new byte[3 + length];
        Span<byte> buffer = bytes;

        buffer[0] = (byte)AccountTransactionType.RegisterData;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(1, 2), (ushort)length);
        Data.CopyTo(buffer.Slice(3));

        return bytes;
    }

    public ulong GetBaseEnergyCost() => 300;
}

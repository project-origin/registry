using System;
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
        return new RegisterDataPayload(data);
    }

    public byte[] SerializeToBytes()
    {
        byte[] result = new byte[Data.Length + 1];
        Span<byte> span = result;
        result[0] = (byte)AccountTransactionType.RegisterData;
        Data.CopyTo(span.Slice(1));
        return result;
    }

    public ulong GetBaseEnergyCost() => 300;
}

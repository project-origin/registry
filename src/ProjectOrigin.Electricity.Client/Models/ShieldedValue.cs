using System.Numerics;
using Google.Protobuf;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Client.Models;

/// <summary>
/// A ShieldedValue contains a Message and a random R
/// which hides the message with the help of a <a href="xref:pedersen_commitment">Pedersen Commitment</a>.
/// </summary>
public class ShieldedValue
{
    /// <summary>
    /// Contains the message that the shielded value should protect.
    /// </summary>
    public ulong Message { get => (ulong)_commitmentParameters.m; }

    /// <summary>
    /// The random value that protects the message.
    /// </summary>
    public BigInteger R { get => _commitmentParameters.r; }

    private CommitmentParameters _commitmentParameters;

    /// <summary>
    /// Creates a ShieldedValue based on a message and r value.
    /// Validates that the value r is in  the Group.
    /// </summary>
    /// <param name="message">contains the message that one wants to hide.</param>
    /// <param name="r">contains the random value that hides the message.</param>
    public ShieldedValue(ulong message, BigInteger r)
    {
        _commitmentParameters = new CommitmentParameters(message, r, Group.Default);
    }

    /// <summary>
    /// Creates a ShieldedValue based on a message,
    /// automatically creates a random value to hide the message.
    /// </summary>
    /// <param name="message">contains the message that one wants to hide.</param>
    public ShieldedValue(ulong message)
    {
        _commitmentParameters = Group.Default.Commit(message);
    }

    internal V1.Commitment ToProtoCommitment()
    {
        return new V1.Commitment()
        {
            C = ByteString.CopyFrom(_commitmentParameters.C.ToByteArray())
        };
    }

    internal V1.CommitmentProof ToProtoCommitmentProof()
    {
        return new V1.CommitmentProof()
        {
            Message = (ulong)_commitmentParameters.m,
            RValue = ByteString.CopyFrom(_commitmentParameters.r.ToByteArray())
        };
    }
}

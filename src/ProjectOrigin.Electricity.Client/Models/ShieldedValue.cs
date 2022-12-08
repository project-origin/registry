using System.Numerics;
using System.Security.Cryptography;
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
    public uint Message { get => (uint)_commitmentParameters.Message; }

    /// <summary>
    /// The random value that protects the message.
    /// </summary>
    public BigInteger RValue { get => _commitmentParameters.RValue; }

    private CommitmentParameters _commitmentParameters;

    /// <summary>
    /// Creates a ShieldedValue based on a message and r value.
    /// Validates that the value r is in  the Group.
    /// </summary>
    /// <param name="message">contains the message that one wants to hide.</param>
    /// <param name="r">contains the random value that hides the message.</param>
    public ShieldedValue(uint message, BigInteger r)
    {
        _commitmentParameters = Group.Default.CreateParameters(message, r);
    }

    /// <summary>
    /// Creates a ShieldedValue based on a message,
    /// automatically creates a random value to hide the message.
    /// </summary>
    /// <param name="message">contains the message that one wants to hide.</param>
    public ShieldedValue(uint message)
    {
        _commitmentParameters = Group.Default.Commit(message);
    }

    internal V1.SliceId ToSliceId()
    {
        return new V1.SliceId()
        {
            Hash = ByteString.CopyFrom(SHA256.HashData(_commitmentParameters.C.ToByteArray()))
        };
    }

    internal CommitmentParameters ToParams()
    {
        return Group.Default.CreateParameters(Message, RValue);
    }

    internal V1.Commitment ToProtoCommitment()
    {
        return new V1.Commitment()
        {
            Content = ByteString.CopyFrom(_commitmentParameters.C.ToByteArray()),
            RangeProof = ByteString.CopyFrom(_commitmentParameters.RangeProof)
        };
    }
}

using System.Text;

namespace ProjectOrigin.PedersenCommitment;

/// <summary>
/// This class holds the <b>private</b> part of a commitment.
/// the input parameters that should NOT be shared unless
/// one wants to share the actually data within the commitment.
/// </summary>
public record SecretCommitmentInfo
{
    private Ristretto.Scalar _blindingValue;

    public uint Message { get; }

    public ReadOnlySpan<byte> BlindingValue { get => _blindingValue.ToBytes(); }

    public Commitment Commitment
    {
        get
        {
            var point = Generator.Default.Commit(Message, _blindingValue);
            return new Commitment(point.Compress()._bytes);
        }
    }

    public SecretCommitmentInfo(uint message) : this(message, Ristretto.Scalar.Random()) { }
    public SecretCommitmentInfo(uint message, ReadOnlySpan<byte> blinding) : this(message, new Ristretto.Scalar(blinding)) { }
    public SecretCommitmentInfo(uint message, Ristretto.Scalar blinding)
    {
        Message = message;
        _blindingValue = blinding;
    }

    public ReadOnlySpan<byte> CreateRangeProof(string label)
    {
        var labelBytes = Encoding.ASCII.GetBytes(label);
        var (rangeProof, compressedPoint) = Ristretto.RangeProof.ProveSingle(Ristretto.BulletProofGen.Default, Generator.Default, Message, _blindingValue, 32, labelBytes);

        //return rangeProof.bytes;
        return ReadOnlySpan<byte>.Empty; // TODO Proofs
    }

    public static ReadOnlySpan<byte> CreateEqualityProof(SecretCommitmentInfo left, SecretCommitmentInfo right)
    {
        return ReadOnlySpan<byte>.Empty; // TODO Proofs
    }

    public static SecretCommitmentInfo operator +(SecretCommitmentInfo left, SecretCommitmentInfo right)
    {
        var messages = left.Message + right.Message;
        var blinding = left._blindingValue + right._blindingValue;
        return new SecretCommitmentInfo(messages, blinding);
    }

    public static SecretCommitmentInfo operator -(SecretCommitmentInfo left, SecretCommitmentInfo right)
    {
        var messages = left.Message - right.Message;
        var blinding = left._blindingValue - right._blindingValue;
        return new SecretCommitmentInfo(messages, blinding);
    }
}

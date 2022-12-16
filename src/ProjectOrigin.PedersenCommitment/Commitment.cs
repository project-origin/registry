using System.Text;
using ProjectOrigin.PedersenCommitment.Ristretto;

namespace ProjectOrigin.PedersenCommitment;

/// <summary>
/// This class holds the <b>public</b> part of a commitment,
/// this can be shared without leaking any information.
/// </summary>
public record Commitment
{
    private CompressedPoint _compressionPoint;

    public ReadOnlySpan<byte> C { get => _compressionPoint._bytes; }

    internal Ristretto.Point Point { get => _compressionPoint.Decompress(); }

    public Commitment(ReadOnlySpan<byte> bytes)
    {
        _compressionPoint = new Ristretto.CompressedPoint(bytes.ToArray());
    }

    public bool VerifyRangeProof(ReadOnlySpan<byte> rangeProof, string label)
    {
        var labelBytes = Encoding.ASCII.GetBytes(label);

        //Ristretto.RangeProof rangeProof;
        //return rangeProof.VerifySingle(BulletProofGen.Default, Generator.Default, compressedPoint, 32, label);

        return rangeProof.IsEmpty; // TODO Proofs
    }

    public static bool VerifyEqualityProof(ReadOnlySpan<byte> equalityProof, Commitment commitment1, Commitment commitment2)
    {
        return equalityProof.IsEmpty; // TODO Proofs
    }

    public static Commitment operator +(Commitment left, Commitment right)
    {
        var newPoint = left.Point + right.Point;
        return new Commitment(newPoint.Compress()._bytes);
    }

    public static Commitment operator -(Commitment left, Commitment right)
    {
        var newPoint = left.Point - right.Point;
        return new Commitment(newPoint.Compress()._bytes);
    }
}

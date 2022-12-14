using System.Numerics;

namespace ProjectOrigin.PedersenCommitment.Tests;

public class PointTests
{
    [Fact]
    public void Elligator() {
        var seed = new byte[64];
        seed[0] = 2;
        var p = Ristretto.Point.FromUniformBytes(seed);
    }

    [Fact]
    public void CompressDecompress()
    {
        var seed = new byte[64];
        seed[0] = 2;
        var point = Ristretto.Point.FromUniformBytes(seed);
        var a = point.Compress();
        var b = a.Decompress();
        var c = b.Compress();
        Assert.Equal(a, c);
    }

    [Fact]
    public void Sub()
    {
        var seed = new byte[64];
        seed[0] = 2;
        var g = Ristretto.Point.FromUniformBytes(seed);
        var a = g * new Scalar(2);
        var b = g * new Scalar(7);
        var c = g * new Scalar(5);

        Assert.Equal(c, b - a);
    }

    [Fact]
    public void MulBigInteger()
    {
        var seed = new byte[64];
        seed[0] = 2;
        var p = Ristretto.Point.FromUniformBytes(seed);

        var p1 = p * new BigInteger(1);
        Assert.Equal(p, p1);

        var p2 = p * new BigInteger(2);
        Assert.NotEqual(p1, p2);

        var p3 = p * new BigInteger(3);

        var p5 = p2 + p3;

        var p5_ = p * new BigInteger(5);

        Assert.Equal(p5, p5_);
    }

    [Fact]
    public void MulScalar() {
        var seed = new byte[64];
        seed[0] = 2;
        var p = Ristretto.Point.FromUniformBytes(seed);

        var p1 = p * new Scalar(1);
        Assert.Equal(p, p1);

        var p2 = p * new Scalar(2);
        Assert.NotEqual(p1, p2);

        var p3 = p * new Scalar(3);

        var p5 = p2 + p3;

        var p5_ = p * new Scalar(5);

        Assert.Equal(p5, p5_);
    }
}


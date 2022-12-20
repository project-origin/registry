using System.Security.Cryptography;
using System.Text;
using ProjectOrigin.PedersenCommitment.Ristretto;

namespace ProjectOrigin.PedersenCommitment.Tests;

public class PointTests
{
    [Fact]
    public void Test()
    {
        var piBytes = Encoding.ASCII.GetBytes("3.141592653589793238462643383279502884197169");
        var sha1 = SHA512.HashData(piBytes);

        var point = Point.FromUniformBytes(sha1);

        Assert.NotNull(point);
    }


    [Fact]
    public void Elligator()
    {

        var piBytes = Encoding.ASCII.GetBytes("hello world");
        var seed = SHA512.HashData(piBytes);


        var p = Ristretto.Point.FromUniformBytes(seed);

        Assert.NotNull(p);
    }

    // [Fact]
    // public void CompressDecompress()
    // {
    //     var seed = new byte[64];
    //     seed[0] = 2;
    //     var point = Ristretto.Point.FromUniformBytes(seed);
    //     var a = point.Compress();
    //     var b = a.Decompress();
    //     var c = b.Compress();
    //     Assert.Equal(a, c);
    // }

    // [Fact]
    // public void Sub()
    // {
    //     var seed = new byte[64];
    //     seed[0] = 2;
    //     var g = Ristretto.Point.FromUniformBytes(seed);
    //     var a = g * new Scalar(2);
    //     var b = g * new Scalar(7);
    //     var c = g * new Scalar(5);

    //     Assert.Equal(c, b - a);
    // }

    // [Fact]
    // public void MulScalar()
    // {
    //     var seed = new byte[64];
    //     seed[0] = 2;
    //     var p = Ristretto.Point.FromUniformBytes(seed);
    //     var p7 = Ristretto.Point.FromUniformBytes(seed);

    //     var p1 = p * new Scalar(1);

    //     Assert.Equal(p, p1);
    //     var p2 = p * new Scalar(2);
    //     Assert.NotEqual(p1, p2);

    //     var p3 = p * new Scalar(3);

    //     var p5 = p2 + p3;

    //     var p5_ = p * new Scalar(5);

    //     Assert.Equal(p5, p5_);
    // }
}


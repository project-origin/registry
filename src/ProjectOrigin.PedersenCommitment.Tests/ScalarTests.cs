using System.Numerics;
using ProjectOrigin.PedersenCommitment.Ristretto;
using Xunit;

namespace ProjectOrigin.PedersenCommitment.Tests;

public class ScalarTest
{
    [Fact]
    public void HashFromBytes()
    {
        Scalar.HashFromBytes(new byte[] { 0xAA });
        Scalar.HashFromBytes(new byte[128]);
    }

    [Fact]
    public void Equal()
    {
        var a = Scalar.HashFromBytes(new byte[] { 0xAA });
        var b = Scalar.HashFromBytes(new byte[] { 0xAA });
        Assert.Equal(a, b);
    }

    [Fact]
    public void NotEqual()
    {
        var a = Scalar.HashFromBytes(new byte[] { 0xAA });
        var b = Scalar.HashFromBytes(new byte[] { 0xBB });
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Addition()
    {
        var a = new Scalar(1);
        var b = new Scalar(2);
        var c = new Scalar(3);
        Assert.Equal(c, a + b);
    }


    [Fact]
    public void Subtraction()
    {
        var a = new Scalar(5);
        var b = new Scalar(2);
        var c = new Scalar(3);
        Assert.Equal(c, a - b);
    }

    [Fact]
    public void Negate()
    {
        var negatedScalar1 = -(new Scalar(1));
        var expectedValue = BigInteger.Parse("7237005577332262213973186563042994240857116359379907606001950938285454250988");

        AssertExt.SequenceEqual(expectedValue.ToByteArray(), negatedScalar1.ToBytes());
    }
}

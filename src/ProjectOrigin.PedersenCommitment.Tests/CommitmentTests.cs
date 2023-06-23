using AutoFixture;
using Xunit;

namespace ProjectOrigin.PedersenCommitment.Tests;

public class CommitmentTests
{
    [Fact]
    public void Commitment_Addition_Success()
    {
        var m1 = new Fixture().Create<uint>();
        var m2 = new Fixture().Create<uint>();

        var r1 = Ristretto.Scalar.Random();
        var r2 = Ristretto.Scalar.Random();

        var c1 = new SecretCommitmentInfo(m1, r1).Commitment;
        var c2 = new SecretCommitmentInfo(m2, r2).Commitment;

        var cSum = c1 + c2;
        var c3 = new SecretCommitmentInfo(m1 + m2, r1 + r2).Commitment;

        AssertExt.SequenceEqual(c3.C, cSum.C);
    }

    [Fact]
    public void Commitment_ToZero_Success()
    {
        var m2 = new Fixture().Create<uint>();
        var m3 = new Fixture().Create<uint>();

        var cp1 = new SecretCommitmentInfo(m2 + m3);
        var cp2 = new SecretCommitmentInfo(m2);
        var cp3 = new SecretCommitmentInfo(m3);

        var cTo0 = cp1 - cp2 - cp3;

        AssertExt.SequenceEqual(cTo0.Commitment.C, (cp1.Commitment - (cp2.Commitment + cp3.Commitment)).C);
    }

    [Fact]
    public void Commitment_EqualityProof_Success()
    {
        var m2 = new Fixture().Create<uint>();
        var m3 = new Fixture().Create<uint>();

        var cp1 = new SecretCommitmentInfo(m2 + m3);
        var cp2 = new SecretCommitmentInfo(m2);
        var cp3 = new SecretCommitmentInfo(m3);

        var b = SecretCommitmentInfo.CreateEqualityProof(cp1, cp2 + cp3, "test");
        var result = Commitment.VerifyEqualityProof(b, cp1.Commitment, cp2.Commitment + cp3.Commitment, "test");

        Assert.True(result);
    }
}

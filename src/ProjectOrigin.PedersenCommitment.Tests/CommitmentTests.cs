using System.Numerics;

namespace ProjectOrigin.PedersenCommitment.Tests;

public class CommitmentTests
{
    [Fact]
    public void Commitment_CreateDefaultGroup_Success()
    {
        var m = new BigInteger(51);
        var r = BigInteger.Parse("35425707649260674451675575047706194335233578342436579356033362");
        var c = Commitment.Create(Group.Default, m, r);

        Assert.Equal(BigInteger.Parse("508952004547232011284116788462783521716447076625673355453585453"), c.C);
    }

    [Fact]
    public void Commitment_CreateOnOtherGroup_ThrowsException()
    {
        var group1 = Group.Default;
        var group2 = Group.Create();

        var m = new Fixture().Create<BigInteger>();

        var r = group1.RandomR();
        var c = Commitment.Create(group1, m, r);

        Assert.Throws<InvalidDataException>(() => new Commitment(c.C, group2));
    }

    [Fact]
    public void Commitment_Addition_Success()
    {
        var group = Group.Create();

        var m1 = new Fixture().Create<BigInteger>();
        var m2 = new Fixture().Create<BigInteger>();
        var r1 = group.RandomR();
        var r2 = group.RandomR();

        var c1 = Commitment.Create(group, m1, r1);
        var c2 = Commitment.Create(group, m2, r2);

        var cSum = c1 * c2;
        var c3 = Commitment.Create(group, m1 + m2, r1 + r2);

        Assert.Equal(c3.C, cSum.C);
    }

    [Fact]
    public void Commitment_ToZero_Success()
    {
        var group = Group.Create();

        var m2 = new Fixture().Create<BigInteger>();
        var m3 = new Fixture().Create<BigInteger>();

        var cp1 = group.Commit(m2 + m3);
        var cp2 = group.Commit(m2);
        var cp3 = group.Commit(m3);

        var rTo0 = (cp1.r - (cp2.r + cp3.r)).MathMod(group.q);
        var cTo0 = Commitment.Create(group, 0, rTo0);

        Assert.Equal(cTo0.C, (cp1.Commitment / (cp2.Commitment * cp3.Commitment)).C);
    }

    [Fact]
    public void Group_Commit_Success()
    {
        var group = Group.Create();

        var m = new Fixture().Create<BigInteger>();

        Assert.NotNull(group.Commit(m));
    }

    [Fact]
    public void CommmitmentParameters_Verify_Success()
    {
        var group = Group.Create();

        var m = new Fixture().Create<BigInteger>();

        var commmitmentParameters = group.Commit(m);

        Assert.True(commmitmentParameters.Verify(Commitment.Create(group, m, commmitmentParameters.r).C));
    }
}

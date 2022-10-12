using System.Numerics;

namespace EnergyOrigin.PedersenCommitment.Tests;

public class CommitmentTests
{
    [Fact]
    public void Commitment_CreateHardcodedData_Success()
    {
        var q = BigInteger.Parse("1202338925383056856633248983477279053213530904230949832491043");
        var p = BigInteger.Parse("519410415765480562065563560862184550988245350627770327636130577");
        var g = BigInteger.Parse("101455240839796123327081946568988620614409829310312504112082811");
        var h = BigInteger.Parse("162315825204305527697219690878071619973299472069112727941372177");
        var group = new Group(p, q, g, h);

        var m = new BigInteger(51);
        var r = BigInteger.Parse("35425707649260674451675575047706194335233578342436579356033362");
        var c = Commitment.Create(group, m, r);

        Assert.Equal(BigInteger.Parse("508952004547232011284116788462783521716447076625673355453585453"), c.C);
    }

    [Fact]
    public void Commitment_CreateOnOtherGroup_ThrowsException()
    {
        var group1 = Group.Create();
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
        var m1 = m2 + m3;

        var r1 = group.RandomR();
        var r2 = group.RandomR();
        var r3 = group.RandomR();

        var c1 = Commitment.Create(group, m1, r1);
        var c2 = Commitment.Create(group, m2, r2);
        var c3 = Commitment.Create(group, m3, r3);

        var rTo0 = (r1 - (r2 + r3)).MathMod(group.q);
        var cTo0 = Commitment.Create(group, 0, rTo0);

        Assert.Equal(cTo0.C, (c1 / (c2 * c3)).C);
    }
}

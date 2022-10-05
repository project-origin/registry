using System.Numerics;

namespace EnergyOrigin.PedersenCommitment.Tests;

public class CommitmentTests
{
    private Group GetParams()
    {
        var q = BigInteger.Parse("1202338925383056856633248983477279053213530904230949832491043");
        var p = BigInteger.Parse("519410415765480562065563560862184550988245350627770327636130577");
        var g = BigInteger.Parse("101455240839796123327081946568988620614409829310312504112082811");
        var h = BigInteger.Parse("162315825204305527697219690878071619973299472069112727941372177");
        return new Group(p, q, g, h);
    }

    [Fact]
    public void TestCommitment_1()
    {
        var curve = GetParams();

        var m = new BigInteger(51);
        var r = BigInteger.Parse("35425707649260674451675575047706194335233578342436579356033362");
        var c = Commitment.Create(curve, m, r);

        Assert.Equal(BigInteger.Parse("508952004547232011284116788462783521716447076625673355453585453"), c.C);
    }

    [Fact]
    public void TestCommitment_2()
    {
        var curve = GetParams();

        var m = new BigInteger(63);
        var r = BigInteger.Parse("343266335926063514703096506657074904310735112622484906460880163");
        var c = Commitment.Create(curve, m, r);

        Assert.Equal(BigInteger.Parse("294471355353168351648869556430803702099579489432772179741534004"), c.C);
    }

    [Fact]
    public void Commitment_Addition_Success()
    {
        var curve = GetParams();

        var m1 = new BigInteger(51);
        var m2 = new BigInteger(63);
        var r1 = BigInteger.Parse("35425707649260674451675575047706194335233578342436579356033362");
        var r2 = BigInteger.Parse("343266335926063514703096506657074904310735112622484906460880163");

        var c1 = Commitment.Create(curve, m1, r1);
        var c2 = Commitment.Create(curve, m2, r2);

        var cSum = c1 * c2;
        var c3 = Commitment.Create(curve, m1 + m2, r1 + r2);

        Assert.Equal(c3.C, cSum.C);
    }

    public void Commitment_ToZero_Success()
    {
        var group = GetParams();

        var m2 = new BigInteger(51);
        var m3 = new BigInteger(63);
        var m1 = m2 + m3;

        var r1 = BigInteger.Parse("35425707649260674451675575047706194335233578342436579356033362");
        var r2 = BigInteger.Parse("343266335926063514703096506657074904310735112622484906460880163");
        var r3 = BigInteger.Parse("606744516755750477061957706194335204310735112622484906460803336");

        var c1 = Commitment.Create(group, m1, r1);

        var c2 = Commitment.Create(group, m2, r2);
        var c3 = Commitment.Create(group, m3, r3);

        var r0 = r1 - (r2 + r3) % group.p;

        var c0 = Commitment.Create(group, 0, r0);

        Assert.Equal(c0.C, (c1 / (c2 * c3)).C);
    }
}

using System.Numerics;

namespace ProjectOrigin.PedersenCommitment.Tests;

public class GroupTests
{
    [Fact]
    public void CreateGroup_FromStaticNumber_Success()
    {
        var q = BigInteger.Parse("1202338925383056856633248983477279053213530904230949832491043");
        var p = BigInteger.Parse("519410415765480562065563560862184550988245350627770327636130577");
        var g = BigInteger.Parse("101455240839796123327081946568988620614409829310312504112082811");
        var h = BigInteger.Parse("162315825204305527697219690878071619973299472069112727941372177");
        var group = new Group(p, q, g, h);

        Assert.NotNull(group);
    }

    [Fact]
    public void CreateGroup_Generate_Success()
    {
        var group = Group.Create();

        Assert.NotNull(group);
    }
}

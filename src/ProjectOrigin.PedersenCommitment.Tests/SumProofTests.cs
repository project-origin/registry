using ProjectOrigin.PedersenCommitment.Ristretto;

namespace ProjectOrigin.PedersenCommitment.Tests;

public class SumProofTests
{


    [Fact]
    public void SanityCheck()
    {
        var pc_gens = Generator.Default;
        var r = Scalar.Random();
        var c0 = pc_gens.Commit(0, r);
        var c1 = pc_gens.H() * r;
        c0.GutSpill();
        c1.GutSpill();
        Assert.Equal(c0, c1);
    }

    [Fact]
    public void Zero()
    {
        var pc_gens = Generator.Default;
        var r = Scalar.Random();
        var c = pc_gens.Commit(0, r);

        var proof = ZeroProof.Prove(pc_gens, r);

        Assert.True(proof.Verify(pc_gens, c));
    }

    [Fact]
    public void Equal()
    {
        var pc_gens = Generator.Default;
        var r0 = Scalar.Random();
        var r1 = Scalar.Random();
        var c0 = pc_gens.Commit(42, r0);
        var c1 = pc_gens.Commit(42, r1);

        var c = c0 - c1;
        var r = r0 - r1;

        var proof = ZeroProof.Prove(pc_gens, r);
        Assert.True(proof.Verify(pc_gens, c));
    }

    [Fact]
    public void Sum()
    {
        var pc_gens = Generator.Default;
        var r0 = Scalar.Random();
        var r1 = Scalar.Random();
        var r2 = Scalar.Random();
        var r3 = Scalar.Random();
        var c0 = pc_gens.Commit(10, r0);
        var c1 = pc_gens.Commit(3, r1);
        var c2 = pc_gens.Commit(5, r2);
        var c3 = pc_gens.Commit(2, r3);

        var c = c0 - (c1 + c2 + c3);
        var r = r0 - (r1 + r2 + r3);

        var proof = ZeroProof.Prove(pc_gens, r);
        Assert.True(proof.Verify(pc_gens, c));
    }


    [Fact]
    public void Wrong()
    {
        var pc_gens = Generator.Default;
        var r = Scalar.Random();
        var c = pc_gens.Commit(42, r); // None Zero
        var proof = ZeroProof.Prove(pc_gens, r);
        Assert.False(proof.Verify(pc_gens, c));
    }

}

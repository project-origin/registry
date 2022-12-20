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
    public void ItWorks()
    {
        var pc_gens = Generator.Default;
        var r = Scalar.Random();
        var c = pc_gens.Commit(0, r);

        var proof = ZeroProof.Prove(pc_gens, r);

        Assert.True(proof.Verify(pc_gens, c));
    }

}

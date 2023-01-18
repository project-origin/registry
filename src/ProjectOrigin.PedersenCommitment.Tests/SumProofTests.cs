using ProjectOrigin.PedersenCommitment.Ristretto;
using System.Text;

namespace ProjectOrigin.PedersenCommitment.Tests;

public class SumProofTests
{

    [Fact]
    public void SanityCheck()
    {
        // check that a zero commit is the same as doing it manually
        var pc_gens = Generator.Default;
        var r = Scalar.Random();
        var c0 = pc_gens.Commit(0, r);
        var c1 = pc_gens.H() * r;
        Assert.Equal(c0, c1);
    }

    [Fact]
    public void Zero()
    {
        var pc_gens = Generator.Default;
        var label = Encoding.UTF8.GetBytes("test");
        var r = Scalar.Random();
        var c = pc_gens.Commit(0, r);



        var proof = ZeroProof.Prove(pc_gens, r, label);

        Assert.True(proof.Verify(pc_gens, c, label));
    }

    [Fact]
    public void Equal()
    {
        var pc_gens = Generator.Default;
        var label = Encoding.UTF8.GetBytes("test");
        var r0 = Scalar.Random();
        var r1 = Scalar.Random();
        var c0 = pc_gens.Commit(42, r0);
        var c1 = pc_gens.Commit(42, r1);

        var c = c0 - c1;
        var r = r0 - r1;

        var proof = ZeroProof.Prove(pc_gens, r, label);
        Assert.True(proof.Verify(pc_gens, c, label));
    }

    // Test the derivative proofs
    [Fact]
    public void Equal2()
    {
        var pc_gens = Generator.Default;
        var label = Encoding.UTF8.GetBytes("test");
        var r0 = Scalar.Random();
        var r1 = Scalar.Random();
        var c0 = pc_gens.Commit(42, r0);
        var c1 = pc_gens.Commit(42, r1);

        var proof = EqualProof.Prove(pc_gens, r0, r1, label);

        Assert.True(proof.Verify(pc_gens, c0, c1, label));
    }

    [Fact]
    public void Sum()
    {
        var pc_gens = Generator.Default;
        var label = Encoding.UTF8.GetBytes("test");
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

        var proof = ZeroProof.Prove(pc_gens, r, label);
        Assert.True(proof.Verify(pc_gens, c, label));
    }

    // Test the derivative proofs
    [Fact]
    public void Sum2()
    {
        var pc_gens = Generator.Default;
        var label = Encoding.UTF8.GetBytes("test");
        var r0 = Scalar.Random();
        var r1 = Scalar.Random();
        var r2 = Scalar.Random();
        var r3 = Scalar.Random();
        var c0 = pc_gens.Commit(10, r0);
        var c1 = pc_gens.Commit(3, r1);
        var c2 = pc_gens.Commit(5, r2);
        var c3 = pc_gens.Commit(2, r3);

        var proof = SumProof.Prove(pc_gens, label, r0, r1, r2, r3);
        Assert.True(proof.Verify(pc_gens, label, c0, c1, c2, c3));
    }


    [Fact]
    public void NonZeroFails()
    {
        var label = Encoding.UTF8.GetBytes("test");
        var pc_gens = Generator.Default;
        var r = Scalar.Random();
        var c = pc_gens.Commit(42, r); // None Zero, since everything else should fail
        var proof = ZeroProof.Prove(pc_gens, r, label);
        Assert.False(proof.Verify(pc_gens, c, label));
    }

    [Fact]
    public void WrongLabelFails()
    {
        var label0 = Encoding.UTF8.GetBytes("proof");
        var label1 = Encoding.UTF8.GetBytes("verify");
        var pc_gens = Generator.Default;
        var r = Scalar.Random();
        var c = pc_gens.Commit(0, r);
        var proof = ZeroProof.Prove(pc_gens, r, label0);
        Assert.False(proof.Verify(pc_gens, c, label1));
    }

    [Fact]
    public void Serde()
    {
        var label = Encoding.UTF8.GetBytes("test");
        var pc_gens = Generator.Default;
        var r = Scalar.Random();
        var c = pc_gens.Commit(0, r);
        var proof = ZeroProof.Prove(pc_gens, r, label);

        var bytes = proof.Serialize();
        var new_proof = ZeroProof.Deserialize(bytes);
        Assert.True(new_proof.Verify(pc_gens, c, label));
    }

}

using System.Text;
using ProjectOrigin.PedersenCommitment.Ristretto;
using Xunit;

namespace ProjectOrigin.PedersenCommitment.Tests;
public class RangeProofTests
{
    [Fact]
    public void SingleProof()
    {
        var pc_gens = Generator.Default;
        var bp_gens = new BulletProofGen(64, 1);
        var blinding = Scalar.Random();
        var label = Encoding.ASCII.GetBytes("test example");

        var (proof, commit) = RangeProof.ProveSingle(bp_gens, pc_gens, 7, blinding, 32, label);
        var res = proof.VerifySingle(bp_gens, pc_gens, commit, 32, label);
        Assert.True(res);

        var bytes = proof.ToBytes();
        var new_proof = RangeProof.FromBytes(bytes);
        res = new_proof.VerifySingle(bp_gens, pc_gens, commit, 32, label);
        Assert.True(res);

        Assert.NotNull(pc_gens);
    }
}

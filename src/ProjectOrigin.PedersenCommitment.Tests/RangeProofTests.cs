using System.Text;
using ProjectOrigin.PedersenCommitment.Ristretto;

namespace ProjectOrigin.PedersenCommitment.Tests;
public class RangeProofTests
{
    [Fact]
    public void SingleProof()
    {
        var bp_gens = new BulletProofGen(64, 1);
        var blinding = Scalar.Random();
        var label = Encoding.ASCII.GetBytes("test example");

        var (proof, commit) = RangeProof.ProveSingle(bp_gens, Generator.Default, 7, blinding, 32, label);
        var res = proof.VerifySingle(bp_gens, Generator.Default, commit, 32, label);
        Assert.True(res);
    }
}

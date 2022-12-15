namespace ProjectOrigin.PedersenCommitment.Tests;

using ProjectOrigin.PedersenCommitment.Ristretto;
using System.Text;

public class RangeProofTests {

    [Fact]
    public void SingleProof()
    {
        var bp_gens = new BulletProofGen(64, 1);
        var pc_gens = new Generator();
        var blinding = Scalar.Random();
        var label = Encoding.ASCII.GetBytes("test example");

        var proof = RangeProof.ProveSingle(bp_gens, pc_gens, 7, blinding, 32, label);
    }
}

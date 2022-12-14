using System.Runtime.InteropServices;
using System.Numerics;
namespace ProjectOrigin.PedersenCommitment;

public record RangeProof : IDisposable
{
    internal class Native
    {
        [DllImport("rust_ffi", EntryPoint = "rangeproof_prove_single")]
        internal static extern RangeProofWithCommit ProveSingle(IntPtr bp_gen, IntPtr pc_gen, ulong v, IntPtr blinding, uint n, byte[] label, int label_len);

        [DllImport("rust_ffi", EntryPoint = "rangeproof_prove_multiple")]
        internal static extern RangeProofWithCommit ProveMultiple(IntPtr bp_gen, IntPtr pc_gen, ulong[] v, IntPtr blinding, uint n, byte[] label, int label_len, int amount);

        [DllImport("rust_ffi", EntryPoint = "rangeproof_dispose")]
        internal static extern void Dispose(IntPtr self);

        [DllImport("rust_ffi", EntryPoint = "rangeproof_dispose")]
        internal static extern void Verify(IntPtr self);
    }

    private readonly IntPtr ptr;

    private RangeProof(IntPtr ptr)
    {
        this.ptr = ptr;
    }

    public RangeProof ProveSingle
        (
            BulletProofGen bp_gen,
            Generator pc_gen,
            ulong v,
            Scalar blinding,
            uint n,
            byte[] label
        )
    {
        var tuple = Native.ProveSingle(
                bp_gen.ptr,
                pc_gen.ptr,
                v,
                blinding.ptr,
                n,
                label,
                label.Length
                );
        // return (new RangeProof(IntPtr.Zero), new Ristretto.Point(tuple.point));
        return new RangeProof(tuple.proof);
    }


    public void Dispose()
    {
        Native.Dispose(ptr);
    }


}

[StructLayout(LayoutKind.Sequential)]
struct RangeProofWithCommit
{
    public IntPtr proof;
    public IntPtr point;
}

public record BulletProofGen : IDisposable
{
    internal readonly IntPtr ptr;

    [DllImport("rust_ffi", EntryPoint = "bpgen_new")]
    private static extern IntPtr New(uint gensCapacity, uint partyCapacity);

    public BulletProofGen(uint gensCapacity, uint partyCapacity)
    {
        this.ptr = New(gensCapacity, partyCapacity);
    }

    [DllImport("rust_ffi", EntryPoint = "bpgen_dispose")]
    private static extern void Dispose(IntPtr self);

    public void Dispose()
    {
        Dispose(ptr);
    }
}

using System.Runtime.InteropServices;
namespace ProjectOrigin.PedersenCommitment;

public record RangeProof {
    internal class Native
    {
        [DllImport("rust_ffi", EntryPoint = "rangeproof_prove_single")]
        internal static extern RangeProofWithCommit ProveSingle(BulletProofGen.Handle bp_gen, IntPtr pc_gen, ulong v, IntPtr blinding, uint n, byte[] label, int label_len);

        [DllImport("rust_ffi", EntryPoint = "rangeproof_prove_multiple")]
        internal static extern RangeProofWithCommit ProveMultiple(BulletProofGen.Handle bp_gen, IntPtr pc_gen, ulong[] v, IntPtr blinding, uint n, byte[] label, int label_len, int amount);

        [DllImport("rust_ffi", EntryPoint = "rangeproof_free")]
        internal static extern void Free(IntPtr self);

        // [DllImport("rust_ffi", EntryPoint = "rangeproof_verify")]
        // internal static extern void Verify(IntPtr self);
    }

    private readonly IntPtr ptr;

    private RangeProof(IntPtr ptr)
    {
        this.ptr = ptr;
    }

    ~RangeProof()
    {
        Native.Free(ptr);
    }

    public static RangeProof ProveSingle
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
}

[StructLayout(LayoutKind.Sequential)]
struct RangeProofWithCommit
{
    public IntPtr proof;
    public IntPtr point;
}

public record BulletProofGen
{
    internal readonly Handle ptr;

    [DllImport("rust_ffi", EntryPoint = "bpgen_new")]
    private static extern Handle New(uint gensCapacity, uint partyCapacity);


    public BulletProofGen(uint gensCapacity, uint partyCapacity)
    {
        this.ptr = New(gensCapacity, partyCapacity);
    }

    internal class Handle : SafeHandle
    {
        [DllImport("rust_ffi", EntryPoint = "bpgen_free")]
        private static extern void Free(Handle self);

        public Handle() : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid
        {
            get { return this.handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            if (!this.IsInvalid)
            {
                Free(this);
            }
            return true;
        }

    }
}


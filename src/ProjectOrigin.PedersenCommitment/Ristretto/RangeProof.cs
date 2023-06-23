using System;
using System.Runtime.InteropServices;

namespace ProjectOrigin.PedersenCommitment.Ristretto;

public record RangeProof
{
    private class Native
    {
        [DllImport("rust_ffi", EntryPoint = "rangeproof_prove_single")]
        internal static extern RangeProofWithCommit ProveSingle(IntPtr bp_gen, IntPtr pc_gen, ulong v, IntPtr blinding, uint n, byte[] label, int label_len);

        [DllImport("rust_ffi", EntryPoint = "rangeproof_prove_multiple")]
        internal static extern RangeProofWithCommit ProveMultiple(IntPtr bp_gen, IntPtr pc_gen, ulong[] v, IntPtr blinding, uint n, byte[] label, int label_len, int amount);

        [DllImport("rust_ffi", EntryPoint = "rangeproof_verify_single")]
        internal static extern bool VerifySingle(IntPtr self, IntPtr bp_gen, IntPtr pc_gen, IntPtr commit, uint n, byte[] label, int label_len);

        [DllImport("rust_ffi", EntryPoint = "rangeproof_verify_multiple")]
        internal static extern bool VerifyMultiple(IntPtr self, IntPtr bp_gen, IntPtr pc_gen, IntPtr commits, uint n, byte[] label, int label_len);

        [DllImport("rust_ffi", EntryPoint = "rangeproof_free")]
        internal static extern void Free(IntPtr self);

        [DllImport("rust_ffi", EntryPoint = "rangeproof_to_bytes")]
        internal static extern Extensions.RawVec ToBytes(IntPtr self);

        [DllImport("rust_ffi", EntryPoint = "rangeproof_from_bytes")]
        internal static extern IntPtr FromBytes(byte[] bytes, uint len);
    }

    private readonly IntPtr _ptr;

    private RangeProof(IntPtr ptr)
    {
        _ptr = ptr;
    }

    ~RangeProof()
    {
        Native.Free(_ptr);
    }
    /// <summary>
    /// Create a rangeproof for a given pair of value 'v' and blinding scalar 'blinding'.
    /// </summary>
    /// <param name="bp_gen">The BulletProofGen used</param>
    /// <param name="pc_gen">The Pedersen Generator used</param>
    /// <param name="v">The value for the proof</param>
    /// <param name="blinding">The blinding for the commitment</param>
    /// <param name="n">The bitsize to proof, n = 8, 16, 32 and 64</param>
    /// <param name="label"> Label for seperating the domain</param>
    /// <returns>RangeProof and a commitment Point</returns>
    public static (RangeProof proof, CompressedPoint commitment) ProveSingle
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
                bp_gen._ptr,
                pc_gen._ptr,
                v,
                blinding._ptr,
                n,
                label,
                label.Length
                );

        var bytes = new byte[CompressedPoint.ByteSize];
        CompressedPoint.ToBytes(tuple.CompressedPoint, bytes);
        return (new RangeProof(tuple.Proof), new CompressedPoint(bytes));
    }

    /// <summary>
    /// Verify the proof
    /// </summary>
    /// <param name="bp_gen">BulletProofGen used</param>
    /// <param name="pc_gen">Pedersen Generator used</param>
    /// <param name="commitment">Commitment from the proving step</param>
    /// <param name="n">bitsize for the proof, n = 8, 16, 32, 64</param>
    /// <param name="label">label for seperating the domain</param>
    /// <returns>true if the proof is valid</returns>
    public bool VerifySingle
        (
            BulletProofGen bp_gen,
            Generator pc_gen,
            CompressedPoint commitment, // Should be a CompressedPoint
            uint n,
            byte[] label
        )
    {
        var commit_ptr = CompressedPoint.FromBytes(commitment._bytes);
        var res = Native.VerifySingle(
                _ptr,
                bp_gen._ptr,
                pc_gen._ptr,
                commit_ptr,
                n,
                label,
                label.Length
                );
        return res;
    }

    /// <summary>
    /// Serializes the proof into a byte array
    /// </summary>
    /// <returns>a byte array of size 2 lg n + 9 for n secret bits</returns>
    public byte[] ToBytes()
    {
        var raw = Native.ToBytes(_ptr);
        var bytes = new byte[raw.size];
        Marshal.Copy(raw.data, bytes, 0, (int)raw.size);
        Extensions.FreeVec(raw);
        return bytes;
    }

    /// <summary>
    /// Deserializes proof from a byte array
    /// </summary>
    /// <param name="bytes">bytes to unpack from</param>
    /// <exception cref="FormatException">If the proof could not be deserialized</exception>
    /// <returns>a proof if the byte array was welformed</returns>
    public static RangeProof FromBytes(byte[] bytes)
    {
        var ptr = Native.FromBytes(bytes, (uint)bytes.Length);
        if (ptr == IntPtr.Zero)
        {
            throw new FormatException("Could not deserialize RangeProof");
        }
        return new RangeProof(ptr);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct RangeProofWithCommit
{
    public IntPtr Proof;
    public IntPtr CompressedPoint;
}

public record BulletProofGen
{
    public static Lazy<BulletProofGen> LazyGenerator = new Lazy<BulletProofGen>(() =>
    {
        return new BulletProofGen(32, 1);
    }, true);

    public static BulletProofGen Default
    {
        get => LazyGenerator.Value;
    }

    internal readonly IntPtr _ptr;

    [DllImport("rust_ffi", EntryPoint = "bpgen_new")]
    private static extern IntPtr New(uint gensCapacity, uint partyCapacity);

    [DllImport("rust_ffi", EntryPoint = "bpgen_free")]
    private static extern void Free(IntPtr self);

    /// <summary>
    /// Create a new BulletProofGen
    /// </summary>
    /// <param name="gensCapacity">
    /// The number of generators to precompute for each party.
    /// For range proofs it is sufficient to pass 64, the maximum bitsize of the rangeproofs.
    /// </param>
    /// <param name="partyCapacity">The maximum number of parties that can produce an aggregated proof.</param>
    /// <returns>a new BulletProofGen</returns>
    public BulletProofGen(uint gensCapacity, uint partyCapacity)
    {
        _ptr = New(gensCapacity, partyCapacity);
    }

    ~BulletProofGen()
    {
        Free(_ptr);
    }
}

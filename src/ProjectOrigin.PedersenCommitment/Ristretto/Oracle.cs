namespace ProjectOrigin.PedersenCommitment.Ristretto;
using System.Text;
using System.Runtime.InteropServices;
using System;

public class Oracle
{

    private class NativeTranscript
    {

        [DllImport("rust_ffi", EntryPoint = "transcript_new")]
        internal static extern IntPtr New(byte[] label, int len);

        [DllImport("rust_ffi", EntryPoint = "transcript_append_point")]
        internal static extern void AppendPoint(IntPtr self, byte[] label, int len, IntPtr point);

        [DllImport("rust_ffi", EntryPoint = "transcript_challenge_scalar")]
        internal static extern IntPtr ChallengeScalar(IntPtr self, byte[] label, int len);
    }

    private readonly IntPtr _ptr;

    public Oracle(byte[] label)
    {
        _ptr = NativeTranscript.New(label, label.Length);

    }

    public Oracle(String label)
    {
        var bytes = Encoding.UTF8.GetBytes(label);
        _ptr = NativeTranscript.New(bytes, bytes.Length);
    }

    public void Add(String label, params Point[] points)
    {
        var bytes = Encoding.UTF8.GetBytes(label);
        foreach (Point p in points)
        {
            NativeTranscript.AppendPoint(this._ptr, bytes, bytes.Length, p._ptr);
        }

    }

    public Scalar Challenge(String label)
    {
        var bytes = Encoding.UTF8.GetBytes(label);
        var scalar_ptr = NativeTranscript.ChallengeScalar(this._ptr, bytes, bytes.Length);
        return new Scalar(scalar_ptr);
    }
}

namespace ProjectOrigin.PedersenCommitment.Ristretto;

class Oracle
{

    private List<byte[]> messages = new List<byte[]>();

    public Oracle(byte[] label)
    {
        messages.Add(label);
    }


    public void Add(params Point[] points)
    {

        foreach (Point p in points)
        {
            messages.Add(p.Compress()._bytes);
        }
    }

    public Scalar Hash()
    {
        var m = 0;
        foreach (var msg in messages)
        {
            m += msg.Length;
        }

        var digest = new byte[m];
        var begin = 0;
        foreach (var msg in messages)
        {

            System.Array.Copy(msg, 0, digest, begin, msg.Length);
            begin += msg.Length;
        }
        return Scalar.HashFromBytes(digest);
    }
}

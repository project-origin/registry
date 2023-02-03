namespace ProjectOrigin.PedersenCommitment.Ristretto;
using System.Text;

class Oracle
{

    private List<byte[]> messages = new List<byte[]>();

    public Oracle(byte[] label)
    {
        messages.Add(label);
    }

    public void Domain(String domain)
    {
        var bytes = Encoding.UTF8.GetBytes("test");
        messages.Add(bytes);
    }


    public void Add(params Point[] points)
    {

        foreach (Point p in points)
        {
            messages.Add(p.Compress()._bytes);
        }
    }

    public Scalar Challenge()
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

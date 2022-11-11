using Google.Protobuf;

namespace ProjectOrigin.VerifiableEventStore;

public static class Serializer
{
    public static byte[] SerializeProto<T>(T proto) where T : IMessage
    {
        using (var stream = new MemoryStream())
        {
            using (var codedStream = new CodedOutputStream(stream))
            {
                proto.WriteTo(codedStream);
            }
            return stream.ToArray();
        }
    }
}

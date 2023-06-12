using Google.Protobuf;

namespace ProjectOrigin.Verifier.Utils.Interfaces;

public interface IProtoDeserializer
{
    IMessage Deserialize(string type, ByteString content);
}

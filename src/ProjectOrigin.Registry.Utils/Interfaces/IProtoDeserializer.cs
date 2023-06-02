using Google.Protobuf;

namespace ProjectOrigin.Registry.Utils.Interfaces;

public interface IProtoDeserializer
{
    IMessage Deserialize(string type, ByteString content);
}

using Google.Protobuf;

namespace ProjectOrigin.Electricity.Server.Interfaces;

public interface IProtoDeserializer
{
    IMessage Deserialize(string type, ByteString content);
}

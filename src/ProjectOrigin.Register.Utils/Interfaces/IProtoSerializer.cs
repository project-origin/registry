using Google.Protobuf;

namespace ProjectOrigin.Register.Utils.Interfaces;

public interface IProtoSerializer
{
    IMessage Deserialize(string type, ByteString content);
}

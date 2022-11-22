using Google.Protobuf;

namespace ProjectOrigin.Register.CommandProcessor.Models;


public record Command<T>(byte[] Id, T Content) : Command(Id, Content) where T : IMessage
{
    public new T Content
    {
        get
        {
            return (T)base.Content;
        }
    }
}

public record Command(byte[] Id, IMessage Content);

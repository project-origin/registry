using Google.Protobuf;

namespace ProjectOrigin.Register.LineProcessor.Interfaces;

public interface IModelProjector
{
    IModel Project(IEnumerable<IMessage> events);
}

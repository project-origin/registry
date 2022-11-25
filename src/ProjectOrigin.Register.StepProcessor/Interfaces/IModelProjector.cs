using Google.Protobuf;

namespace ProjectOrigin.Register.StepProcessor.Interfaces;

public interface IModelProjector
{
    IModel Project(IEnumerable<IMessage> events);
}

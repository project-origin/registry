using ProjectOrigin.Register.CommandProcessor.Models;

namespace ProjectOrigin.Register.CommandProcessor.Interfaces;

public interface ICommandDispatcher
{
    Task<V1.CommandStatus> Dispatch(Command request);
}

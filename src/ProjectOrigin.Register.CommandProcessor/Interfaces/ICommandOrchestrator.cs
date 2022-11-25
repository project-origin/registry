using Google.Protobuf;
using ProjectOrigin.Register.CommandProcessor.Models;

namespace ProjectOrigin.Register.CommandProcessor.Interfaces;

public interface ICommandOrchestrator<T> where T : IMessage
{
    Task<Register.V1.CommandStatus> Process(Command<T> command);
}

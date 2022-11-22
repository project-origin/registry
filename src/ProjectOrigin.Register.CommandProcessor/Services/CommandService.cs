using Grpc.Core;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Register.CommandProcessor.Interfaces;
using ProjectOrigin.Register.CommandProcessor.Models;
using ProjectOrigin.Register.Utils.Interfaces;

namespace ProjectOrigin.Register.CommandProcessor.Services;

public class CommandService : V1.CommandService.CommandServiceBase
{
    private readonly ILogger<CommandService> _logger;
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IProtoSerializer _serializer;

    public CommandService(ILogger<CommandService> logger, ICommandDispatcher commandDispatcher, IProtoSerializer serializer)
    {
        _logger = logger;
        _commandDispatcher = commandDispatcher;
        _serializer = serializer;
    }

    public override async Task<V1.CommandStatus> SubmitCommand(V1.Command protoCommand, ServerCallContext context)
    {
        try
        {
            var obj = _serializer.Deserialize(protoCommand.Type, protoCommand.Payload);
            var commandType = typeof(Command<>).MakeGenericType(obj.GetType());
            var command = (Command)Activator.CreateInstance(commandType, protoCommand.Id.ToByteArray(), obj)!;

            return await _commandDispatcher.Dispatch(command);
        }
        catch (Exception ex)
        {
            var message = $"Unhandled internal exception while processing command with ID:”{protoCommand.Id.ToBase64()}”";

            _logger.LogError(ex, message);

            return new V1.CommandStatus()
            {
                Id = protoCommand.Id,
                State = V1.CommandState.Failed,
                Error = message
            };
        }
    }
}

using Grpc.Core;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Register.StepProcessor.Interfaces;

namespace ProjectOrigin.Register.CommandProcessor.Services;

public class CommandService : V1.CommandService.CommandServiceBase
{
    private readonly ILogger<CommandService> _logger;
    private readonly ICommandStepProcessor _processor;

    public CommandService(ILogger<CommandService> logger, ICommandStepProcessor processor)
    {
        _processor = processor;
        _logger = logger;
    }

    public override async Task<V1.CommandStatus> SubmitCommand(V1.Command protoCommand, ServerCallContext context)
    {
        var result = new V1.CommandStatus()
        {
            Id = protoCommand.Id,
            State = V1.CommandState.Succeeded,
        };


        try
        {
            _logger.LogInformation($"Started command ”{protoCommand.Id}”");

            foreach (var step in protoCommand.Steps)
            {
                var stepResult = await _processor.Process(step);
                result.Steps.Add(stepResult);

                if (stepResult.State == V1.CommandState.Failed)
                {
                    result.State = V1.CommandState.Failed;
                    _logger.LogInformation($"Failed step ”{protoCommand.Id}”");
                    break;
                }
                _logger.LogInformation($"Completed step ”{protoCommand.Id}”");
            }

            _logger.LogInformation($"Completed command ”{protoCommand.Id}”");
        }
        catch (Exception ex)
        {
            var message = $"Unhandled internal exception while processing command with ID:”{protoCommand.Id.ToBase64()}”";
            result.State = V1.CommandState.Failed;
            _logger.LogError(ex, message);
        }

        return result;
    }
}

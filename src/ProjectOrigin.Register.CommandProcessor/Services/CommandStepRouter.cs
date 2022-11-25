using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Register.CommandProcessor.Services;

public class CommandStepRouter : ICommandStepProcessor
{
    private Dictionary<string, ICommandStepProcessor> _routes;

    public CommandStepRouter(Dictionary<string, ICommandStepProcessor> routes)
    {
        _routes = routes;
    }

    public Task<CommandStepResult> Process(CommandStep request)
    {
        var targetRegistry = request.FederatedStreamId.Registry;

        if (!_routes.TryGetValue(targetRegistry, out var processor))
            return Task.FromResult(new CommandStepResult(request.CommandStepId, CommandStepState.Failed, $"Registry ”{targetRegistry}” unknown"));

        return processor.Process(request);
    }
}

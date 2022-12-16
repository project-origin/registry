using ProjectOrigin.Register.StepProcessor.Interfaces;

namespace ProjectOrigin.Register.CommandProcessor.Services;

public class CommandStepRouter : ICommandStepProcessor
{
    private Dictionary<string, ICommandStepProcessor> _routes;

    public CommandStepRouter(Dictionary<string, ICommandStepProcessor> routes)
    {
        _routes = routes;
    }

    public Task<V1.CommandStepStatus> Process(V1.CommandStep request)
    {
        var targetRegistry = request.RoutingId.Registry;

        if (!_routes.TryGetValue(targetRegistry, out var processor))
        {
            return Task.FromResult(new V1.CommandStepStatus()
            {
                State = V1.CommandState.Failed,
                Error = $"Unable to route CommandStep to ”{targetRegistry}”, unknown registry."
            });
        }

        return processor.Process(request);
    }
}

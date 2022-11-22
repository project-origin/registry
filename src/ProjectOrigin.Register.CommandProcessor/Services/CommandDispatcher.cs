using Google.Protobuf;
using ProjectOrigin.Register.CommandProcessor.Interfaces;
using ProjectOrigin.Register.V1;

namespace ProjectOrigin.Register.CommandProcessor.Services;

public class CommandDispatcher : ICommandDispatcher
{
    private IEnumerable<object> _orchestrators;

    public CommandDispatcher(IEnumerable<object> orchestrators)
    {
        _orchestrators = orchestrators;
    }

    public Task<CommandStatus> Dispatch(Models.Command request)
    {
        var commandType = request.Content.GetType();

        var t = typeof(Models.Command<>);
        var cc = t.MakeGenericType(commandType);

        var orchestratorType = typeof(ICommandOrchestrator<>).MakeGenericType(commandType);
        var orchestrator = _orchestrators.SingleOrDefault(orchestrator => orchestratorType.IsAssignableFrom(orchestrator.GetType()));
        if (orchestrator is null)
            return Task.FromResult(new CommandStatus()
            {
                Id = ByteString.CopyFrom(request.Id),
                State = CommandState.Failed,
                Error = "Not supported command"
            });

        var methodInfo = orchestrator.GetType().GetMethods().Where(m =>
            m.Name.Equals(nameof(ICommandOrchestrator<IMessage>.Process))
            && m.GetParameters().SingleOrDefault(x => x.ParameterType == cc) is not null).SingleOrDefault();

        if (methodInfo is null)
            return Task.FromResult(new CommandStatus()
            {
                Id = ByteString.CopyFrom(request.Id),
                State = CommandState.Failed,
                Error = "Not supported command"
            });

        return (Task<Register.V1.CommandStatus>)methodInfo.Invoke(orchestrator, new object[] { request })!;
    }
}

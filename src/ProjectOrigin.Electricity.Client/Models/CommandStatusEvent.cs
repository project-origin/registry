namespace ProjectOrigin.Electricity.Client.Models;

/// <summary>
/// A CommandStateEvent is an event send by the client
/// to let callers know updated state of an command sent to the registry.
/// </summary>
public class CommandStatusEvent
{
    /// <summary>
    /// Contains the ID/CommandReference to the command the update relates to.
    /// </summary>
    public CommandId Id { get; }

    /// <summary>
    /// Contains the actual state the command is in, on the registry.
    /// </summary>
    public CommandState State { get; }

    /// <summary>
    /// If the CommandState is Failed, then this value will contain the message associated with the failure.
    /// </summary>
    public string? Error { get; }

    internal CommandStatusEvent(CommandId id, CommandState state, string? error)
    {
        Id = id;
        State = state;
        Error = error;
    }
}

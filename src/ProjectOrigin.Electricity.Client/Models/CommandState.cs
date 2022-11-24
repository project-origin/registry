namespace ProjectOrigin.Electricity.Client.Models;

public enum CommandState
{
    Failed = 0,
    Succeeded = 1,
}

public struct CommandStatusEvent
{
    public TransactionId Id { get; init; }
    public CommandState State { get; init; }
    public string? Error { get; init; }

    public CommandStatusEvent(TransactionId id, CommandState state, string? error)
    {
        Id = id;
        State = state;
        Error = error;
    }
}

namespace ProjectOrigin.Electricity.Client.Models;

public enum CommandState
{
    Failed = 0,
    Succeeded = 1,
}

public record CommandStatusEvent(TransactionId id, CommandState state, string? Error);

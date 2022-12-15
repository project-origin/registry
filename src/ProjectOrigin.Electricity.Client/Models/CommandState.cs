namespace ProjectOrigin.Electricity.Client.Models;

/// <summary>
/// A CommandState is an enum that describes the state of the command on the registry.
/// </summary>
public enum CommandState
{
    /// <summary>
    /// Denotes that the command failed to be executed on the registry.
    /// </summary>
    Failed = 0,

    /// <summary>
    /// Denotes that the command succeeded.
    /// </summary>
    Succeeded = 1,
}

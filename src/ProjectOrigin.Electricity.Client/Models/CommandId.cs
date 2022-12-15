namespace ProjectOrigin.Electricity.Client.Models;

/// <summary>
/// Object containing a Id of the command one has sent to the registry.
/// This reference is created based on a SHA256 of the serialized content of the command.
/// </summary>
public class CommandId
{
    /// <summary>
    /// The raw SHA256 hash value of the command.
    /// </summary>
    public byte[] Hash { get; }

    /// <summary>
    /// Creates an instance of a CommandId based on the hash.
    /// </summary>
    /// <param name="hash">contains the message that one wants to hide.</param>
    public CommandId(byte[] hash)
    {
        Hash = hash;
    }
}

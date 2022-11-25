using System.Numerics;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Client.Models;

/// <summary>
/// A ShieldedValue contains a Message and a random R
/// which hides the message with the help of a Pedersen Commitment.
/// </summary>
public class ShieldedValue
{
    /// <summary>
    /// Contains the message that the shielded value should protect.
    /// </summary>
    public ulong Message { get; }

    /// <summary>
    /// The random value that protects the message.
    /// </summary>
    public BigInteger R { get; }

    /// <summary>
    /// Creates a ShieldedValue based on a message and r value.
    /// Validates that the value r is in  the Group.
    /// </summary>
    /// <param name="message">contains the message that one wants to hide.</param>
    /// <param name="r">contains the random value that hides the message.</param>
    public ShieldedValue(ulong message, BigInteger r)
    {
        var cm = new CommitmentParameters(message, r, Group.Default);
        Message = message;
        R = cm.r;
    }

    /// <summary>
    /// Creates a ShieldedValue based on a message,
    /// automatically creates a random value to hide the message.
    /// </summary>
    /// <param name="message">contains the message that one wants to hide.</param>
    public ShieldedValue(ulong message)
    {
        Message = message;
        R = Group.Default.Commit(message).r;
    }
}

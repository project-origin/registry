using System.Numerics;

namespace ProjectOrigin.Electricity.Client.Models;

/// <summary>
/// ShieldedValue record type
/// </summary>
/// <param name="Message">contains the message that one wants to hide.</param>
/// <param name="R">contains the random value that hides the message.</param>
/// <remarks>
/// A ShieldedValue is a record that contains a Message and
/// a random R which hides the message with the help of a Pedersen Commitment.
/// </remarks>
public record ShieldedValue(ulong Message, BigInteger R);

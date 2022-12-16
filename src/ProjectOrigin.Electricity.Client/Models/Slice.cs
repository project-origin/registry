using NSec.Cryptography;

namespace ProjectOrigin.Electricity.Client.Models;

/// <summary>
/// A <a href="xref:granular_certificate#slices">slice</a> represents an part of a
/// <a href="xref:granular_certificate">Granular Certificate</a>
/// </summary>
public class Slice
{
    /// <summary>
    /// The shielded Quantity of the slice.
    /// </summary>
    public ShieldedValue Quantity { get; init; }

    /// <summary>
    /// The publicKey which will be set as owner of the slice.
    /// </summary>
    public PublicKey Owner { get; init; }

    internal Slice(ShieldedValue quantity, PublicKey owner)
    {
        Quantity = quantity;
        Owner = owner;
    }
}

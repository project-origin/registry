namespace ProjectOrigin.Electricity.Client.Models;

/// <summary>
/// A SliceCollection represents the new slices created from an existing slice,
/// it might contain a remainder, if the entire source slice was not used.
/// </summary>
public class SliceCollection
{
    /// <summary>
    /// The source slice which will be sliced into new slices.
    /// </summary>
    public ShieldedValue Source { get; init; }

    /// <summary>
    /// A collection of new <a href="xref:granular_certificate#slice">slices</a> to create
    /// from the source slice
    /// </summary>
    public IEnumerable<Slice> Slices { get; init; }

    /// <summary>
    /// If the sum of slices is smaller than the source, then a remainder will be created,
    /// which will be set to the original owner.
    /// </summary>
    public ShieldedValue? Remainder { get; init; }

    internal SliceCollection(ShieldedValue source, IEnumerable<Slice> slices, ShieldedValue? remainder = null)
    {
        Source = source;
        Slices = slices;
        Remainder = remainder;
    }
}

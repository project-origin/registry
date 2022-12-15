using NSec.Cryptography;
using ProjectOrigin.Electricity.Client.Models;

namespace ProjectOrigin.Electricity.Client;

/// <summary>
/// To help create <a href="xref:granular_certificate#slices">Granular Certificate slices</a> from the current slice.
/// </summary>
public class Slicer
{
    private ShieldedValue _source;
    private List<Slice> _slices = new List<Slice>();

    /// <summary>
    /// Creates a slicer based on an existing <a href="xref:granular_certificate#slices">Granular Certificate slice</a>.
    /// </summary>
    /// <param name="source">a shieldedValue of the source slice on the certificate from which to create the new slices.</param>
    public Slicer(ShieldedValue source)
    {
        _source = source;
    }

    /// <summary>
    /// Create a slice of the following size and to the specified owner.
    /// </summary>
    /// <param name="quantity">The ShieldedValue of the new slice.</param>
    /// <param name="newOwner">The Ed25519 publicKey which should be set as the owner of the slice.</param>
    public Slicer CreateSlice(ShieldedValue quantity, PublicKey newOwner)
    {
        _slices.Add(new Slice(quantity, newOwner));
        if (_slices.Select(slice => slice.Quantity.Message).Aggregate((a, b) => a + b) > _source.Message)
            throw new NotSupportedException();

        return this;
    }

    /// <summary>
    /// Collects all the slices created in a SliceCollection, which might contain a remainder, if the sum of slices is smaller than the source.
    /// </summary>
    public SliceCollection Collect()
    {
        ShieldedValue? remainder = null;
        var slicesSum = _slices.Select(slice => slice.Quantity.Message).Aggregate((a, b) => a + b);

        if (slicesSum < _source.Message)
            remainder = new ShieldedValue(_source.Message - slicesSum);

        return new SliceCollection(_source, _slices, remainder);
    }
}

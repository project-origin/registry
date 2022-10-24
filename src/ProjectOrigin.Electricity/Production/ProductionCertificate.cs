using ProjectOrigin.Electricity.Production.Requests;
using ProjectOrigin.Electricity.Shared;
using ProjectOrigin.Electricity.Shared.Internal;

namespace ProjectOrigin.Electricity.Production;

internal class ProductionCertificate
{
    public FederatedStreamId Id { get => issued!.Id; }

    public IEnumerable<CertificateSlices> Slices { get => slices; }

    private List<CertificateSlices> slices = new List<CertificateSlices>();

    private ProductionIssuedEvent issued;

    public void Apply(ProductionIssuedEvent issued)
    {
        this.issued = issued;
        slices.Add(new(issued.QuantityCommitment, issued.OwnerPublicKey));
    }

    public void Apply(ProductionSliceTransferredEvent e)
    {
        var oldSlice = slices.Single(slice => slice.Commitment == e.Slice.Source);
        slices.Remove(oldSlice);
        slices.Add(new(e.Slice.Quantity, e.NewOwner));
        slices.Add(new(e.Slice.Remainder, oldSlice.Owner));
    }
}

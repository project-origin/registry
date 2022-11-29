using NSec.Cryptography;
using ProjectOrigin.Electricity.Client.Models;

namespace ProjectOrigin.Electricity.Client;

public partial class ElectricityClient
{
    /// <summary>
    /// This is used to claim a slice from a <b>production certificate</b> to a <b>consumption certificate</b>.
    /// </summary>
    /// <param name="quantity">a shieldedValue containing the amount to claim.</param>
    /// <param name="consumptionId">the federated certicate id for the <b>consumption certificate</b> resides.</param>
    /// <param name="consumptionSource">a shieldedValue of the source slice on the <b>consumption certificate</b> from which to create the new slices.</param>
    /// <param name="consumptionRemainder">a shieldedValue of the remainder slice on the <b>consumption certificate</b>, a Zero slice should be provided if all is transfered.</param>
    /// <param name="consumptionSigner">the signing key for the owner of the <b>consumption certificate</b>.</param>
    /// <param name="productionId">the federated certicate id for the <b>production certificate</b>.</param>
    /// <param name="productionSource">a shieldedValue of the source slice on the <b>production certificate</b> from which to create the new slices.</param>
    /// <param name="productionRemainder">a shieldedValue of the remainder slice on the <b>production certificate</b>, a Zero slice should be provided if all is transfered.</param>
    /// <param name="productionSigner">the signing key for the current owner of the slice on the <b>production certificate</b>.</param>
    public Task<CommandId> ClaimCertificate(
        ShieldedValue quantity,
        FederatedCertifcateId consumptionId,
        ShieldedValue consumptionSource,
        ShieldedValue consumptionRemainder,
        Key consumptionSigner,
        FederatedCertifcateId productionId,
        ShieldedValue productionSource,
        ShieldedValue productionRemainder,
        Key productionSigner
    )
    {
        var allocationId = new Register.V1.Uuid()
        {
            Value = Guid.NewGuid().ToString()
        };
        var prodCertId = productionId.ToProto();
        var consCertId = consumptionId.ToProto();

        var productionAllocationEvent = new V1.ClaimCommand.Types.AllocatedEvent()
        {
            AllocationId = allocationId,
            ProductionCertificateId = prodCertId,
            ConsumptionCertificateId = consCertId,
            Slice = CreateSlice(productionSource, quantity, productionRemainder)
        };

        var consumptionAllocationEvent = new V1.ClaimCommand.Types.AllocatedEvent()
        {
            AllocationId = allocationId,
            ProductionCertificateId = prodCertId,
            ConsumptionCertificateId = consCertId,
            Slice = CreateSlice(consumptionSource, quantity, consumptionRemainder)
        };

        var productionClaimedEvent = new V1.ClaimCommand.Types.ClaimedEvent()
        {
            AllocationId = allocationId,
            CertificateId = prodCertId,
        };

        var consumptionClaimedEvent = new V1.ClaimCommand.Types.ClaimedEvent()
        {
            AllocationId = allocationId,
            CertificateId = consCertId,
        };

        var consumptionAllocatedProof = CreateSliceProof(consumptionSource, quantity, consumptionRemainder);
        var productionAllocatedProof = CreateSliceProof(productionSource, quantity, productionRemainder);

        var command = new V1.ClaimCommand()
        {
            ConsumptionAllocatedEvent = consumptionAllocationEvent,
            ConsumptionAllocatedSignature = Sign(consumptionSigner, consumptionAllocationEvent),
            ProductionAllocatedEvent = productionAllocationEvent,
            ProductionAllocatedSignature = Sign(productionSigner, productionAllocationEvent),
            ConsumptionClaimedEvent = consumptionClaimedEvent,
            ConsumptionClaimedSignature = Sign(consumptionSigner, consumptionClaimedEvent),
            ProductionClaimedEvent = productionClaimedEvent,
            ProductionClaimedSignature = Sign(productionSigner, productionClaimedEvent),
            ConsumptionAllocatedProof = consumptionAllocatedProof,
            ProductionAllocatedProof = productionAllocatedProof,
        };

        return SendCommand(command);
    }
}

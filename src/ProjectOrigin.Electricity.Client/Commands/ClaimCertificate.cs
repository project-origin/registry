using System.Numerics;
using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client.Models;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Register.V1;

namespace ProjectOrigin.Electricity.Client;


public partial class ElectricityClient
{

    /// <summary>
    /// This is used to claim a slice from a <b>production certificate</b> to a <b>consumption certificate</b>.
    /// </summary>
    /// <param name="quantity">a shieldedValue containing the amount to claim.</param>
    /// <param name="consumptionRegistry">the name or identifier of the registry where the <b>consumption certificate</b> resides.</param>
    /// <param name="consumptionCertificateId">the unique Uuid of the <b>consumption certificate</b>.</param>
    /// <param name="consumptionSource">a shieldedValue of the source slice on the <b>consumption certificate</b> from which to create the new slices.</param>
    /// <param name="consumptionRemainder">a shieldedValue of the remainder slice on the <b>consumption certificate</b>, a Zero slice should be provided if all is transfered.</param>
    /// <param name="consumptionSigner">the signing key for the owner of the <b>consumption certificate</b>.</param>

    /// <param name="productionRegistry">the name or identifier of the registry where the <b>production certificate</b> resides.</param>
    /// <param name="productionCertificateId">the unique Uuid of the <b>production certificate</b>.</param>
    /// <param name="productionSource">a shieldedValue of the source slice on the <b>production certificate</b> from which to create the new slices.</param>
    /// <param name="productionRemainder">a shieldedValue of the remainder slice on the <b>production certificate</b>, a Zero slice should be provided if all is transfered.</param>
    /// <param name="productionSigner">the signing key for the current owner of the slice on the <b>production certificate</b>.</param>
    public Task<TransactionId> ClaimCertificate(
        ShieldedValue quantity,
        string consumptionRegistry,
        Guid consumptionCertificateId,
        ShieldedValue consumptionSource,
        ShieldedValue consumptionRemainder,
        Key consumptionSigner,
        string productionRegistry,
        Guid productionCertificateId,
        ShieldedValue productionSource,
        ShieldedValue productionRemainder,
        Key productionSigner
    )
    {
        var allocationId = new Register.V1.Uuid()
        {
            Value = Guid.NewGuid().ToString()
        };
        var prodCertId = ToProtoId(productionRegistry, productionCertificateId);
        var consCertId = ToProtoId(consumptionRegistry, consumptionCertificateId);

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


    private static FederatedStreamId ToProtoId(string productionRegistry, Guid productionCertificateId) => new Register.V1.FederatedStreamId()
    {
        Registry = productionRegistry,
        StreamId = new Register.V1.Uuid()
        {
            Value = productionCertificateId.ToString()
        }
    };

    private V1.Commitment ToProtoCommitment(ShieldedValue sv)
    {
        var commitmentParameters = new CommitmentParameters(sv.message, sv.r, Group);
        return new V1.Commitment()
        {
            C = ByteString.CopyFrom(commitmentParameters.C.ToByteArray())
        };
    }

    private V1.CommitmentProof ToProtoCommitmentProof(ShieldedValue sv)
    {
        return new V1.CommitmentProof()
        {
            M = sv.message,
            R = ByteString.CopyFrom(sv.r.ToByteArray())
        };
    }

    private V1.Slice CreateSlice(ShieldedValue source, ShieldedValue quantity, ShieldedValue remainder)
    {
        return new V1.Slice()
        {
            Source = ToProtoCommitment(source),
            Quantity = ToProtoCommitment(quantity),
            Remainder = ToProtoCommitment(remainder),
            ZeroR = ByteString.CopyFrom(((source.r - (quantity.r + remainder.r)).MathMod(Group.q)).ToByteArray())
        };
    }

    private V1.SliceProof CreateSliceProof(ShieldedValue productionSource, ShieldedValue quantity, ShieldedValue productionRemainder)
    {
        return new V1.SliceProof()
        {
            Source = ToProtoCommitmentProof(productionSource),
            Quantity = ToProtoCommitmentProof(quantity),
            Remainder = ToProtoCommitmentProof(productionRemainder),
        };
    }
}

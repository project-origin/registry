using System.Numerics;
using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client.Models;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Register.V1;

namespace ProjectOrigin.Electricity.Client;


public partial class ElectricityClient
{
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

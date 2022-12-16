using System.Security.Cryptography;
using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client.Models;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Client;

/// <summary>
/// The ElectricityCommandbuilder helps the user to build a Registry command.
/// </summary>
public class ElectricityCommandBuilder
{

    /// <summary>
    /// Executes the commands build with the builder to the givene client.
    /// </summary>
    public Task<CommandId> Execute(RegisterClient client)
    {
        var command = new Register.V1.Command();
        command.Steps.AddRange(_steps);

        var commandHash = SHA256.HashData(command.ToByteArray());
        command.Id = ByteString.CopyFrom(commandHash);

        return client.Execute(command);
    }

    /// <summary>
    /// This is used to issue a Consumption <a href="xref:granular_certificate">Granular Certificate</a>
    /// </summary>
    /// <param name="id">the federated certicate id for the certificate.</param>
    /// <param name="inteval">the interval for the certificate, contains a start and end date.</param>
    /// <param name="gridArea">the gridArea/PriceArea of which the Meter is a part of.</param>
    /// <param name="gsrn">a shieldedValue of the GSRN of the Meter.</param>
    /// <param name="quantity">a shieldedValue of the quantity in Wh the meter has used in the period.</param>
    /// <param name="owner">the Ed25519 publicKey which should be set as the owner of the certificate.</param>
    /// <param name="issuingBodySigner">the signing key for the issuing body.</param>
    public ElectricityCommandBuilder IssueConsumptionCertificate(
        FederatedCertifcateId id,
        DateInterval inteval,
        string gridArea,
        ShieldedValue gsrn,
        ShieldedValue quantity,
        PublicKey owner,
        Key issuingBodySigner
    )
    {
        var federatedId = id.ToProto();
        var @event = new V1.ConsumptionIssuedEvent()
        {
            CertificateId = federatedId,
            Period = inteval.ToProto(),
            GridArea = gridArea,
            GsrnCommitment = gsrn.ToProtoCommitment(),
            QuantityCommitment = quantity.ToProtoCommitment(),
            OwnerPublicKey = new V1.PublicKey()
            {
                Content = ByteString.CopyFrom(owner.Export(KeyBlobFormat.RawPublicKey))
            },
        };

        SignCommandAndAddStep(issuingBodySigner, federatedId, @event);

        return this;
    }

    /// <summary>
    /// This is used to issue a Production <a href="xref:granular_certificate">Granular Certificate</a>
    /// </summary>
    /// <param name="id">the federated certicate id for the certificate.</param>
    /// <param name="inteval">the interval for the certificate, contains a start and end date.</param>
    /// <param name="gridArea">the gridArea/PriceArea of which the Meter is a part of.</param>
    /// <param name="fuelCode">the AIB standard fuelCode.</param>
    /// <param name="techCode">the AIB standard techCode.</param>
    /// <param name="gsrn">a shieldedValue of the GSRN of the Meter.</param>
    /// <param name="quantity">a shieldedValue of the quantity in Wh the meter has used in the period.</param>
    /// <param name="owner">the Ed25519 publicKey which should be set as the owner of the certificate.</param>
    /// <param name="issuingBodySigner">the signing key for the issuing body.</param>
    public ElectricityCommandBuilder IssueProductionCertificate(
        FederatedCertifcateId id,
        DateInterval inteval,
        string gridArea,
        string fuelCode,
        string techCode,
        ShieldedValue gsrn,
        ShieldedValue quantity,
        PublicKey owner,
        Key issuingBodySigner
    )
    {
        var federatedId = id.ToProto();
        var @event = new V1.ProductionIssuedEvent()
        {
            CertificateId = federatedId,
            Period = inteval.ToProto(),
            GridArea = gridArea,
            FuelCode = fuelCode,
            TechCode = techCode,
            GsrnCommitment = gsrn.ToProtoCommitment(),
            QuantityCommitment = quantity.ToProtoCommitment(),
            // QuantityPublication = new V1.CommitmentPublication()
            // {
            //     Message = quantity.Message,
            //     RValue = ByteString.CopyFrom(quantity.RValue.ToByteArray())
            // },
            OwnerPublicKey = owner.ToProto(),
        };

        SignCommandAndAddStep(issuingBodySigner, federatedId, @event);
        return this;
    }

    /// <summary>
    /// This is used to transfer a <a href="xref:granular_certificate#slices">Granular Certificate slice</a> from the current owner to a new owner.
    /// </summary>
    /// <param name="id">the federated certicate id for the certificate.</param>
    /// <param name="source">a shieldedValue of the source slice on the certificate from which to create the new slices.</param>
    /// <param name="currentOwnerSigner">the signing key for the current owner of the slice.</param>
    /// <param name="newOwner">the Ed25519 publicKey which should be set as the owner of the certificate.</param>
    public ElectricityCommandBuilder TransferCertificate(
        FederatedCertifcateId id,
        ShieldedValue source,
        Key currentOwnerSigner,
        PublicKey newOwner
    )
    {
        var federatedId = id.ToProto();
        var @event = new V1.TransferredEvent()
        {
            CertificateId = federatedId,
            SourceSlice = source.ToSliceId(),
            NewOwner = newOwner.ToProto(),
        };

        SignCommandAndAddStep(currentOwnerSigner, federatedId, @event);
        return this;
    }

    /// <summary>
    /// This is used to create new <a href="xref:granular_certificate#slices">slices</a> from an existing <a href="xref:granular_certificate#slices">slice</a>.
    /// </summary>
    /// <param name="id">The federated certicate id for the certificate.</param>
    /// <param name="sliceCollection">The collection of new slices created with the help of the Slicer.</param>
    /// <param name="currentOwnerSigner">The signing key for the current owner of the slice.</param>
    public ElectricityCommandBuilder SliceCertificate(
        FederatedCertifcateId id,
        SliceCollection sliceCollection,
        Key currentOwnerSigner
    )
    {
        var federatedId = id.ToProto();
        var @event = new V1.SlicedEvent()
        {
            CertificateId = federatedId,
            SourceSlice = sliceCollection.Source.ToSliceId(),
        };

        foreach (var slice in sliceCollection.Slices)
        {
            @event.NewSlices.Add(new V1.SlicedEvent.Types.Slice
            {
                Quantity = slice.Quantity.ToProtoCommitment(),
                NewOwner = slice.Owner.ToProto(),
            });
        }

        if (sliceCollection.Remainder is not null)
        {
            @event.NewSlices.Add(new V1.SlicedEvent.Types.Slice
            {
                Quantity = sliceCollection.Remainder.ToProtoCommitment(),
                NewOwner = currentOwnerSigner.PublicKey.ToProto(),
            });
        }

        SignCommandAndAddStep(currentOwnerSigner, federatedId, @event);
        return this;
    }

    /// <summary>
    /// This is used to claim a slice from a <b>production certificate</b> to a <b>consumption certificate</b>.
    /// </summary>
    /// <param name="quantity">a shieldedValue containing the amount to claim.</param>
    /// <param name="consumptionId">the federated certicate id for the <b>consumption certificate</b> resides.</param>
    /// <param name="consumptionSource">a shieldedValue of the source slice on the <b>consumption certificate</b> from which to create the new slices.</param>
    /// <param name="consumptionSigner">the signing key for the owner of the <b>consumption certificate</b>.</param>
    /// <param name="productionId">the federated certicate id for the <b>production certificate</b>.</param>
    /// <param name="productionSource">a shieldedValue of the source slice on the <b>production certificate</b> from which to create the new slices.</param>
    /// <param name="productionSigner">the signing key for the current owner of the slice on the <b>production certificate</b>.</param>
    public ElectricityCommandBuilder ClaimCertificate(
        ShieldedValue quantity,
        FederatedCertifcateId consumptionId,
        ShieldedValue consumptionSource,
        Key consumptionSigner,
        FederatedCertifcateId productionId,
        ShieldedValue productionSource,
        Key productionSigner
    )
    {
        var allocationId = new Register.V1.Uuid()
        {
            Value = Guid.NewGuid().ToString()
        };
        var prodCertId = productionId.ToProto();
        var consCertId = consumptionId.ToProto();

        var allocatedEvent = new V1.AllocatedEvent
        {
            AllocationId = allocationId,
            ProductionCertificateId = prodCertId,
            ConsumptionCertificateId = consCertId,
            ProductionSourceSlice = productionSource.ToSliceId(),
            ConsumptionSourceSlice = consumptionSource.ToSliceId(),
            EqualityProof = ByteString.CopyFrom(Group.Default.CreateEqualityProof(productionSource.ToParams(), consumptionSource.ToParams()))
        };

        var productionClaimedEvent = new V1.ClaimedEvent
        {
            AllocationId = allocationId,
            CertificateId = prodCertId,
        };

        var consumptionClaimedEvent = new V1.ClaimedEvent
        {
            AllocationId = allocationId,
            CertificateId = consCertId,
        };

        SignCommandAndAddStep(productionSigner, prodCertId, allocatedEvent, consCertId);
        SignCommandAndAddStep(consumptionSigner, consCertId, allocatedEvent, prodCertId);
        SignCommandAndAddStep(productionSigner, prodCertId, productionClaimedEvent, consCertId);
        SignCommandAndAddStep(consumptionSigner, consCertId, consumptionClaimedEvent, prodCertId);

        return this;
    }

    private List<Register.V1.CommandStep> _steps = new List<Register.V1.CommandStep>();

    internal void SignCommandAndAddStep(Key issuingBodySigner, Register.V1.FederatedStreamId federatedId, IMessage @event, params Register.V1.FederatedStreamId[] other)
    {
        var a = new Register.V1.CommandStep()
        {
            RoutingId = federatedId,
            SignedEvent = new Register.V1.SignedEvent()
            {
                Type = @event.Descriptor.FullName,
                Payload = @event.ToByteString(),
                Signature = RegisterClient.Sign(issuingBodySigner, @event)
            }
        };
        a.OtherStreams.AddRange(other);
        _steps.Add(a);
    }
}

using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client.Models;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Client;

public partial class ElectricityClient
{
    /// <summary>
    /// This is used to issue a Consumption GC
    /// </summary>
    /// <param name="registry">the name or identifier of the registry to issue the certificate to.</param>
    /// <param name="certificateId">the unique Uuid of the certificate.</param>
    /// <param name="dateFrom">DateTimeOffset from when the certificate begins.</param>
    /// <param name="dateTo">DateTimeOffset from when the certificate ends, must be larger that dateFrom.</param>
    /// <param name="gridArea">the gridArea/PriceArea of which the Meter is a part of.</param>
    /// <param name="gsrn">a shieldedValue of the GSRN of the Meter.</param>
    /// <param name="quantity">a shieldedValue of the quantity in Wh the meter has used in the period.</param>
    /// <param name="owner">the Ed25519 publicKey which should be set as the owner of the certificate.</param>
    /// <param name="issuingBodySigner">the signing key for the issuing body.</param>
    public Task<TransactionId> IssueConsumptionCertificate(
        string registry,
        Guid certificateId,
        DateTimeOffset dateFrom,
        DateTimeOffset dateTo,
        string gridArea,
        ShieldedValue gsrn,
        ShieldedValue quantity,
        PublicKey owner,
        Key issuingBodySigner
    )
    {
        var @event = new V1.IssueConsumptionCommand.Types.ConsumptionIssuedEvent()
        {
            CertificateId = ToProtoId(registry, certificateId),
            Period = new V1.TimePeriod()
            {
                DateTimeFrom = Timestamp.FromDateTimeOffset(dateFrom),
                DateTimeTo = Timestamp.FromDateTimeOffset(dateTo),
            },
            GridArea = gridArea,
            GsrnCommitment = gsrn.ToProtoCommitment(),
            QuantityCommitment = quantity.ToProtoCommitment(),
            OwnerPublicKey = new V1.PublicKey()
            {
                Content = ByteString.CopyFrom(owner.Export(KeyBlobFormat.RawPublicKey))
            },
        };

        var proof = new V1.IssueConsumptionCommand.Types.ConsumptionIssuedProof()
        {
            GsrnProof = gsrn.ToProtoCommitmentProof(),
            QuantityProof = quantity.ToProtoCommitmentProof()
        };

        var signature = Sign(issuingBodySigner, @event);

        var commandContent = new V1.IssueConsumptionCommand()
        {
            Event = @event,
            Signature = signature,
            Proof = proof,
        };

        return SendCommand(commandContent);
    }
}

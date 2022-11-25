using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client.Models;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Client;

public partial class ElectricityClient
{
    /// <summary>
    /// This is used to issue a Production GC
    /// </summary>
    /// <param name="registry">the name or identifier of the registry to issue the certificate to.</param>
    /// <param name="certificateId">the unique Uuid of the certificate.</param>
    /// <param name="dateFrom">DateTimeOffset from when the certificate begins.</param>
    /// <param name="dateTo">DateTimeOffset from when the certificate ends, must be larger that dateFrom.</param>
    /// <param name="gridArea">the gridArea/PriceArea of which the Meter is a part of.</param>
    /// <param name="fuelCode">the AIB standard fuelCode.</param>
    /// <param name="techCode">the AIB standard techCode.</param>
    /// <param name="gsrn">a shieldedValue of the GSRN of the Meter.</param>
    /// <param name="quantity">a shieldedValue of the quantity in Wh the meter has used in the period.</param>
    /// <param name="owner">the Ed25519 publicKey which should be set as the owner of the certificate.</param>
    /// <param name="issuingBodySigner">the signing key for the issuing body.</param>
    public Task<TransactionId> IssueProductionCertificate(
        string registry,
        Guid certificateId,
        DateTimeOffset dateFrom,
        DateTimeOffset dateTo,
        string gridArea,
        string fuelCode,
        string techCode,
        ShieldedValue gsrn,
        ShieldedValue quantity,
        PublicKey owner,
        Key issuingBodySigner
    )
    {
        var @event = new V1.IssueProductionCommand.Types.ProductionIssuedEvent()
        {
            CertificateId = new Register.V1.FederatedStreamId()
            {
                Registry = registry,
                StreamId = new Register.V1.Uuid()
                {
                    Value = certificateId.ToString()
                }
            },
            Period = new V1.TimePeriod()
            {
                DateTimeFrom = Timestamp.FromDateTimeOffset(dateFrom),
                DateTimeTo = Timestamp.FromDateTimeOffset(dateTo),
            },
            GridArea = gridArea,
            FuelCode = fuelCode,
            TechCode = techCode,
            GsrnCommitment = new V1.Commitment()
            {
                C = ByteString.CopyFrom(Commitment.Create(Group, gsrn.message, gsrn.r).C.ToByteArray())
            },
            QuantityCommitment = new V1.Commitment()
            {
                C = ByteString.CopyFrom(Commitment.Create(Group, quantity.message, quantity.r).C.ToByteArray())
            },
            OwnerPublicKey = new V1.PublicKey()
            {
                Content = ByteString.CopyFrom(owner.Export(KeyBlobFormat.RawPublicKey))
            },
        };

        var proof = new V1.IssueProductionCommand.Types.ProductionIssuedProof()
        {
            GsrnProof = new V1.CommitmentProof()
            {
                M = (ulong)gsrn.message,
                R = ByteString.CopyFrom(gsrn.r.ToByteArray()),
            },
            QuantityProof = new V1.CommitmentProof()
            {
                M = (ulong)quantity.message,
                R = ByteString.CopyFrom(quantity.r.ToByteArray()),
            }
        };

        var signature = Sign(issuingBodySigner, @event);

        var commandContent = new V1.IssueProductionCommand()
        {
            Event = @event,
            Signature = signature,
            Proof = proof,
        };

        return SendCommand(commandContent);
    }
}

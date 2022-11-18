using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client.Models;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Client;

public partial class ElectricityClient
{
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
        Key signer
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

        var signature = Sign(signer, @event);

        var commandContent = new V1.IssueProductionCommand()
        {
            Event = @event,
            Signature = signature,
            Proof = proof,
        };

        return SendCommand(commandContent);
    }
}

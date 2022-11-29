using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client.Models;

namespace ProjectOrigin.Electricity.Client;

public partial class ElectricityClient
{
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
    public Task<CommandId> IssueProductionCertificate(
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
        var @event = new V1.IssueProductionCommand.Types.ProductionIssuedEvent()
        {
            CertificateId = id.ToProto(),
            Period = inteval.ToProto(),
            GridArea = gridArea,
            FuelCode = fuelCode,
            TechCode = techCode,
            GsrnCommitment = gsrn.ToProtoCommitment(),
            QuantityCommitment = quantity.ToProtoCommitment(),
            OwnerPublicKey = new V1.PublicKey()
            {
                Content = ByteString.CopyFrom(owner.Export(KeyBlobFormat.RawPublicKey))
            },
        };

        var proof = new V1.IssueProductionCommand.Types.ProductionIssuedProof()
        {
            GsrnProof = gsrn.ToProtoCommitmentProof(),
            QuantityProof = quantity.ToProtoCommitmentProof()
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

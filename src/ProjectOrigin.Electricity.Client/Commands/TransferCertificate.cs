using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client.Models;

namespace ProjectOrigin.Electricity.Client;

public partial class ElectricityClient
{
    /// <summary>
    /// This is used to transfer a <a href="xref:granular_certificate#slice">Granular Certificate slice</a> from the current owner to a new owner.
    /// </summary>
    /// <param name="id">the federated certicate id for the certificate.</param>
    /// <param name="source">a shieldedValue of the source slice on the certificate from which to create the new slices.</param>
    /// <param name="quantity">a shieldedValue of the new slice.</param>
    /// <param name="remainder">a shieldedValue of the remainder slice, a Zero slice should be provided if all is transfered.</param>
    /// <param name="currentOwnerSigner">the signing key for the current owner of the slice.</param>
    /// <param name="newOwner">the Ed25519 publicKey which should be set as the owner of the certificate.</param>
    public Task<CommandId> TransferCertificate(
        FederatedCertifcateId id,
        ShieldedValue source,
        ShieldedValue quantity,
        ShieldedValue remainder,
        Key currentOwnerSigner,
        PublicKey newOwner
    )
    {
        var productionAllocationEvent = new V1.TransferProductionSliceCommand.Types.ProductionSliceTransferredEvent()
        {
            CertificateId = id.ToProto(),
            Slice = CreateSlice(source, quantity, remainder),
            NewOwner = ByteString.CopyFrom(newOwner.Export(KeyBlobFormat.RawPublicKey))
        };

        var command = new V1.TransferProductionSliceCommand()
        {
            Event = productionAllocationEvent,
            Signature = Sign(currentOwnerSigner, productionAllocationEvent),
            Proof = CreateSliceProof(source, quantity, remainder)
        };

        return SendCommand(command);
    }
}

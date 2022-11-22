using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client.Models;

namespace ProjectOrigin.Electricity.Client;

public partial class ElectricityClient
{
    public Task<TransactionId> TransferCertificate(
        string consumptionRegistry,
        Guid consumptionCertificateId,
        ShieldedValue source,
        ShieldedValue quantity,
        ShieldedValue remainder,
        Key consumptionSigner,
        PublicKey newOwner
    )
    {
        var certId = ToProtoId(consumptionRegistry, consumptionCertificateId);

        var productionAllocationEvent = new V1.TransferProductionSliceCommand.Types.ProductionSliceTransferredEvent()
        {
            CertificateId = certId,
            Slice = CreateSlice(source, quantity, remainder),
            NewOwner = ByteString.CopyFrom(newOwner.Export(KeyBlobFormat.RawPublicKey))
        };

        var command = new V1.TransferProductionSliceCommand()
        {
            Event = productionAllocationEvent,
            Signature = Sign(consumptionSigner, productionAllocationEvent),
            Proof = CreateSliceProof(source, quantity, remainder)
        };

        return SendCommand(command);
    }
}

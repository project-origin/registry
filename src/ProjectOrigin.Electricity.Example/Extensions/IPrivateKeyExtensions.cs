using System;
using System.Security.Cryptography;
using Google.Protobuf;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace ProjectOrigin.Electricity.Example.Extensions;

public static class IPrivateKeyExtensions
{
    public static Registry.V1.Transaction SignTransaction(this IPrivateKey signerKey, Common.V1.FederatedStreamId streamId, IMessage @event)
    {
        var header = new Registry.V1.TransactionHeader()
        {
            FederatedStreamId = streamId,
            PayloadType = @event.Descriptor.FullName,
            PayloadSha512 = ByteString.CopyFrom(SHA512.HashData(@event.ToByteArray())),
            Nonce = Guid.NewGuid().ToString(),
        };

        var transaction = new Registry.V1.Transaction()
        {
            Header = header,
            HeaderSignature = ByteString.CopyFrom(signerKey.Sign(header.ToByteArray())),
            Payload = @event.ToByteString()
        };

        return transaction;
    }

}

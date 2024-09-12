using System.Threading.Tasks;
using Google.Protobuf;
using System;
using System.Security.Cryptography;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;
using Google.Protobuf.WellKnownTypes;

namespace ProjectOrigin.Electricity.IntegrationTests;

public static class Helper
{
    public static async Task<TResult> RepeatUntilOrTimeout<TResult>(Func<Task<TResult>> getResultFunc, Func<TResult, bool> isValidFunc, TimeSpan timeout)
    {
        var began = DateTimeOffset.UtcNow;
        while (true)
        {
            var result = await getResultFunc();
            if (isValidFunc(result))
                return result;

            await Task.Delay(100);

            if (began + timeout < DateTimeOffset.UtcNow)
            {
                throw new TimeoutException();
            }
        }
    }

    public static V1.IssuedEvent CreateIssuedEvent(string registryName, string area, IPublicKey owner, SecretCommitmentInfo commitmentInfo, Guid certId)
    {
        return new Electricity.V1.IssuedEvent
        {
            CertificateId = new Common.V1.FederatedStreamId
            {
                Registry = registryName,
                StreamId = new Common.V1.Uuid
                {
                    Value = certId.ToString()
                },
            },
            Type = Electricity.V1.GranularCertificateType.Consumption,
            Period = new Electricity.V1.DateInterval
            {
                Start = Timestamp.FromDateTimeOffset(new DateTimeOffset(2023, 1, 1, 12, 0, 0, 0, TimeSpan.Zero)),
                End = Timestamp.FromDateTimeOffset(new DateTimeOffset(2023, 1, 1, 13, 0, 0, 0, TimeSpan.Zero))
            },
            GridArea = area,
            AssetIdHash = ByteString.Empty,
            QuantityCommitment = new Electricity.V1.Commitment
            {
                Content = ByteString.CopyFrom(commitmentInfo.Commitment.C),
                RangeProof = ByteString.CopyFrom(commitmentInfo.CreateRangeProof(certId.ToString()))
            },
            OwnerPublicKey = new Electricity.V1.PublicKey
            {
                Content = ByteString.CopyFrom(owner.Export())
            }
        };
    }

    public static async Task SendTransactions(this Registry.V1.RegistryService.RegistryServiceClient client, params Registry.V1.Transaction[] transaction)
    {
        var request = new Registry.V1.SendTransactionsRequest();
        request.Transactions.Add(transaction);
        await client.SendTransactionsAsync(request);
    }

    public static async Task<Registry.V1.GetStreamTransactionsResponse> GetStream(this Registry.V1.RegistryService.RegistryServiceClient client, Guid streamId)
    {
        return await client.GetStreamTransactionsAsync(new Registry.V1.GetStreamTransactionsRequest()
        {
            StreamId = new Common.V1.Uuid
            {
                Value = streamId.ToString()
            }
        });
    }

    public static async Task<Registry.V1.GetTransactionStatusResponse> GetStatus(this Registry.V1.RegistryService.RegistryServiceClient client, Registry.V1.Transaction transaction)
    {
        return await client.GetTransactionStatusAsync(new Registry.V1.GetTransactionStatusRequest
        {
            Id = Convert.ToBase64String(SHA256.HashData(transaction.ToByteArray()))
        });
    }

    public static Registry.V1.Transaction SignTransaction(Common.V1.FederatedStreamId streamId, IMessage @event, IPrivateKey signerKey)
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

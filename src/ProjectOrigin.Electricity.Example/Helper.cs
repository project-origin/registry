using System.Security.Cryptography;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NBitcoin;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;
using SimpleBase;

namespace ProjectOrigin.Electricity.Example;

public class Helper
{
    static DateTimeOffset Start = new DateTimeOffset(2023, 1, 1, 12, 0, 0, 0, TimeSpan.Zero);
    private string _registryName;
    private string _area;

    public Helper(string registryName, string area)
    {
        _registryName = registryName;
        _area = area;
    }

    public Electricity.V1.ConsumptionIssuedEvent CreateConsumptionIssuedEvent(Guid certId, SecretCommitmentInfo commitmentInfo, IPublicKey ownerKey)
    {
        var @event = new Electricity.V1.ConsumptionIssuedEvent
        {
            CertificateId = new Common.V1.FederatedStreamId
            {
                Registry = _registryName,
                StreamId = new Common.V1.Uuid
                {
                    Value = certId.ToString()
                },
            },
            Period = new Electricity.V1.DateInterval
            {
                Start = Timestamp.FromDateTimeOffset(Start),
                End = Timestamp.FromDateTimeOffset(Start.AddHours(1))
            },
            GridArea = _area,
            GsrnHash = ByteString.Empty,
            QuantityCommitment = new Electricity.V1.Commitment
            {
                Content = ByteString.CopyFrom(commitmentInfo.Commitment.C),
                RangeProof = ByteString.CopyFrom(commitmentInfo.CreateRangeProof(certId.ToString()))
            },
            OwnerPublicKey = new Electricity.V1.PublicKey
            {
                Content = ByteString.CopyFrom(ownerKey.Export())
            }
        };

        return @event;
    }

    public Registry.V1.Transaction SignTransaction(Common.V1.FederatedStreamId streamId, IMessage @event, IHDPrivateKey signerKey)
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
            HeaderSignature = new Registry.V1.Signature
            {
                Value = ByteString.CopyFrom(signerKey.Sign(header.ToByteArray()))
            },
            Payload = @event.ToByteString()
        };

        return transaction;
    }

    public async Task<TResult> RepeatUntilOrTimeout<TResult>(Func<Task<TResult>> getResultFunc, Func<TResult, bool> isValidFunc, TimeSpan timeout)
    {
        var began = DateTimeOffset.UtcNow;
        while (true)
        {
            var result = await getResultFunc();
            if (isValidFunc(result))
                return result;

            await Task.Delay(1000);

            if (began + timeout < DateTimeOffset.UtcNow)
            {
                throw new TimeoutException();
            }
        }
    }

    public Registry.V1.GetTransactionStatusRequest CreateStatusRequest(Registry.V1.Transaction signedTransaction)
    {
        var sha = SHA256.HashData(signedTransaction.ToByteArray());
        var transactionId = Base58.Bitcoin.Encode(sha);

        return new ProjectOrigin.Registry.V1.GetTransactionStatusRequest()
        {
            Id = new Registry.V1.TransactionId
            {
                Value = transactionId
            }
        };
    }
}

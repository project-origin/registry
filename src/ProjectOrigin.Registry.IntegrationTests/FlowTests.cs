using Xunit.Abstractions;
using ProjectOrigin.TestUtils;
using ProjectOrigin.Registry.Server;
using Xunit;
using System.Threading.Tasks;
using System.Collections.Generic;
using Google.Protobuf;
using System;
using System.Security.Cryptography;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.PedersenCommitment;
using SimpleBase;
using Google.Protobuf.WellKnownTypes;
using FluentAssertions;
using ProjectOrigin.Registry.V1;
using ProjectOrigin.Electricity.V1;

namespace ProjectOrigin.Electricity.IntegrationTests;

public class FlowTests : GrpcTestBase<Startup>, IClassFixture<ElectricityServiceFixture>
{
    private Secp256k1Algorithm _algorithm;
    private ElectricityServiceFixture _verifierFixture;
    private const string RegistryName = "SomeRegistry";

    private Registry.V1.RegistryService.RegistryServiceClient Client => new Registry.V1.RegistryService.RegistryServiceClient(_grpcFixture.Channel);

    public FlowTests(ElectricityServiceFixture verifierFixture, GrpcTestFixture<Startup> grpcFixture, ITestOutputHelper outputHelper) : base(grpcFixture, outputHelper)
    {
        _algorithm = new Secp256k1Algorithm();

        _verifierFixture = verifierFixture;
        grpcFixture.ConfigureHostConfiguration(new Dictionary<string, string?>()
        {
            {$"Verifiers:ProjectOrigin.Electricity.v1", _verifierFixture.Url},
            {"RegistryName", RegistryName}
        });
    }

    [Fact]
    public async Task test()
    {
        var owner = _algorithm.GenerateNewPrivateKey();

        var commitmentInfo = new SecretCommitmentInfo(250);
        var certId = Guid.NewGuid();

        ConsumptionIssuedEvent @event = CreateIssuedEvent(owner, commitmentInfo, certId);

        var transaction = SignTransaction(@event.CertificateId, @event, _verifierFixture.IssuerKey);

        var status = await GetStatus(transaction);
        status.Status.Should().Be(Registry.V1.TransactionState.Unknown);

        await SendTransaction(transaction);
        status = await GetStatus(transaction);
        status.Status.Should().Be(Registry.V1.TransactionState.Pending);

        status = await RepeatUntilOrTimeout(
            () => GetStatus(transaction),
            (result => result.Status == Registry.V1.TransactionState.Committed),
            TimeSpan.FromSeconds(60));

        status.Message.Should().BeEmpty();

        var stream = await GetStream(certId);
        stream.Transactions.Should().HaveCount(1);
    }

    private async Task<TResult> RepeatUntilOrTimeout<TResult>(Func<Task<TResult>> getResultFunc, Func<TResult, bool> isValidFunc, TimeSpan timeout)
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

    private ConsumptionIssuedEvent CreateIssuedEvent(IHDPrivateKey owner, SecretCommitmentInfo commitmentInfo, Guid certId)
    {
        return new Electricity.V1.ConsumptionIssuedEvent
        {
            CertificateId = new Common.V1.FederatedStreamId
            {
                Registry = RegistryName,
                StreamId = new Common.V1.Uuid
                {
                    Value = certId.ToString()
                },
            },
            Period = new Electricity.V1.DateInterval
            {
                Start = Timestamp.FromDateTimeOffset(new DateTimeOffset(2023, 1, 1, 12, 0, 0, 0, TimeSpan.Zero)),
                End = Timestamp.FromDateTimeOffset(new DateTimeOffset(2023, 1, 1, 13, 0, 0, 0, TimeSpan.Zero))
            },
            GridArea = _verifierFixture.IssuerArea,
            GsrnHash = ByteString.Empty,
            QuantityCommitment = new Electricity.V1.Commitment
            {
                Content = ByteString.CopyFrom(commitmentInfo.Commitment.C),
                RangeProof = ByteString.CopyFrom(commitmentInfo.CreateRangeProof(certId.ToString()))
            },
            OwnerPublicKey = new Electricity.V1.PublicKey
            {
                Content = ByteString.CopyFrom(owner.PublicKey.Export())
            }
        };
    }

    private async Task SendTransaction(Registry.V1.Transaction transaction)
    {
        var request = new Registry.V1.SendTransactionsRequest();
        request.Transactions.Add(transaction);
        await Client.SendTransactionsAsync(request);

    }

    private async Task<GetStreamTransactionsResponse> GetStream(Guid streamId)
    {
        return await Client.GetStreamTransactionsAsync(new Registry.V1.GetStreamTransactionsRequest()
        {
            StreamId = new Common.V1.Uuid
            {
                Value = streamId.ToString()
            }
        });
    }

    private async Task<Registry.V1.GetTransactionStatusResponse> GetStatus(Registry.V1.Transaction transaction)
    {
        TransactionId id = GetId(transaction);

        return await Client.GetTransactionStatusAsync(new Registry.V1.GetTransactionStatusRequest
        {
            Id = id
        });
    }

    private static TransactionId GetId(Transaction transaction)
    {
        return new Registry.V1.TransactionId
        {
            Value = Base58.Bitcoin.Encode(SHA256.HashData(transaction.ToByteArray()))
        };
    }

    private Registry.V1.Transaction SignTransaction(Common.V1.FederatedStreamId streamId, IMessage @event, IHDPrivateKey signerKey)
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
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProjectOrigin.Electricity.Server;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.Verifier.V1;
using Xunit.Abstractions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using FluentAssertions;
using System.Security.Cryptography;
using ProjectOrigin.PedersenCommitment;
using Xunit;
using ProjectOrigin.TestUtils;
using System.Text;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.Electricity.Server.Interfaces;
using ProjectOrigin.Electricity.Server.Services;

namespace ProjectOrigin.Electricity.IntegrationTests;

public class FlowTests : GrpcTestBase<Startup>
{
    private IPrivateKey _issuerKey;

    const string Area = "TestArea";
    const string Registry = "test-registry";

    public FlowTests(GrpcTestFixture<Startup> grpcFixture, ITestOutputHelper outputHelper) : base(grpcFixture, outputHelper)
    {
        _issuerKey = Algorithms.Ed25519.GenerateNewPrivateKey();

        grpcFixture.ConfigureHostConfiguration(new Dictionary<string, string?>()
        {
            {$"Issuers:{Area}", Convert.ToBase64String(Encoding.UTF8.GetBytes(_issuerKey.PublicKey.ExportPkixText()))},
            {$"Registries:{Registry}:Address", "http://localhost:5000"}
        });

        grpcFixture.testServicesConfigure = (services) =>
        {
            services.RemoveAll<IRemoteModelLoader>();
            services.AddTransient<IRemoteModelLoader, GrpcRemoteModelLoader>();
        };
    }

    [Fact]
    public async Task IssueConsumptionCertificate_Success()
    {
        var owner = Algorithms.Secp256k1.GenerateNewPrivateKey();

        var commitmentInfo = new SecretCommitmentInfo(250);
        var certId = Guid.NewGuid().ToString();

        var @event = new Electricity.V1.IssuedEvent
        {
            CertificateId = new Common.V1.FederatedStreamId
            {
                Registry = Registry,
                StreamId = new Common.V1.Uuid
                {
                    Value = certId
                },
            },
            Type = Electricity.V1.GranularCertificateType.Consumption,
            Period = new Electricity.V1.DateInterval
            {
                Start = Timestamp.FromDateTimeOffset(new DateTimeOffset(2023, 1, 1, 12, 0, 0, 0, TimeSpan.Zero)),
                End = Timestamp.FromDateTimeOffset(new DateTimeOffset(2023, 1, 1, 13, 0, 0, 0, TimeSpan.Zero))
            },
            GridArea = Area,
            AssetIdHash = ByteString.Empty,
            QuantityCommitment = new Electricity.V1.Commitment
            {
                Content = ByteString.CopyFrom(commitmentInfo.Commitment.C),
                RangeProof = ByteString.CopyFrom(commitmentInfo.CreateRangeProof(certId))
            },
            OwnerPublicKey = new Electricity.V1.PublicKey
            {
                Content = ByteString.CopyFrom(owner.PublicKey.Export())
            }
        };

        VerifyTransactionResponse result = await SignEventAndVerify(@event.CertificateId, @event, _issuerKey);

        result.ErrorMessage.Should().BeEmpty();
        result.Valid.Should().BeTrue();
    }

    private async Task<VerifyTransactionResponse> SignEventAndVerify(Common.V1.FederatedStreamId streamId, IMessage @event, IPrivateKey key, IEnumerable<Registry.V1.Transaction>? stream = null)
    {
        var client = new Verifier.V1.VerifierService.VerifierServiceClient(_grpcFixture.Channel);
        var request = new Verifier.V1.VerifyTransactionRequest
        {
            Transaction = SignEvent(streamId, @event, key)
        };

        if (stream is not null)
            request.Stream.AddRange(stream);

        var result = await client.VerifyTransactionAsync(request);
        return result;
    }

    private static Registry.V1.Transaction SignEvent(Common.V1.FederatedStreamId streamId, IMessage @event, IPrivateKey signerKey)
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


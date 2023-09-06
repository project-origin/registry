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

public class ExceptionTest : GrpcTestBase<Startup>
{
    private IPrivateKey _issuerKey;

    const string Area = "TestArea";
    const string Registry = "test-registry";

    public ExceptionTest(GrpcTestFixture<Startup> grpcFixture, ITestOutputHelper outputHelper) : base(grpcFixture, outputHelper)
    {
        _issuerKey = Algorithms.Ed25519.GenerateNewPrivateKey();

        grpcFixture.ConfigureHostConfiguration(new Dictionary<string, string?>()
        {
            {$"Issuers:{Area}", Convert.ToBase64String(Encoding.UTF8.GetBytes(_issuerKey.PublicKey.ExportPkixText()))},
            {$"Registries:{Registry}:Address", "http://localhost:80"}
        });

        grpcFixture.testServicesConfigure = (services) =>
        {
            services.RemoveAll<IRemoteModelLoader>();
            services.AddTransient<IRemoteModelLoader, GrpcRemoteModelLoader>();
        };
    }

    [Fact]
    public async Task UnknownPayloadType_ReturnsUnexpectedError()
    {
        var client = new VerifierService.VerifierServiceClient(_grpcFixture.Channel);

        var request = CreateInvalidSignedEvent(_issuerKey);

        var result = await client.VerifyTransactionAsync(request);

        result.ErrorMessage.Should().Be("Unexpected error while verifying transaction");
        result.Valid.Should().BeFalse();
    }

    private VerifyTransactionRequest CreateInvalidSignedEvent(IPrivateKey signerKey)
    {
        IMessage @event = new V1.IssuedEvent();

        var header = new Registry.V1.TransactionHeader()
        {
            FederatedStreamId = new Common.V1.FederatedStreamId()
            {
                Registry = Registry,
                StreamId = new Common.V1.Uuid
                {
                    Value = Guid.NewGuid().ToString()
                }
            },
            PayloadType = "SomeUnknownType",
            PayloadSha512 = ByteString.CopyFrom(SHA512.HashData(@event.ToByteArray())),
            Nonce = Guid.NewGuid().ToString(),
        };

        var transaction = new Registry.V1.Transaction()
        {
            Header = header,
            HeaderSignature = ByteString.CopyFrom(signerKey.Sign(header.ToByteArray())),
            Payload = @event.ToByteString()
        };

        return new VerifyTransactionRequest
        {
            Transaction = transaction
        };
    }
}


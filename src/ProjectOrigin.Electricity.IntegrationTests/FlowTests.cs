using AutoFixture;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client.Models;
using ProjectOrigin.Electricity.IntegrationTests.Helpers;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Electricity.Server;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using Xunit.Abstractions;

namespace ProjectOrigin.Electricity.IntegrationTests;

public class FlowTests : ElectricityClientTestBase
{
    private static Key _dk1_issuer_key = Key.Create(SignatureAlgorithm.Ed25519);
    private static Key _dk2_issuer_key = Key.Create(SignatureAlgorithm.Ed25519);


    private const string Area_DK1 = nameof(Area_DK1);
    private const string Area_DK2 = nameof(Area_DK2);

    public FlowTests(GrpcTestFixture<Startup> fixture, ITestOutputHelper outputHelper) : base(fixture, outputHelper)
    {
        var blockchainMock = new Mock<IBlockchainConnector>();
        blockchainMock.Setup(obj => obj.PublishBytes(It.IsAny<byte[]>())).Returns(Task.FromResult(new TransactionReference(new Fixture().Create<string>())));
        blockchainMock.Setup(obj => obj.GetBlock(It.IsAny<TransactionReference>())).Returns(Task.FromResult<Block?>(new Block(new Fixture().Create<string>(), true)));

        fixture.ConfigureWebHost((webHostBuilder) =>
        {
            webHostBuilder.ConfigureServices((services) =>
            {
                services.AddTransient<IBlockchainConnector>((s) => blockchainMock.Object);
                services.Configure<IssuerOptions>(option =>
                {
                    option.AreaIssuerPublicKey = (area) => area switch
                    {
                        Area_DK1 => _dk1_issuer_key.PublicKey,
                        Area_DK2 => _dk2_issuer_key.PublicKey,
                        _ => null,
                    };
                });
            });
        });
    }

    [Fact]
    public async Task IssueConsumptionCertificate_Success()
    {
        var gsrn = Client.Group.Commit(150);
        var quantity = Client.Group.Commit(250);
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

        var id = await Client.IssueConsumptionCertificate(
            new FederatedCertifcateId(
                Registries.RegistryA,
                Guid.NewGuid()
            ),
            new Client.Models.DateInterval(
                new DateTimeOffset(2022, 10, 1, 12, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2022, 10, 1, 13, 0, 0, TimeSpan.Zero)
            ),
            Area_DK1,
            new Client.Models.ShieldedValue(150, gsrn.r),
            new Client.Models.ShieldedValue(250, quantity.r),
            ownerKey.PublicKey,
            _dk1_issuer_key
            );

        var res = await GetResult();

        AssertValidResponse(id, res);
    }

    [Fact]
    public async Task IssueProductionCertificate_Success()
    {
        var gsrn = Client.Group.Commit(150);
        var quantity = Client.Group.Commit(250);
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

        var id = await Client.IssueProductionCertificate(
            new FederatedCertifcateId(
                Registries.RegistryB,
                Guid.NewGuid()
            ),
            new Client.Models.DateInterval(
                new DateTimeOffset(2022, 10, 1, 12, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2022, 10, 1, 13, 0, 0, TimeSpan.Zero)
            ),
            Area_DK2,
            "F01050100",
            "T020002",
            new Client.Models.ShieldedValue(150, gsrn.r),
            new Client.Models.ShieldedValue(250, quantity.r),
            ownerKey.PublicKey,
            _dk2_issuer_key
            );

        var res = await GetResult();

        AssertValidResponse(id, res);
    }


    [Fact]
    public async Task TransferCertificate_Success()
    {
        var gsrn = Client.Group.Commit(570000000001213);

        var quantity = Client.Group.Commit(250);
        var transfer1 = Client.Group.Commit(150);
        var remainder1 = Client.Group.Commit(100);

        var transfer2 = Client.Group.Commit(100);
        var remainder2 = Client.Group.Commit(50);

        var ownerKey1 = Key.Create(SignatureAlgorithm.Ed25519);
        var ownerKey2 = Key.Create(SignatureAlgorithm.Ed25519);
        var ownerKey3 = Key.Create(SignatureAlgorithm.Ed25519);

        var certId = new FederatedCertifcateId(
            Registries.RegistryB,
            Guid.NewGuid()
        );

        {
            var id = await Client.IssueProductionCertificate(
                certId,
                new Client.Models.DateInterval(
                    new DateTimeOffset(2022, 10, 1, 12, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2022, 10, 1, 13, 0, 0, TimeSpan.Zero)
                ),
                Area_DK2,
                "F01050100",
                "T020002",
                gsrn.ToShieldedValue(),
                quantity.ToShieldedValue(),
                ownerKey1.PublicKey,
                _dk2_issuer_key
                );
            var res = await GetResult();
            AssertValidResponse(id, res);
        }

        {
            var id = await Client.TransferCertificate(
                certId,
                quantity.ToShieldedValue(),
                transfer1.ToShieldedValue(),
                remainder1.ToShieldedValue(),
                ownerKey1,
                ownerKey2.PublicKey
                );
            var res = await GetResult();
            AssertValidResponse(id, res);
        }

        {
            var id = await Client.TransferCertificate(
                certId,
                transfer1.ToShieldedValue(),
                transfer2.ToShieldedValue(),
                remainder2.ToShieldedValue(),
                ownerKey2,
                ownerKey3.PublicKey
                );
            var res = await GetResult();
            AssertValidResponse(id, res);
        }
    }


    [Fact]
    public async Task ClaimCertificate_Success()
    {
        var gsrn = Client.Group.Commit(570000000001213);


        var consCertId = new FederatedCertifcateId(
            Registries.RegistryB,
            Guid.NewGuid()
        );
        var consQuantity = Client.Group.Commit(150);

        var prodCertId = new FederatedCertifcateId(
            Registries.RegistryB,
            Guid.NewGuid()
        );
        var prodQuantity = Client.Group.Commit(250);

        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

        var id_1 = await Client.IssueConsumptionCertificate(
            consCertId,
            new Client.Models.DateInterval(
                new DateTimeOffset(2022, 10, 1, 12, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2022, 10, 1, 13, 0, 0, TimeSpan.Zero)
            ),
            Area_DK1,
            gsrn.ToShieldedValue(),
            consQuantity.ToShieldedValue(),
            ownerKey.PublicKey,
            _dk1_issuer_key
            );

        var res_1 = await GetResult();
        AssertValidResponse(id_1, res_1);

        var id_2 = await Client.IssueProductionCertificate(
            prodCertId,
            new Client.Models.DateInterval(
                new DateTimeOffset(2022, 10, 1, 12, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2022, 10, 1, 13, 0, 0, TimeSpan.Zero)
            ),
            Area_DK1,
            "F01050100",
            "T020002",
            gsrn.ToShieldedValue(),
            prodQuantity.ToShieldedValue(),
            ownerKey.PublicKey,
            _dk1_issuer_key
            );

        var res_2 = await GetResult();
        AssertValidResponse(id_2, res_2);

        var claimQuantity = Client.Group.Commit(150);
        var consRemainder = Client.Group.Commit(0);
        var prodRemainder = Client.Group.Commit(100);

        var id_3 = await Client.ClaimCertificate(
            claimQuantity.ToShieldedValue(),
            consCertId,
            consQuantity.ToShieldedValue(),
            consRemainder.ToShieldedValue(),
            ownerKey,
            prodCertId,
            prodQuantity.ToShieldedValue(),
            prodRemainder.ToShieldedValue(),
            ownerKey
            );

        var res_3 = await GetResult();
        AssertValidResponse(id_3, res_3);
    }

    void AssertValidResponse(CommandId id, CommandStatusEvent? res)
    {
        Assert.NotNull(res);
        Assert.Equal(id.Hash, res!.Id.Hash);
        if (!string.IsNullOrEmpty(res.Error)) throw new Xunit.Sdk.XunitException(res.Error);
        Assert.Equal(CommandState.Succeeded, res.State);
    }
}

public static class Extensions
{
    public static Client.Models.ShieldedValue ToShieldedValue(this PedersenCommitment.CommitmentParameters cm)
    {
        return new Client.Models.ShieldedValue((ulong)cm.m, cm.r);
    }
}

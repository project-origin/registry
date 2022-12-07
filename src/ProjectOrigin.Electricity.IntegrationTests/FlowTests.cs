using AutoFixture;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client;
using ProjectOrigin.Electricity.Client.Models;
using ProjectOrigin.Electricity.IntegrationTests.Helpers;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Electricity.Server;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using Xunit.Abstractions;

namespace ProjectOrigin.Electricity.IntegrationTests;

public class FlowTests : RegisterClientTestBase
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

    // [Fact]
    // public async Task IssueConsumptionCertificate_Success()
    // {
    //     var commandBuilder = new ElectricityCommandBuilder();
    //     var gsrn = Group.Default.Commit(150);
    //     var quantity = Group.Default.Commit(250);
    //     var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

    //     var id = await commandBuilder
    //         .IssueConsumptionCertificate(
    //             new FederatedCertifcateId(
    //                 Registries.RegistryA,
    //                 Guid.NewGuid()
    //             ),
    //             new Client.Models.DateInterval(
    //                 new DateTimeOffset(2022, 10, 1, 12, 0, 0, TimeSpan.Zero),
    //                 new DateTimeOffset(2022, 10, 1, 13, 0, 0, TimeSpan.Zero)
    //             ),
    //             Area_DK1,
    //             new Client.Models.ShieldedValue(150, gsrn.r),
    //             new Client.Models.ShieldedValue(250, quantity.r),
    //             ownerKey.PublicKey,
    //             _dk1_issuer_key
    //             )
    //         .Execute(Client);

    //     var res = await GetResult();

    //     AssertValidResponse(id, res);
    // }

    // [Fact]
    // public async Task IssueProductionCertificate_Success()
    // {
    //     var commandBuilder = new ElectricityCommandBuilder();
    //     var gsrn = Group.Default.Commit(150);
    //     var quantity = Group.Default.Commit(250);
    //     var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

    //     var id = await commandBuilder
    //         .IssueProductionCertificate(
    //         new FederatedCertifcateId(
    //             Registries.RegistryB,
    //             Guid.NewGuid()
    //         ),
    //         new Client.Models.DateInterval(
    //             new DateTimeOffset(2022, 10, 1, 12, 0, 0, TimeSpan.Zero),
    //             new DateTimeOffset(2022, 10, 1, 13, 0, 0, TimeSpan.Zero)
    //         ),
    //         Area_DK2,
    //         "F01050100",
    //         "T020002",
    //         new Client.Models.ShieldedValue(150, gsrn.r),
    //         new Client.Models.ShieldedValue(250, quantity.r),
    //         ownerKey.PublicKey,
    //         _dk2_issuer_key
    //         )
    //         .Execute(Client);

    //     var res = await GetResult();

    //     AssertValidResponse(id, res);
    // }


    // [Fact]
    // public async Task SliceCertificate_Success()
    // {
    //     var commandBuilder = new ElectricityCommandBuilder();
    //     var gsrn = Group.Default.Commit(570000000001213);

    //     var ownerKey1 = Key.Create(SignatureAlgorithm.Ed25519);
    //     var ownerKey2 = Key.Create(SignatureAlgorithm.Ed25519);
    //     var ownerKey3 = Key.Create(SignatureAlgorithm.Ed25519);

    //     var slice_0 = new ShieldedValue(250);
    //     var slice_1 = new ShieldedValue(150);
    //     var slice_1_1 = new ShieldedValue(100);
    //     var slice_1_2 = new ShieldedValue(50);

    //     var slicer = new Slicer(slice_0);
    //     slicer.CreateSlice(slice_1, ownerKey2.PublicKey);
    //     var collection1 = slicer.Collect();
    //     Assert.NotNull(collection1.Remainder);

    //     var slicer2 = new Slicer(slice_1);
    //     slicer2.CreateSlice(slice_1_1, ownerKey3.PublicKey);
    //     slicer2.CreateSlice(slice_1_2, ownerKey1.PublicKey);
    //     var collection2 = slicer2.Collect();
    //     Assert.Null(collection2.Remainder);

    //     var certId = new FederatedCertifcateId(
    //         Registries.RegistryB,
    //         Guid.NewGuid()
    //     );

    //     var id = await commandBuilder
    //         .IssueProductionCertificate(
    //             certId,
    //             new Client.Models.DateInterval(
    //                 new DateTimeOffset(2022, 10, 1, 12, 0, 0, TimeSpan.Zero),
    //                 new DateTimeOffset(2022, 10, 1, 13, 0, 0, TimeSpan.Zero)
    //             ),
    //             Area_DK2,
    //             "F01050100",
    //             "T020002",
    //             gsrn.ToShieldedValue(),
    //             slice_0,
    //             ownerKey1.PublicKey,
    //             _dk2_issuer_key
    //             )
    //         .SliceCertificate(
    //             certId,
    //             collection1,
    //             ownerKey1
    //             )
    //         .SliceCertificate(
    //             certId,
    //             collection2,
    //             ownerKey2
    //             )
    //         .Execute(Client);

    //     var res = await GetResult();
    //     AssertValidResponse(id, res);
    // }

    // [Fact]
    // public async Task ClaimCertificate_Success()
    // {
    //     var commandBuilder = new ElectricityCommandBuilder();
    //     var gsrn = Group.Default.Commit(570000000001213);
    //     var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

    //     var consCertId = new FederatedCertifcateId(
    //         Registries.RegistryB,
    //         Guid.NewGuid()
    //     );
    //     var consQuantity = new ShieldedValue(150);

    //     var prodCertId = new FederatedCertifcateId(
    //         Registries.RegistryB,
    //         Guid.NewGuid()
    //     );
    //     var prodQuantity = new ShieldedValue(250);
    //     var prod_slice = new ShieldedValue(150);

    //     var claimQuantity = new ShieldedValue(150);

    //     var slicer = new Slicer(prodQuantity);
    //     slicer.CreateSlice(prod_slice, ownerKey.PublicKey);
    //     var collection1 = slicer.Collect();
    //     Assert.NotNull(collection1.Remainder);

    //     var id = await commandBuilder
    //         .IssueProductionCertificate(
    //             prodCertId,
    //             new Client.Models.DateInterval(
    //                 new DateTimeOffset(2022, 10, 1, 12, 0, 0, TimeSpan.Zero),
    //                 new DateTimeOffset(2022, 10, 1, 13, 0, 0, TimeSpan.Zero)
    //             ),
    //             Area_DK1,
    //             "F01050100",
    //             "T020002",
    //             gsrn.ToShieldedValue(),
    //             prodQuantity,
    //             ownerKey.PublicKey,
    //             _dk1_issuer_key
    //         )
    //         .IssueConsumptionCertificate(
    //             consCertId,
    //             new Client.Models.DateInterval(
    //                 new DateTimeOffset(2022, 10, 1, 12, 0, 0, TimeSpan.Zero),
    //                 new DateTimeOffset(2022, 10, 1, 13, 0, 0, TimeSpan.Zero)
    //             ),
    //             Area_DK1,
    //             gsrn.ToShieldedValue(),
    //             consQuantity,
    //             ownerKey.PublicKey,
    //             _dk1_issuer_key
    //             )
    //         .ClaimCertificate(
    //             claimQuantity,
    //             consCertId,
    //             consQuantity,
    //             ownerKey,
    //             prodCertId,
    //             prod_slice,
    //             ownerKey
    //         )
    //         .Execute(Client);

    //     AssertValidResponse(id, await GetResult());
    // }

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
    public static ShieldedValue ToShieldedValue(this CommitmentParameters cm)
    {
        return new ShieldedValue((uint)cm.m, cm.r);
    }
}

using System;
using System.Collections.Generic;
using ProjectOrigin.Electricity.Server;
using Xunit.Abstractions;
using Xunit;
using ProjectOrigin.TestUtils;
using AutoFixture;
using Microsoft.Extensions.Options;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;

namespace ProjectOrigin.Electricity.IntegrationTests;

public class MisconfigurationTest : GrpcTestBase<Startup>
{
    private Fixture _fixture;
    const string Area = "TestArea";

    public MisconfigurationTest(GrpcTestFixture<Startup> grpcFixture, ITestOutputHelper outputHelper) : base(grpcFixture, outputHelper)
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void OptionsValidationException_IfInvalidKeyFormat()
    {
        _grpcFixture.ConfigureHostConfiguration(new Dictionary<string, string?>()
        {
            {$"Issuers:{Area}", Convert.ToBase64String(_fixture.Create<byte[]>())}
        });

        var ex = Assert.Throws<OptionsValidationException>(() => { var channel = _grpcFixture.Channel; });
        Assert.Equal($"A issuer key ”{Area}” is a invalid format.", ex.Message);
    }

    [Fact]
    public void OptionsValidationException_IfNoAreaIssuerDefined()
    {
        _grpcFixture.ConfigureHostConfiguration(new Dictionary<string, string?>()
        {
        });

        var ex = Assert.Throws<OptionsValidationException>(() => { var channel = _grpcFixture.Channel; });
        Assert.Equal("No Issuer areas configured.", ex.Message);
    }

    [Fact]
    public void OptionsValidationException_InvalidUrl()
    {
        var algorithm = new Secp256k1Algorithm();
        var issuerKey = algorithm.GenerateNewPrivateKey();

        _grpcFixture.ConfigureHostConfiguration(new Dictionary<string, string?>()
        {
            {$"Issuers:{Area}", Convert.ToBase64String(issuerKey.PublicKey.Export())},
            {$"Registries:MyRegistry:Address", "This is not a url"}
        });

        var ex = Assert.Throws<OptionsValidationException>(() => { var channel = _grpcFixture.Channel; });
        Assert.Equal("Invalid URL address specified for registry ”MyRegistry”", ex.Message);
    }
}

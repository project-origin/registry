using AutoFixture;
using FluentAssertions;
using ProjectOrigin.WalletSystem.Server.HDWallet;
using Xunit;

namespace ProjectOrigin.Electricity.Tests;

public class Secp256k1Tests
{
    private Secp256k1Algorithm _algorithm;
    private Fixture _fixture;

    public Secp256k1Tests()
    {
        _algorithm = new Secp256k1Algorithm();
        _fixture = new Fixture();
    }

    [Fact]
    public void signature_is_valid()
    {
        var key = _algorithm.Create();
        var data = _fixture.Create<byte[]>();
        var signature = key.Sign(data).ToArray();

        var verifcationResult = key.PublicKey.VerifySignature(data, signature);

        verifcationResult.Should().BeTrue();
    }


    [Fact]
    public void signature_is_invalid()
    {
        var key = _algorithm.Create();
        var otherKey = _algorithm.Create();
        var data = _fixture.Create<byte[]>();
        var signature = otherKey.Sign(data).ToArray();

        var verifcationResult = key.PublicKey.VerifySignature(data, signature);

        verifcationResult.Should().BeFalse();
    }
}

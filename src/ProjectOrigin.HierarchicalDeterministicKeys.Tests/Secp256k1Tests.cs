using AutoFixture;
using FluentAssertions;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
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
        var key = _algorithm.GenerateNewPrivateKey();
        var data = _fixture.Create<byte[]>();
        var signature = key.Sign(data).ToArray();

        var verifcationResult = key.PublicKey.Verify(data, signature);

        verifcationResult.Should().BeTrue();
    }

    [Fact]
    public void signature_is_invalid()
    {
        var key = _algorithm.GenerateNewPrivateKey();
        var otherKey = _algorithm.GenerateNewPrivateKey();
        var data = _fixture.Create<byte[]>();
        var signature = otherKey.Sign(data).ToArray();

        var verifcationResult = key.PublicKey.Verify(data, signature);

        verifcationResult.Should().BeFalse();
    }
}

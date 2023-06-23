using AutoFixture;
using FluentAssertions;
using ProjectOrigin.HierarchicalDeterministicKeys;
using Xunit;

namespace ProjectOrigin.Electricity.Tests;

public class Secp256k1Tests
{
    private Fixture _fixture;

    public Secp256k1Tests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void signature_is_valid()
    {
        var key = Algorithms.Secp256k1.GenerateNewPrivateKey();
        var data = _fixture.Create<byte[]>();
        var signature = key.Sign(data).ToArray();

        var verifcationResult = key.PublicKey.Verify(data, signature);

        verifcationResult.Should().BeTrue();
    }

    [Fact]
    public void signature_is_invalid()
    {
        var key = Algorithms.Secp256k1.GenerateNewPrivateKey();
        var otherKey = Algorithms.Secp256k1.GenerateNewPrivateKey();
        var data = _fixture.Create<byte[]>();
        var signature = otherKey.Sign(data).ToArray();

        var verifcationResult = key.PublicKey.Verify(data, signature);

        verifcationResult.Should().BeFalse();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using AutoFixture;
using FluentAssertions;
using ProjectOrigin.VerifiableEventStore.Extensions;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventProver;
using Xunit;

namespace ProjectOrigin.VerifiableEventStore.Tests;

public class MerkleTreeTests
{

    [Fact]
    public void VerifyBalanced()
    {
        var fixture = new Fixture();

        var e1 = fixture.Create<byte[]>();
        var e2 = fixture.Create<byte[]>();
        var e3 = fixture.Create<byte[]>();
        var e4 = fixture.Create<byte[]>();
        var e5 = fixture.Create<byte[]>();
        var e6 = fixture.Create<byte[]>();
        var e7 = fixture.Create<byte[]>();
        var e8 = fixture.Create<byte[]>();

        var events = new List<byte[]>() { e1, e2, e3, e4, e5, e6, e7, e8 };

        var root = events.CalculateMerkleRoot(x => x);

        var h12 = Sha256(Sha256(e1), Sha256(e2));
        var h34 = Sha256(Sha256(e3), Sha256(e4));
        var h56 = Sha256(Sha256(e5), Sha256(e6));
        var h78 = Sha256(Sha256(e7), Sha256(e8));

        var h1234 = Sha256(h12, h34);
        var h5678 = Sha256(h56, h78);

        var h12345678 = Sha256(h1234, h5678);

        root.Should().BeEquivalentTo(h12345678);
    }

    [Fact]
    public void VerifyUnbalanced()
    {
        var fixture = new Fixture();

        var e1 = fixture.Create<byte[]>();
        var e2 = fixture.Create<byte[]>();
        var e3 = fixture.Create<byte[]>();
        var e4 = fixture.Create<byte[]>();
        var e5 = fixture.Create<byte[]>();
        var e6 = fixture.Create<byte[]>();

        var events = new List<byte[]>() { e1, e2, e3, e4, e5, e6 };

        var root = events.CalculateMerkleRoot(x => x);

        var h12 = Sha256(Sha256(e1), Sha256(e2));
        var h34 = Sha256(Sha256(e3), Sha256(e4));
        var h56 = Sha256(Sha256(e5), Sha256(e6));
        var h66 = Sha256(Sha256(e6), Sha256(e6));

        var h1234 = Sha256(h12, h34);
        var h5666 = Sha256(h56, h66);

        var h12345666 = Sha256(h1234, h5666);

        root.Should().BeEquivalentTo(h12345666);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(32)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(66)]
    public void VerifyNumberOf(int number)
    {
        var fixture = new Fixture();

        var events = new List<byte[]>();

        for (int i = 0; i < number; i++)
        {
            events.Add(fixture.Create<byte[]>());
        }

        var root = events.CalculateMerkleRoot(x => x);

        Assert.NotNull(root);
    }

    [Theory]
    [InlineData(2, 1, 1)]
    [InlineData(4, 1, 2)]
    [InlineData(8, 5, 3)]
    [InlineData(16, 5, 4)]
    [InlineData(1 << 10, 5, 10)]
    public void GetRequiredHashes_ValidateNumberOfHashesReturned(int numberOfEvents, int leafIndex, int numberOfHashes)
    {
        var events = new Fixture().CreateMany<StreamTransaction>(numberOfEvents).ToList();
        var hashes = events.GetRequiredHashes(x => x.Payload, leafIndex);

        Assert.Equal(numberOfHashes, hashes.Count());
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 0)]
    [InlineData(3, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 0)]
    [InlineData(4, 1)]
    [InlineData(4, 2)]
    [InlineData(4, 3)]
    [InlineData(8, 0)]
    [InlineData(8, 1)]
    [InlineData(8, 2)]
    [InlineData(8, 3)]
    [InlineData(8, 4)]
    [InlineData(8, 5)]
    [InlineData(8, 6)]
    [InlineData(8, 7)]
    [InlineData(16, 9)]
    [InlineData(27, 10)]
    [InlineData(32, 15)]
    [InlineData(32, 31)]
    public void GetRequiredHashes_ValidateHashesCorrect(int numberOfEvents, int leafIndex)
    {
        var events = new Fixture().CreateMany<StreamTransaction>(numberOfEvents).ToList();
        var hashes = events.GetRequiredHashes(x => x.Payload, leafIndex);

        MerkleProof merkleProof = new(events[leafIndex].TransactionHash, events[leafIndex].Payload, leafIndex, hashes);

        var calculatedRoot = HashRootFromMerkleProof(merkleProof);

        var rootHash = events.CalculateMerkleRoot(e => e.Payload);

        Assert.Equal(rootHash, calculatedRoot);
    }

    private static byte[] HashRootFromMerkleProof(MerkleProof proof)
    {
        var balancedTreeNodeCount = (int)Math.Pow(2, proof.Hashes.Count());
        if (proof.LeafIndex >= balancedTreeNodeCount)
            balancedTreeNodeCount = (int)Math.Pow(2, proof.Hashes.Count() + 1);

        return HashRootFromMerkleProof(proof.Hashes, proof.LeafIndex, proof.Transaction, balancedTreeNodeCount);
    }


    private static byte[] HashRootFromMerkleProof(IEnumerable<byte[]> hashes, int leafIndex, byte[] leafContent, int balancedCount)
    {
        if (!hashes.Any())
        {
            var data = SHA256.HashData(leafContent);
            for (int i = (int)Math.Log(balancedCount, 2); i > 0; i--)
            {
                data = SHA256.HashData(data.Concat(data).ToArray());
            }
            return data;
        }
        if (hashes.Count() == 1 && balancedCount == 2)
        {
            if (leafIndex == 0)
                return Sha256(SHA256.HashData(leafContent), hashes.Single());
            else
                return Sha256(hashes.Single(), SHA256.HashData(leafContent));
        }
        else
        {
            var half = balancedCount / 2;
            if (leafIndex >= half)
            {
                return Sha256(hashes.First(), HashRootFromMerkleProof(hashes.Skip(1), leafIndex - half, leafContent, half));
            }
            else
            {
                return Sha256(HashRootFromMerkleProof(hashes.Take(hashes.Count() - 1), leafIndex, leafContent, half), hashes.Last());
            }
        }
    }

    private static byte[] Sha256(byte[] a)
    {
        return SHA256.HashData(a);
    }

    private static byte[] Sha256(byte[] a, byte[] b)
    {
        return SHA256.HashData(a.Concat(b).ToArray());
    }
}

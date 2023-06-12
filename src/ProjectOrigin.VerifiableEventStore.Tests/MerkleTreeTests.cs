using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using ProjectOrigin.VerifiableEventStore.Extensions;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Tests;

public class MerkleTreeTests
{
    [Theory]
    [InlineData(2, 1, 1)]
    [InlineData(4, 1, 2)]
    [InlineData(8, 5, 3)]
    [InlineData(16, 5, 4)]
    public void GetRequiredHashes_ValidateNumberOfHashesReturned(int numberOfEvents, int leafIndex, int numberOfHashes)
    {
        var events = new Fixture().CreateMany<VerifiableEvent>(numberOfEvents);
        var hashes = events.GetRequiredHashes(x => x.Content, leafIndex);

        Assert.Equal(numberOfHashes, hashes.Count());
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(2, 0)]
    [InlineData(4, 0)]
    [InlineData(4, 1)]
    [InlineData(4, 2)]
    [InlineData(4, 3)]
    [InlineData(8, 5)]
    [InlineData(16, 9)]
    [InlineData(32, 15)]
    public void GetRequiredHashes_ValidateHashesCorrect(int numberOfEvents, int leafIndex)
    {
        var events = new Fixture().CreateMany<VerifiableEvent>(numberOfEvents);
        var hashes = events.GetRequiredHashes(x => x.Content, leafIndex);

        var calculatedRoot = HashRootFromMerkleProof(hashes, leafIndex, events.Skip(leafIndex).First().Content);

        var rootHash = events.CalculateMerkleRoot(e => e.Content);

        Assert.Equal(rootHash, calculatedRoot);
    }

    private byte[] HashRootFromMerkleProof(IEnumerable<byte[]> hashes, int leafIndex, byte[] content)
    {
        if (hashes.Count() == 1)
        {
            if (leafIndex == 0)
            {
                return SHA256Array.HashData(SHA256.HashData(content), hashes.Single());
            }
            else
            {
                return SHA256Array.HashData(hashes.Single(), SHA256.HashData(content));
            }
        }
        else
        {
            var numberOfEvents = (int)Math.Pow(2, hashes.Count());
            if (leafIndex >= numberOfEvents / 2)
            {
                return SHA256Array.HashData(hashes.First(), HashRootFromMerkleProof(hashes.Skip(1), leafIndex - numberOfEvents / 2, content));
            }
            else
            {
                return SHA256Array.HashData(HashRootFromMerkleProof(hashes.Take(hashes.Count() - 1), leafIndex, content), hashes.Last());

            }
        }
    }
}

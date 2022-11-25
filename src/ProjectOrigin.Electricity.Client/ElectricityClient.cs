using System.Numerics;
using Google.Protobuf;
using Grpc.Net.Client;
using ProjectOrigin.Electricity.Client.Models;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Client;

/// <summary>
/// Class that enables one to communicate with a registry.
/// </summary>
public partial class ElectricityClient : RegisterClient
{
    /// <summary>
    /// Create a Electricity client based on a string url for the address of the gRPC endpoint for the registry.
    /// </summary>
    /// <param name="registryAddress">the url with the address to the registry.</param>
    public ElectricityClient(string registryAddress) : base(registryAddress)
    {
    }

    /// <summary>
    /// Create a Electricity client based on an existing GrpcChannel.
    /// </summary>
    /// <param name="channel">the channel to use.</param>
    public ElectricityClient(GrpcChannel channel) : base(channel)
    {
    }

    /// <summary>
    /// Returns and instance of the Pedersen Commitment Group
    /// which should be used when creating Shielded values.
    /// </summary>
    public Group Group { get => Group.Default; }

    private V1.Slice CreateSlice(ShieldedValue source, ShieldedValue quantity, ShieldedValue remainder)
    {
        return new V1.Slice()
        {
            Source = source.ToProtoCommitment(),
            Quantity = quantity.ToProtoCommitment(),
            Remainder = remainder.ToProtoCommitment(),
            ZeroR = ByteString.CopyFrom(((source.R - (quantity.R + remainder.R)).MathMod(Group.Default.q)).ToByteArray())
        };
    }

    private V1.SliceProof CreateSliceProof(ShieldedValue productionSource, ShieldedValue quantity, ShieldedValue productionRemainder)
    {
        return new V1.SliceProof()
        {
            Source = productionSource.ToProtoCommitmentProof(),
            Quantity = quantity.ToProtoCommitmentProof(),
            Remainder = productionRemainder.ToProtoCommitmentProof(),
        };
    }
}

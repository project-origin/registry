using Grpc.Net.Client;

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
}

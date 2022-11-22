using Grpc.Net.Client;

namespace ProjectOrigin.Electricity.Client;

public partial class ElectricityClient : RegisterClient
{
    public ElectricityClient(string registryAddress) : base(registryAddress)
    {
    }

    public ElectricityClient(GrpcChannel channel) : base(channel)
    {
    }
}

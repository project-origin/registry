public record ConcordiumOptions
{
    public string Address { get; init; } //"http://34.71.98.161:10001"
    public string AuthenticationToken { get; init; } //"rpcadmin"
    public string AccountAddress { get; init; }
    public string SignerFilepath { get; init; }
}

namespace ProjectOrigin.Registry.Server.Models;

public record ProcessOptions
{
    public int ServerNumber { get; init; }
    public int Servers { get; init; }
    public int VerifyThreads { get; init; }
    public int Weight { get; init; }
}

namespace ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

/// <summary>
/// This is a simple interface for a Hierarchical Deterministic (HD) algorithm.
/// </summary>
/// <remarks>
/// This interface is used to abstract the HD algorithm from the rest of the application.
/// This allows for easy swapping of the HD algorithm in the future.
/// </remarks>
public interface IHDAlgorithm
{
    public IHDPrivateKey GenerateNewPrivateKey();
    public IHDPrivateKey ImportHDPrivateKey(ReadOnlySpan<byte> privateKeyBytes);
    public IHDPublicKey ImportHDPublicKey(ReadOnlySpan<byte> publicKeyBytes);
    public IPublicKey ImportPublicKey(ReadOnlySpan<byte> privateKeyBytes);
}

/// <summary>
/// This is a simple interface for a Hierarchical Deterministic (HD) private key.
/// </summary>
public interface IHDPrivateKey
{
    /// <summary>
    /// Signs the given data with the private key.
    /// </summary>
    public ReadOnlySpan<byte> Sign(ReadOnlySpan<byte> data);

    /// <summary>
    /// The public key that corresponds to this private key.
    /// </summary>
    public IPublicKey PublicKey { get; }

    /// <summary>
    /// The HD public key that corresponds to this private key.
    /// </summary>
    public IHDPublicKey Neuter();

    /// <summary>
    /// Exports the private key as a byte array.
    /// </summary>
    public ReadOnlySpan<byte> Export();

    /// <summary>
    /// Derives a child private key from this private key.
    /// </summary>
    public IHDPrivateKey Derive(int position);
}

/// <summary>
/// This is a simple interface for a Hierarchical Deterministic (HD) public key.
/// </summary>
public interface IHDPublicKey
{
    /// <summary>
    /// Verifies the given signature against the given data.
    /// </summary>
    public ReadOnlySpan<byte> Export();

    /// <summary>
    /// Verifies the given signature against the given data.
    /// </summary>
    public IHDPublicKey Derive(int position);

    /// <summary>
    /// Verifies the given signature against the given data.
    /// </summary>
    public bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature);

    /// <summary>
    /// Neuter the HD public key to a public key where it
    /// can not be used to derive any more child keys.
    /// </summary>
    public IPublicKey GetPublicKey();
}

public interface IPublicKey
{
    /// <summary>
    /// Export the public key as a span.
    /// </summary>
    ReadOnlySpan<byte> Export();

    /// <summary>
    /// Verify the given signature against the given data.
    /// </summary>
    bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature);
}

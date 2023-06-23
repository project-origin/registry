using System;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;

namespace ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

/// <summary>
/// This is a simple interface for a Hierarchical Deterministic (HD) algorithm.
/// </summary>
/// <remarks>
/// This interface is used to abstract the HD algorithm from the rest of the application.
/// This allows for easy swapping of the HD algorithm in the future.
/// </remarks>
public interface IHDAlgorithm : IPublicKeyAlgorithm
{
    private static Lazy<IHDAlgorithm> secp256k1 = new Lazy<IHDAlgorithm>(() => new Secp256k1Algorithm());

    public static IHDAlgorithm Secp256k1 => secp256k1.Value;

    public IHDPrivateKey GenerateNewPrivateKey();
    public IHDPrivateKey ImportHDPrivateKey(ReadOnlySpan<byte> privateKeyBytes);
    public IHDPublicKey ImportHDPublicKey(ReadOnlySpan<byte> publicKeyBytes);
}

/// <summary>
/// This is a simple interface for a Hierarchical Deterministic (HD) private key.
/// </summary>
public interface IHDPrivateKey : IPrivateKey
{


    /// <summary>
    /// The HD public key that corresponds to this private key.
    /// </summary>
    public IHDPublicKey Neuter();

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

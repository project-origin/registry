using ProjectOrigin.Registry.V1;

namespace ProjectOrigin.Electricity.Interfaces;

public interface IKeyAlgorithm
{
    IPublicKey ImportPublicKey(ReadOnlySpan<byte> span);
    bool TryImport(ReadOnlySpan<byte> span, out IPublicKey _);

    IPrivateKey Create();
}

public interface IPublicKey
{
    ReadOnlySpan<byte> Export();
    bool VerifySignature(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature);
}

public interface IPrivateKey
{
    ReadOnlySpan<byte> Sign(ReadOnlySpan<byte> data);

    IPublicKey PublicKey { get; }
}

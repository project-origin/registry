using ProjectOrigin.Registry.V1;

namespace ProjectOrigin.Electricity.Interfaces;

public interface IKeyAlgorithm
{
    IPublicKey ImportPublicKey(ReadOnlySpan<byte> span);
    bool TryImport(ReadOnlySpan<byte> span, out IPublicKey _);
}

public interface IPublicKey
{
    bool VerifySignature(ReadOnlySpan<byte> data, Signature signature);
}


using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.Register.V1;

namespace ProjectOrigin.Electricity;

public static class extensions
{
    public static bool Verify(this V1.CommitmentProof proof, V1.Commitment commitment)
    {
        return Mapper.ToModel(proof).Verify(Mapper.ToModel(commitment));
    }

    public static Guid ToGuid(this Uuid allocationId) => Guid.Parse(allocationId.Value);

}

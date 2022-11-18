
namespace ProjectOrigin.Electricity.Tests;

internal static class Extensions
{
    public static Register.V1.Uuid ToUuid(this Guid allocationId)
    {
        return new Register.V1.Uuid()
        {
            Value = allocationId.ToString()
        };
    }

}

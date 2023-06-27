using Google.Protobuf.WellKnownTypes;
using ProjectOrigin.Electricity.Server.Interfaces;
using Xunit;

namespace ProjectOrigin.Electricity.Tests;

public static class AssertExtensions
{
    public static void AssertValid(this VerificationResult result)
    {
        var invalid = result as VerificationResult.Invalid;
        if (invalid is not null)
        {
            throw new Xunit.Sdk.XunitException($"Expected Valid, got Invalid: ”{invalid.ErrorMessage}”");
        }
    }

    public static void AssertInvalid(this VerificationResult result, string expectedError)
    {
        var invalid = result as VerificationResult.Invalid;

        if (invalid is not null)
        {
            Assert.Equal(expectedError, invalid.ErrorMessage);
        }
        else
        {
            throw new Xunit.Sdk.XunitException($"Expected Invalid, got Valid, expected error: ”{expectedError}”");
        }
    }


    internal static V1.DateInterval AddHours(this V1.DateInterval interval, int hours)
    {
        return new V1.DateInterval()
        {
            Start = Timestamp.FromDateTimeOffset(interval.Start.ToDateTimeOffset().AddHours(1)),
            End = Timestamp.FromDateTimeOffset(interval.End.ToDateTimeOffset().AddHours(1))
        };
    }
}

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
}

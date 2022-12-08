using ProjectOrigin.Register.StepProcessor.Models;

public abstract class AbstractVerifierTests
{
    public void AssertValid(VerificationResult result)
    {
        var invalid = result as VerificationResult.Invalid;
        if (invalid is not null)
        {
            throw new Xunit.Sdk.XunitException($"Expected Valid, got Invalid: ”{invalid.ErrorMessage}”");
        }
    }

    public void AssertInvalid(VerificationResult result, string expectedError)
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

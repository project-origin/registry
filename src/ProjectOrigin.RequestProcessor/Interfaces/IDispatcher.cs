using ProjectOrigin.RequestProcessor.Models;

namespace ProjectOrigin.RequestProcessor.Interfaces;

public interface IDispatcher
{
    Task<(VerificationResult Result, int NextEventIndex)> Verify(IPublishRequest request);
}

using ProjectOrigin.RequestProcessor.Models;

namespace ProjectOrigin.RequestProcessor.Interfaces;

public interface IRequestVerifier<TRequest, TModel> where TRequest : PublishRequest
{
    Task<VerificationResult> Verify(TRequest request, TModel? model);
}

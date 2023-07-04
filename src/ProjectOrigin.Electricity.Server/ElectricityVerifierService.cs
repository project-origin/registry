using System.Threading.Tasks;
using Grpc.Core;
using ProjectOrigin.Electricity.Server.Interfaces;
using ProjectOrigin.Verifier.V1;

namespace ProjectOrigin.Electricity.Server;

internal class ElectricityVerifierService : ProjectOrigin.Verifier.V1.VerifierService.VerifierServiceBase
{
    private IVerifierDispatcher _verifierDispatcher;

    public ElectricityVerifierService(IVerifierDispatcher verifierDispatcher)
    {
        _verifierDispatcher = verifierDispatcher;
    }

    public override async Task<VerifyTransactionResponse> VerifyTransaction(VerifyTransactionRequest request, ServerCallContext context)
    {
        var result = await _verifierDispatcher.Verify(request.Transaction, request.Stream);

        var response = new VerifyTransactionResponse
        {
            Valid = true
        };

        if (result is VerificationResult.Invalid)
        {
            response.Valid = false;
            response.ErrorMessage = ((VerificationResult.Invalid)result).ErrorMessage;
        }

        return response;
    }
}

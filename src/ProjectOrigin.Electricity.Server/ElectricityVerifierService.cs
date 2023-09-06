using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Electricity.Server.Exceptions;
using ProjectOrigin.Electricity.Server.Interfaces;
using ProjectOrigin.Verifier.V1;

namespace ProjectOrigin.Electricity.Server;

internal class ElectricityVerifierService : VerifierService.VerifierServiceBase
{
    private readonly ILogger<ElectricityVerifierService> _logger;
    private readonly IVerifierDispatcher _verifierDispatcher;

    public ElectricityVerifierService(ILogger<ElectricityVerifierService> logger, IVerifierDispatcher verifierDispatcher)
    {
        _logger = logger;
        _verifierDispatcher = verifierDispatcher;
    }

    public override async Task<VerifyTransactionResponse> VerifyTransaction(VerifyTransactionRequest request, ServerCallContext context)
    {
        try
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
        catch (InvalidPayloadException ex)
        {
            _logger.LogError(ex, "Invalid payload while verifying transaction");

            return new VerifyTransactionResponse
            {
                Valid = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

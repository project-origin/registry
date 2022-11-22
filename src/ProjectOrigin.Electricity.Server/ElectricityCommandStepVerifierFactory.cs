using ProjectOrigin.Electricity.Consumption.Requests;
using ProjectOrigin.Electricity.Production.Requests;
using ProjectOrigin.Register.StepProcessor.Interfaces;

namespace ProjectOrigin.Electricity.Server;

public class ElectricityCommandStepVerifierFactory : ICommandStepVerifierFactory
{
    private IServiceProvider _serviceProvider;

    public IEnumerable<Type> SupportedTypes
    {
        get
        {
            return new List<Type>(){
                typeof(ConsumptionAllocatedVerifier),
                typeof(ConsumptionClaimedVerifier),
                typeof(ConsumptionIssuedVerifier),
                typeof(ProductionAllocatedVerifier),
                typeof(ProductionClaimedVerifier),
                typeof(ProductionIssuedVerifier),
                typeof(ProductionSliceTransferredVerifier),
            };
        }
    }

    public ElectricityCommandStepVerifierFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object Get(Type type)
    {
        return ActivatorUtilities.CreateInstance(_serviceProvider, type);
    }
}

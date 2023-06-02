using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Registry.Utils.Services;

namespace ProjectOrigin.Electricity.Services;

public class ElectricityModelHydrater : AbstractModelHydrator
{
    private IKeyAlgorithm _keyAlgorithm;

    public ElectricityModelHydrater(IKeyAlgorithm keyAlgorithm)
    {
        _keyAlgorithm = keyAlgorithm;
    }

    protected override object Create(object firstEvent)
    {
        if (firstEvent is V1.ConsumptionIssuedEvent)
            return new Consumption.ConsumptionCertificate((V1.ConsumptionIssuedEvent)firstEvent, _keyAlgorithm);
        else if (firstEvent is V1.ProductionIssuedEvent)
            return new Production.ProductionCertificate((V1.ProductionIssuedEvent)firstEvent, _keyAlgorithm);
        else
            throw new NotSupportedException($"Event ”{firstEvent.GetType().FullName}” not supported to create model");
    }
}

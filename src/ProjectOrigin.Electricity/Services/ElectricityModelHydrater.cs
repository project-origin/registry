using System;
using ProjectOrigin.Verifier.Utils.Services;

namespace ProjectOrigin.Electricity.Services;

public class ElectricityModelHydrater : AbstractModelHydrator
{
    protected override object Create(object firstEvent)
    {
        if (firstEvent is V1.ConsumptionIssuedEvent)
            return new Consumption.ConsumptionCertificate((V1.ConsumptionIssuedEvent)firstEvent);
        else if (firstEvent is V1.ProductionIssuedEvent)
            return new Production.ProductionCertificate((V1.ProductionIssuedEvent)firstEvent);
        else
            throw new NotSupportedException($"Event ”{firstEvent.GetType().FullName}” not supported to create model");
    }
}

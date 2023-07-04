using System;
using ProjectOrigin.Electricity.Models;

namespace ProjectOrigin.Electricity.Server.Services;

public class ElectricityModelHydrater : AbstractModelHydrator
{
    protected override object Create(object firstEvent)
    {
        if (firstEvent is V1.IssuedEvent)
            return new GranularCertificate((V1.IssuedEvent)firstEvent);
        else
            throw new NotSupportedException($"Event ”{firstEvent.GetType().FullName}” not supported to create model");
    }
}

using System;
using ProjectOrigin.Electricity.Server.Models;

namespace ProjectOrigin.Electricity.Server.Services;

public class ElectricityModelHydrater : AbstractModelHydrator
{
    protected override object Create(object firstEvent)
    {
        if (firstEvent is V1.IssuedEvent typedEvent)
            return new GranularCertificate(typedEvent);
        else
            throw new NotSupportedException($"Event ”{firstEvent.GetType().FullName}” not supported to create model");
    }
}

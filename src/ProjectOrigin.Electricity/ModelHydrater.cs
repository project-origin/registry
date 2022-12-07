namespace ProjectOrigin.Electricity;

public class ModelHydrater
{
    public T HydrateModel<T>(IEnumerable<object> eventStream) where T : class
    {
        return HydrateModel(eventStream) as T ?? throw new Exception();
    }

    public dynamic HydrateModel(IEnumerable<object> eventStream)
    {
        dynamic model = Create(eventStream.First());

        foreach (var @event in eventStream.Skip(1))
        {
            model.Apply(@event);
        }

        return model;
    }

    private dynamic Create(object firstEvent)
    {
        if (firstEvent is V1.ConsumptionIssuedEvent)
            return new Consumption.ConsumptionCertificate((V1.ConsumptionIssuedEvent)firstEvent);
        else if (firstEvent is V1.ProductionIssuedEvent)
            return new Production.ProductionCertificate((V1.ProductionIssuedEvent)firstEvent);
        else
            throw new NotSupportedException($"Event ”{firstEvent.GetType().FullName}” not supported to create model");
    }
}

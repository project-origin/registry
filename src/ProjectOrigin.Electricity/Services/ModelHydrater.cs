using ProjectOrigin.Electricity.Interfaces;

namespace ProjectOrigin.Electricity;

internal class ModelHydrater : IModelHydrater
{
    private const string ApplyMethodName = "Apply";

    public T HydrateModel<T>(IEnumerable<object> eventStream) where T : class
    {
        return HydrateModel(eventStream) as T ?? throw new Exception();
    }

    public object? HydrateModel(IEnumerable<object> eventStream)
    {
        object? model = null;

        if (eventStream.Count() > 0)
        {
            model = Create(eventStream.First());
            foreach (var @event in eventStream.Skip(1))
            {
                Apply(model, @event);
            }
        }

        return model;
    }

    private static void Apply(object model, object @event)
    {
        var methodInfo = model.GetType()
                            .GetMethods()
                            .Where(method =>
                                method.Name == ApplyMethodName
                                && method.GetParameters().SingleOrDefault(x => x.ParameterType == @event.GetType()) != null)
                            .SingleOrDefault();

        if (methodInfo != null)
        {
            methodInfo.Invoke(model, new object[] { @event });
        }
        else
        {
            throw new NotSupportedException($"Model of type ”{model.GetType().Name}” does not have an ”{ApplyMethodName}” method for type ”{@event.GetType().FullName}”");
        }
    }

    private static object Create(object firstEvent)
    {
        if (firstEvent is V1.ConsumptionIssuedEvent)
            return new Consumption.ConsumptionCertificate((V1.ConsumptionIssuedEvent)firstEvent);
        else if (firstEvent is V1.ProductionIssuedEvent)
            return new Production.ProductionCertificate((V1.ProductionIssuedEvent)firstEvent);
        else
            throw new NotSupportedException($"Event ”{firstEvent.GetType().FullName}” not supported to create model");
    }
}

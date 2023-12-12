using System;
using System.Collections.Generic;
using System.Linq;
using ProjectOrigin.Electricity.Server.Interfaces;

namespace ProjectOrigin.Electricity.Server.Services;

public abstract class AbstractModelHydrator : IModelHydrater
{
    protected const string ApplyMethodName = "Apply";

    public T? HydrateModel<T>(IEnumerable<object> eventStream) where T : class
    {
        return HydrateModel(eventStream) as T;
    }

    public object? HydrateModel(IEnumerable<object> eventStream)
    {
        object? model = null;

        if (eventStream.Any())
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
                            .SingleOrDefault(method =>
                                method.Name == ApplyMethodName
                                && method.GetParameters().SingleOrDefault(x => x.ParameterType == @event.GetType()) != null);

        if (methodInfo is not null)
        {
            methodInfo.Invoke(model, new object[] { @event });
        }
        else
        {
            throw new NotSupportedException($"Model of type ”{model.GetType().Name}” does not have an ”{ApplyMethodName}” method for type ”{@event.GetType().FullName}”");
        }
    }

    protected abstract object Create(object firstEvent);
}

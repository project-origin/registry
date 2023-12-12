using System.Collections.Generic;

namespace ProjectOrigin.Electricity.Server.Interfaces;

public interface IModelHydrater
{
    T? HydrateModel<T>(IEnumerable<object> eventStream) where T : class;
    object? HydrateModel(IEnumerable<object> eventStream);
}

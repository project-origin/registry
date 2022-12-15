namespace ProjectOrigin.Electricity.Interfaces;

internal interface IModelHydrater
{
    T HydrateModel<T>(IEnumerable<object> eventStream) where T : class;
    object? HydrateModel(IEnumerable<object> eventStream);
}

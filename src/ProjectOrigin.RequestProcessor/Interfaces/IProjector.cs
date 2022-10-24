namespace ProjectOrigin.RequestProcessor.Interfaces;

public interface IProjector
{
    IModel Project(IEnumerable<object> events);
}

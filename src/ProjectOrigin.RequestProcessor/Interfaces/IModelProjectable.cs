namespace ProjectOrigin.RequestProcessor.Interfaces;

public interface IModelProjectable<T>
{
    void Apply(T e);
}

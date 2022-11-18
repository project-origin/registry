namespace ProjectOrigin.Register.LineProcessor.Interfaces;

public interface IModel
{
}

public interface IModelProjectable<T>
{
    void Apply(T e);
}


namespace ProjectOrigin.Register.StepProcessor.Interfaces;

public interface IModel
{
}

public interface IModelProjectable<T>
{
    void Apply(T e);
}


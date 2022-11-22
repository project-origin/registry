using System.Reflection;
using Google.Protobuf;
using ProjectOrigin.Register.StepProcessor.Interfaces;

namespace ProjectOrigin.Register.StepProcessor.Services;

public class ModelProjector : IModelProjector
{
    private Type _type;
    private Dictionary<Type, MethodInfo> _applyDict;

    public ModelProjector(Type type)
    {
        _type = type;
        var methonName = nameof(IModelProjectable<object>.Apply);
        var applyMethods = type.GetMethods().Where(x => x.Name == methonName && x.GetParameters().Count() == 1 && x.ReturnType == typeof(void));
        _applyDict = applyMethods.ToDictionary(m => m.GetParameters().Single().ParameterType);
    }

    public IModel Project(IEnumerable<IMessage> events)
    {
        var obj = Activator.CreateInstance(_type) ?? throw new Exception("Type could not be instanciated.");

        foreach (var e in events)
        {
            var eventType = e.GetType();
            if (_applyDict.TryGetValue(eventType, out var method))
            {
                method.Invoke(obj, new object[] { e });
            }
            else
            {
                throw new NotImplementedException($"No ”Apply” method implemented on class ”{_type.Name}” for event ”{eventType.Name}”");
            }
        }

        return (IModel)obj;
    }
}

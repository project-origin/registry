using System.Reflection;
using Google.Protobuf;
using ProjectOrigin.Register.LineProcessor.Interfaces;

namespace ProjectOrigin.Register.LineProcessor.Services;

public class Projector : IModelProjector
{
    private Type type;
    private Dictionary<Type, MethodInfo> applyDict;

    public Projector(Type type)
    {
        this.type = type;
        var methonName = nameof(IModelProjectable<object>.Apply);
        var applyMethods = type.GetMethods().Where(x => x.Name == methonName && x.GetParameters().Count() == 1 && x.ReturnType == typeof(void));
        applyDict = applyMethods.ToDictionary(m => m.GetParameters().Single().ParameterType);
    }

    public IModel Project(IEnumerable<IMessage> events)
    {
        var obj = Activator.CreateInstance(type) ?? throw new Exception("Type could not be instanciated.");

        foreach (var e in events)
        {
            var eventType = e.GetType();
            if (applyDict.TryGetValue(eventType, out var method))
            {
                method.Invoke(obj, new object[] { e });
            }
            else
            {
                throw new NotImplementedException($"No ”Apply” method implemented on class ”{type.Name}” for event ”{eventType.Name}”");
            }
        }

        return (IModel)obj;
    }
}

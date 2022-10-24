using System.Reflection;
using ProjectOrigin.RequestProcessor.Interfaces;

namespace ProjectOrigin.RequestProcessor.Services;

public class Projector : IProjector
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

    public IModel Project(IEnumerable<object> events)
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

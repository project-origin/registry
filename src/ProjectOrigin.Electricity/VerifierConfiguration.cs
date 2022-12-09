using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.Services;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.Utils.Interfaces;
using ProjectOrigin.Register.Utils.Services;

namespace ProjectOrigin.Electricity;

public static class VerifierConfiguration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        AddImplementationsForInterface(services, typeof(IEventVerifier<,>));
        AddImplementationsForInterface(services, typeof(IEventVerifier<>));

        services.AddScoped<IProtoSerializer>((serviceProvider) =>
        {
            return new ProtoSerializer(Assembly.GetExecutingAssembly());
        });
        services.AddScoped<IModelHydrater, ModelHydrater>();
        services.AddScoped<ICommandStepVerifiere>((serviceProvider) =>
        {
            return ActivatorUtilities.CreateInstance<ElectricityStepVerifier>(serviceProvider);
        });
    }

    private static void AddImplementationsForInterface(IServiceCollection services, Type interfaceType) =>
        Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(item => item.GetInterfaces()
                .Where(i => i.IsGenericType)
                .Any(i => i.GetGenericTypeDefinition() == interfaceType) && !item.IsAbstract && !item.IsInterface)
            .ToList()
            .ForEach(assignedTypes =>
            {
                var serviceType = assignedTypes.GetInterfaces().First(i => i.GetGenericTypeDefinition() == interfaceType);
                services.AddScoped(serviceType, assignedTypes);
            });
}

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Durably;

internal static class FlowAssemblyScanner
{
    public static void RegisterFlows(IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters)
            {
                continue;
            }

            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType)
                {
                    continue;
                }

                var definition = iface.GetGenericTypeDefinition();
                if (definition == typeof(IFlow<>))
                {
                    var stateType = iface.GetGenericArguments()[0];
                    services.AddSingleton(type);
                    services.AddSingleton(iface, sp => sp.GetRequiredService(type));

                    var registrationType = typeof(FlowRegistration<>).MakeGenericType(stateType);
                    var fromFlowType = registrationType.GetMethod(
                        nameof(FlowRegistration<object>.FromFlowType),
                        BindingFlags.Public | BindingFlags.Static)!;
                    var registration = (IFlowRegistration)fromFlowType.Invoke(null, new object[] { type })!;
                    services.AddSingleton(registration);
                }
                else if (definition == typeof(IStep<>))
                {
                    services.TryAddTransient(type);
                }
            }
        }
    }
}

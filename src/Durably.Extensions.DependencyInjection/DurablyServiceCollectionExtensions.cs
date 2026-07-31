using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Durably;

public static class DurablyServiceCollectionExtensions
{
    public static IDurablyBuilder AddDurably(this IServiceCollection services, Action<DurablyOptions>? configure = null)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var options = new DurablyOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<IStateSerializer>(_ => new JsonStateSerializer(options.SerializerOptions));
        services.TryAddSingleton<ITraceSink, NoOpTraceSink>();
        services.TryAddSingleton<IExecutionWorkSignal, ExecutionWorkSignal>();
        services.TryAddSingleton<IFlowRegistry>(sp =>
        {
            var registry = new FlowRegistry();
            foreach (var registration in sp.GetServices<IFlowRegistration>())
            {
                registry.Register(registration);
            }

            return registry;
        });
        services.TryAddSingleton<ITraceQuery>(sp =>
            new TraceStoreQuery(sp.GetRequiredService<ITraceStore>()));
        services.TryAddSingleton<IFlowEngine>(sp =>
            new FlowEngine(
                sp.GetRequiredService<IExecutionStore>(),
                sp.GetRequiredService<IStateSerializer>(),
                sp.GetRequiredService<IExecutionWorkSignal>()));
        services.TryAddSingleton(sp =>
        {
            var durablyOptions = sp.GetRequiredService<DurablyOptions>();
            var stepDefaults = new StepDefaults(durablyOptions.DefaultRetry, durablyOptions.DefaultStepTimeout);
            var loggerFactory = sp.GetService(typeof(Microsoft.Extensions.Logging.ILoggerFactory))
                as Microsoft.Extensions.Logging.ILoggerFactory;
            var logger = loggerFactory?.CreateLogger("Durably.ExecutionProcessor");
            return new ExecutionProcessor(
                sp.GetRequiredService<IExecutionStore>(),
                sp.GetRequiredService<IFlowRegistry>(),
                sp.GetRequiredService<IStateSerializer>(),
                sp,
                sp.GetRequiredService<ITraceSink>(),
                stepDefaults,
                logger);
        });

        services.TryAddSingleton<DurablyWorkerOptions>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DurablyWorkerService>());

        return new DurablyBuilder(services, options);
    }

    public static IDurablyBuilder ConfigureWorker(this IDurablyBuilder builder, Action<DurablyWorkerOptions> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var options = new DurablyWorkerOptions();
        configure(options);
        builder.Services.RemoveAll<DurablyWorkerOptions>();
        builder.Services.AddSingleton(options);
        return builder;
    }

    public static IDurablyBuilder AddFlow<TFlow, TState>(this IDurablyBuilder builder)
        where TFlow : class, IFlow<TState>
        where TState : class, new()
    {
        builder.Services.AddSingleton<TFlow>();
        builder.Services.AddSingleton<IFlow<TState>>(sp => sp.GetRequiredService<TFlow>());
        builder.Services.AddSingleton<IFlowRegistration>(_ => FlowRegistration<TState>.FromFlowType(typeof(TFlow)));
        return builder;
    }

    public static IDurablyBuilder AddFlow<TState>(this IDurablyBuilder builder, IFlowBuilder<TState> flow)
        where TState : class, new()
    {
        if (flow is not FlowBuilder<TState> typed)
        {
            throw new ArgumentException("Flow must be created via Flow.For<TState>() / the built-in builder.", nameof(flow));
        }

        var registration = FlowRegistration<TState>.FromBuilder(typed);
        builder.Services.AddSingleton<IFlowRegistration>(_ => registration);
        return builder;
    }

    public static IDurablyBuilder AddFlowsFromAssembly(this IDurablyBuilder builder, Assembly assembly)
    {
        if (assembly is null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        FlowAssemblyScanner.RegisterFlows(builder.Services, assembly);
        return builder;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Durably;

/// <summary>Registers the in-memory persistence layer on an <see cref="IDurablyBuilder"/>.</summary>
public static class DurablyInMemoryBuilderExtensions
{
    /// <summary>
    /// Use in-process stores for executions, queries, and traces (tests, demos, local development).
    /// </summary>
    public static IDurablyBuilder UseInMemoryStore(this IDurablyBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.RemoveAll<InMemoryExecutionStore>();
        builder.Services.RemoveAll<IExecutionStore>();
        builder.Services.AddSingleton<InMemoryExecutionStore>();
        builder.Services.AddSingleton<IExecutionStore>(sp => sp.GetRequiredService<InMemoryExecutionStore>());

        builder.Services.RemoveAll<IExecutionQuery>();
        builder.Services.AddSingleton<IExecutionQuery>(sp =>
            new InMemoryExecutionQuery(sp.GetRequiredService<InMemoryExecutionStore>()));

        builder.Services.RemoveAll<ITraceStore>();
        builder.Services.AddSingleton<ITraceStore, InMemoryTraceStore>();

        builder.Services.RemoveAll<ITraceQuery>();
        builder.Services.AddSingleton<ITraceQuery>(sp =>
            new TraceStoreQuery(sp.GetRequiredService<ITraceStore>()));

        return builder;
    }
}

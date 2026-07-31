using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Durably.Engine;
/// <summary>Invokes flow success/failure hooks without failing the execution pipeline.</summary>
internal sealed class FlowHookInvoker
{
    private readonly IServiceProvider? _services;
    private readonly ILogger _logger;

    public FlowHookInvoker(IServiceProvider? services, ILogger? logger = null)
    {
        _services = services;
        _logger = logger ?? NullLogger.Instance;
    }

    public async Task InvokeSuccessHooksAsync<TState>(
        FlowBuilder<TState> builder,
        TState state,
        CancellationToken cancellationToken)
    {
        if (builder.OnSuccessHandler is not null)
        {
            await InvokeSafeAsync(() => builder.OnSuccessHandler(state, cancellationToken)).ConfigureAwait(false);
        }

        foreach (var handler in ResolveHandlers<IFlowSuccessHandler<TState>>())
        {
            await InvokeSafeAsync(() => handler.HandleAsync(state, cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task InvokeFailureHooksAsync<TState>(
        FlowBuilder<TState> builder,
        TState state,
        string? failedStep,
        Exception? error,
        CancellationToken cancellationToken)
    {
        if (builder.OnFailureHandler is not null)
        {
            await InvokeSafeAsync(() => builder.OnFailureHandler(state, error, cancellationToken)).ConfigureAwait(false);
        }

        foreach (var handler in ResolveHandlers<IFlowFailureHandler<TState>>())
        {
            await InvokeSafeAsync(() => handler.HandleAsync(state, failedStep, error, cancellationToken)).ConfigureAwait(false);
        }
    }

    private IEnumerable<THandler> ResolveHandlers<THandler>()
    {
        if (_services is null)
        {
            return Array.Empty<THandler>();
        }

        var enumerable = _services.GetService(typeof(IEnumerable<THandler>));
        if (enumerable is IEnumerable<THandler> handlers)
        {
            return handlers;
        }

        return Array.Empty<THandler>();
    }

    private async Task InvokeSafeAsync(Func<Task> invoke)
    {
        try
        {
            await invoke().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Durably flow hook failed and was swallowed so execution can continue.");
        }
    }
}

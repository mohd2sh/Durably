namespace Durably.Core.UnitTests;

public sealed class GlobalDefaultRetryOverrideFlow : IFlow<OrderState>
{
    private readonly Func<OrderState, CancellationToken, Task> _noRetryBody;
    private readonly Func<OrderState, CancellationToken, Task> _defaultRetryBody;

    public GlobalDefaultRetryOverrideFlow(
        Func<OrderState, CancellationToken, Task> noRetryBody,
        Func<OrderState, CancellationToken, Task> defaultRetryBody)
    {
        _noRetryBody = noRetryBody ?? throw new ArgumentNullException(nameof(noRetryBody));
        _defaultRetryBody = defaultRetryBody ?? throw new ArgumentNullException(nameof(defaultRetryBody));
    }

    public void Build(IFlowBuilder<OrderState> builder) => builder
        .Step("no-retry", _noRetryBody, o => o.Retry(RetryPolicy.None))
        .Step("default-retry", _defaultRetryBody);
}

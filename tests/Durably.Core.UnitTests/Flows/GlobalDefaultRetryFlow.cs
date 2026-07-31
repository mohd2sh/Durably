namespace Durably.Core.UnitTests;

public sealed class GlobalDefaultRetryFlow : IFlow<OrderState>
{
    private readonly Func<OrderState, CancellationToken, Task> _body;

    public GlobalDefaultRetryFlow(Func<OrderState, CancellationToken, Task> body)
    {
        _body = body ?? throw new ArgumentNullException(nameof(body));
    }

    public void Build(IFlowBuilder<OrderState> builder) =>
        builder.Step("flaky", _body);
}

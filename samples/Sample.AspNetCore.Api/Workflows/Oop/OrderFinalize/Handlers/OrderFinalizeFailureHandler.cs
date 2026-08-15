namespace Sample.AspNetCore.Api.Workflows.Oop.OrderFinalize;

public sealed class OrderFinalizeFailureHandler : IFlowFailureHandler<OrderFinalizeState>
{
    private readonly ILogger<OrderFinalizeFailureHandler> _logger;

    public OrderFinalizeFailureHandler(ILogger<OrderFinalizeFailureHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(
        OrderFinalizeState state,
        string? failedStep,
        Exception? error,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            error,
            "Order {OrderId} finalize failed at step {FailedStep}: {Message}",
            state.Order.Id,
            failedStep,
            error?.Message);
        return Task.CompletedTask;
    }
}

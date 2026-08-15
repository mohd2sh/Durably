namespace Sample.AspNetCore.Api.Workflows.Oop.OrderFinalize;

public sealed class OrderFinalizeSuccessHandler : IFlowSuccessHandler<OrderFinalizeState>
{
    private readonly ILogger<OrderFinalizeSuccessHandler> _logger;

    public OrderFinalizeSuccessHandler(ILogger<OrderFinalizeSuccessHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderFinalizeState state, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Order {OrderId} finalize succeeded. EmailSent={EmailSent}, Finalized={Finalized}",
            state.Order.Id,
            state.EmailSent,
            state.Finalized);
        return Task.CompletedTask;
    }
}

using Sample.AspNetCore.Api.Services;

namespace Sample.AspNetCore.Api.Workflows.Oop.OrderFinalize;

public sealed class FinalizeOrderStep : IStep<OrderFinalizeState>
{
    private readonly IOrderService _orders;

    public FinalizeOrderStep(IOrderService orders)
    {
        _orders = orders;
    }

    public async Task ExecuteAsync(OrderFinalizeState state, IStepContext context, CancellationToken cancellationToken)
    {
        await _orders.FinalizeAsync(state.Order.Id, cancellationToken);
        state.Finalized = true;
    }
}

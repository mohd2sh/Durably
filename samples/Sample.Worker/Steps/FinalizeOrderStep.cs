using Durably;
using Sample.Worker.Models;
using Sample.Worker.Services;

namespace Sample.Worker.Steps;

public sealed class FinalizeOrderStep : IStep<OrderFinalizeState>
{
    private readonly IOrderService _orders;

    public FinalizeOrderStep(IOrderService orders)
    {
        _orders = orders;
    }

    public async Task ExecuteAsync(OrderFinalizeState state, IStepContext context, CancellationToken cancellationToken)
    {
        await _orders.FinalizeAsync(state.OrderId, cancellationToken);
        state.Finalized = true;
    }
}

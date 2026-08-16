namespace Sample.AspNetCore.Api.Workflows.Oop.OrderFulfillment;

public sealed class PackExpressStep : IStep<OrderFulfillmentState>
{
    public Task ExecuteAsync(OrderFulfillmentState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.Packed = true;
        return Task.CompletedTask;
    }
}

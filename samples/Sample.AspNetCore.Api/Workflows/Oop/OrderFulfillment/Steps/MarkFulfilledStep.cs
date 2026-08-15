namespace Sample.AspNetCore.Api.Workflows.Oop.OrderFulfillment;

public sealed class MarkFulfilledStep : IStep<OrderFulfillmentState>
{
    public Task ExecuteAsync(OrderFulfillmentState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.Fulfilled = true;
        return Task.CompletedTask;
    }
}

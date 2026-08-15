namespace Sample.AspNetCore.Api.Workflows.Oop.OrderFulfillment;

public sealed class ValidateOrderStep : IStep<OrderFulfillmentState>
{
    public Task ExecuteAsync(OrderFulfillmentState state, IStepContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(state.Order.Id))
        {
            throw new InvalidOperationException("Order id is required.");
        }

        state.Validated = true;
        return Task.CompletedTask;
    }
}

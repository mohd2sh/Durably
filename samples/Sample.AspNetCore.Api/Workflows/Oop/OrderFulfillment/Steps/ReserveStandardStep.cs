namespace Sample.AspNetCore.Api.Workflows.Oop.OrderFulfillment;

public sealed class ReserveStandardStep : IStep<OrderFulfillmentState>
{
    public Task ExecuteAsync(OrderFulfillmentState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.Reservation = $"standard:{state.Order.Id}";
        return Task.CompletedTask;
    }
}

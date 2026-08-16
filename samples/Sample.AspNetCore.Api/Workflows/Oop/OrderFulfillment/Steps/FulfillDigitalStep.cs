namespace Sample.AspNetCore.Api.Workflows.Oop.OrderFulfillment;

public sealed class FulfillDigitalStep : IStep<OrderFulfillmentState>
{
    public Task ExecuteAsync(OrderFulfillmentState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.Reservation = $"digital:{state.Order.Id}";
        return Task.CompletedTask;
    }
}

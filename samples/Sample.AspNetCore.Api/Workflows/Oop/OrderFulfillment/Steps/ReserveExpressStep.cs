namespace Sample.AspNetCore.Api.Workflows.Oop.OrderFulfillment;

public sealed class ReserveExpressStep : IStep<OrderFulfillmentState>
{
    public Task ExecuteAsync(OrderFulfillmentState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.Reservation = $"express:{state.Order.Id}";
        return Task.CompletedTask;
    }
}

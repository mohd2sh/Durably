namespace Sample.Worker.Services;

public sealed class OrderService : IOrderService
{
    public Task FinalizeAsync(string orderId, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

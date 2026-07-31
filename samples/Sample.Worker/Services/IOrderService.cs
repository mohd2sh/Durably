namespace Sample.Worker.Services;

public interface IOrderService
{
    Task FinalizeAsync(string orderId, CancellationToken cancellationToken);
}
